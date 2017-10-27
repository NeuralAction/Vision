using MathNet.Numerics.LinearAlgebra;
using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;
using Xamarin.Forms;

namespace Vision.Detection
{
    public class FaceRect : Rect
    {
        public List<EyeRect> Children { get; set; } = new List<EyeRect>();
        public EyeRect LeftEye
        {
            get
            {
                foreach(EyeRect r in Children)
                {
                    if (r.Center.X < _width * 0.5 && r.Center.Y < _height * 0.45)
                        return r;
                }
                return null;
            }
        }
        public EyeRect RightEye
        {
            get
            {
                foreach (EyeRect r in Children)
                {
                    if (r.Center.X > _width * 0.5 && r.Center.Y < _height * 0.45)
                        return r;
                }
                return null;
            }
        }
        public EyeGazeInfo Info { get; set; }

        public Point[] Landmarks { get; set; }
        public double[] LandmarkTransformVector { get; set; }
        public double[] LandmarkRotationVector { get; set; }
        public double[,] LandmarkCameraMatrix { get; set; }
        public double[] LandmarkDistCoeffs { get; set; }

        public double UnitPerMM { get; set; }

        public FaceSmoother Smoother { get; set; }

        public FaceRect(Rect rect, FaceSmoother smoother) : base(rect.Rectangle)
        {
            Smoother = smoother;
        }

        public void Add(EyeRect rect)
        {
            Children.Add(rect);
        }

        public void Draw(Mat frame, double thickness = 1, bool drawEyes = false, bool drawLandmarks = false)
        {
            Core.Cv.DrawRectangle(frame, this, Scalar.BgrMagenta, thickness, LineTypes.Link4, 0);

            if (drawEyes && Children != null)
            {
                foreach (EyeRect eye in Children)
                    eye.Draw(frame, thickness);
            }

            if (drawLandmarks && Landmarks != null)
            {
                bool first = true;
                int count = 0;
                foreach (Point pt in Landmarks)
                {
                    Scalar color = Scalar.BgrRed;
                    if (first)
                    {
                        color = Scalar.BgrMagenta;
                        first = false;
                    }
                    count++;
                    Core.Cv.DrawCircle(frame, pt, 2, color, 4, LineTypes.AntiAlias);
                    frame.DrawText(pt.X + 5, pt.Y, count.ToString());
                }

                if (LandmarkTransformVector != null && LandmarkRotationVector != null && LandmarkDistCoeffs != null && LandmarkCameraMatrix != null)
                {
                    Profiler.Start("FaceRectProject");
                    List<Point3D> nose_end_point3D = new List<Point3D>()
                    {
                        new Point3D(0,0,0) * UnitPerMM,
                        new Point3D(80,0,0) * UnitPerMM,
                        new Point3D(0,80,0) * UnitPerMM,
                        new Point3D(0,0,80) * UnitPerMM,
                    };

                    double[] rv = LandmarkRotationVector;
                    double[] tv = LandmarkTransformVector;
                    double[] dc = LandmarkDistCoeffs;
                    double[,] cm = LandmarkCameraMatrix;

                    Point[] nose_end_point2D;
                    double[,] jacobia;
                    Core.Cv.ProjectPoints(nose_end_point3D, rv, tv, cm, dc, out nose_end_point2D, out jacobia);

                    Point start_origin = nose_end_point2D[0];
                    Core.Cv.DrawLine(frame, start_origin, nose_end_point2D[1], Scalar.BgrRed, 2, LineTypes.AntiAlias);
                    Core.Cv.DrawLine(frame, start_origin, nose_end_point2D[2], Scalar.BgrGreen, 2, LineTypes.AntiAlias);
                    Core.Cv.DrawLine(frame, start_origin, nose_end_point2D[3], Scalar.BgrBlue, 2, LineTypes.AntiAlias);

                    string msg = $"tv:{tv[0].ToString("0.0").PadRight(5)},{tv[1].ToString("0.0").PadRight(5)},{tv[2].ToString("0.0").PadRight(5)}\n" +
                        $"rv:{rv[0].ToString("0.0").PadRight(5)},{rv[1].ToString("0.0").PadRight(5)},{rv[2].ToString("0.0").PadRight(5)}";
                    frame.DrawText(X, Y + Height + 35, msg, Scalar.BgrWhite);
                    Profiler.End("FaceRectProject");
                }
            }
        }

        public double[] SolveLookScreenRodrigues(Point scrPt, ScreenProperties properties)
        {
            Vector<double> ptVec = CreateVector.Dense(SolveLookScreenVector(scrPt, properties).ToArray());

            Vector<double> originVec = CreateVector.Dense(new double[] { 0, 0, -1 });

            var dotProduct = ptVec.DotProduct(originVec);
            var crossProduct = Util.CrossProduct(ptVec, originVec);
            crossProduct = crossProduct / crossProduct.L2Norm();

            var theta = Math.Acos(dotProduct);
            if (theta > Math.PI / 2)
            {
                theta = Math.PI - theta;
                crossProduct = crossProduct * -1;
            }
            var rodVec = crossProduct * theta;

            var rod = rodVec.ToArray();

            return rod;
        }

        public Point3D SolveLookScreenVector(Point scrPt, ScreenProperties properties)
        {
            var unitPermm = UnitPerMM;

            Point3D point3d = properties.ToCameraCoordinate(unitPermm, scrPt);
            point3d = point3d - new Point3D(LandmarkTransformVector);

            Vector<double> ptVec = CreateVector.Dense(point3d.ToArray());
            ptVec = ptVec / ptVec.L2Norm();

            return new Point3D(ptVec.ToArray());
        }

        public Point SolveRayScreenRodrigues(double[] rod, ScreenProperties properties)
        {
            double[,] rotMat;
            Core.Cv.Rodrigues(rod, out rotMat);

            var rotMatMat = CreateMatrix.DenseOfArray(rotMat);
            var tempVec = CreateVector.Dense(new double[] { 0, 0, -1 }) * rotMatMat;

            return SolveRayScreenVector(new Point3D(tempVec.ToArray()), properties);
        }

        public Point SolveRayScreenVector(Point3D vec, ScreenProperties properties)
        {
            var unitPermm = UnitPerMM;

            var tempVec = CreateVector.Dense(vec.ToArray());
            var tempScale = Math.Abs(LandmarkTransformVector[2] / tempVec[2]);
            tempVec = tempVec * tempScale;
            tempVec = tempVec + CreateVector.DenseOfArray(LandmarkTransformVector);

            if (tempVec[2] > 0.001)
                throw new ArgumentException("vector cannot be solve xD");

            var tempScr = properties.ToScreenCoordinate(unitPermm, new Point3D(tempVec.ToArray()));
            //tempScr.X = properties.PixelSize.Width - Util.FixZero(tempScr.X);
            //tempScr.Y = -Util.FixZero(tempScr.Y);
            tempScr.X = Util.FixZero(tempScr.X);
            tempScr.Y = Util.FixZero(tempScr.Y);

            return tempScr;
        }

        public Mat ROI(Mat frame)
        {
            return new Mat(frame, ToCvRect());
        }
    }
}
