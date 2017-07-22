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

        public FaceRect(Rect rect) : base(rect.Rectangle)
        {

        }

        public void Add(EyeRect rect)
        {
            Children.Add(rect);
        }

        public void Draw(VMat frame, double thickness = 1, bool drawEyes = false, bool drawLandmarks = false)
        {
            Point center = new Point(X + Width * 0.5, Y + Height * 0.5);

            Size size = new Size(Width * 0.5, Height * 0.5);

            Core.Cv.DrawEllipse(frame, center, size, 0, 0, 360, Scalar.BgrMagenta, thickness, LineType.Link4, 0);

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
                    Core.Cv.DrawCircle(frame, pt, 2, color, 4);
                    frame.DrawText(pt.X + 5, pt.Y, count.ToString());
                }

                if (LandmarkTransformVector != null && LandmarkRotationVector != null && LandmarkDistCoeffs != null && LandmarkCameraMatrix != null)
                {
                    Profiler.Start("FaceRectProject");
                    List<Point3D> nose_end_point3D = new List<Point3D>()
                    {
                        new Point3D(450,0,0),
                        new Point3D(0,450,0),
                        new Point3D(0,0,450),
                    };

                    double[] rv = LandmarkRotationVector;
                    double[] tv = LandmarkTransformVector;
                    double[] dc = LandmarkDistCoeffs;
                    double[,] cm = LandmarkCameraMatrix;

                    Point[] nose_end_point2D;
                    double[,] jacobia;
                    Core.Cv.ProjectPoints(nose_end_point3D, rv, tv, cm, dc, out nose_end_point2D, out jacobia);

                    Point start_nose = Landmarks[Flandmark.LandmarkNose];
                    Core.Cv.DrawLine(frame, start_nose, nose_end_point2D[0], Scalar.BgrRed, 2);
                    Core.Cv.DrawLine(frame, start_nose, nose_end_point2D[1], Scalar.BgrGreen, 2);
                    Core.Cv.DrawLine(frame, start_nose, nose_end_point2D[2], Scalar.BgrBlue, 2);

                    string msgTv = $"tv:{tv[0].ToString("0.0").PadRight(5)},{tv[1].ToString("0.0").PadRight(5)},{tv[2].ToString("0.0").PadRight(5)}";
                    string msgRv = $"rv:{rv[0].ToString("0.0").PadRight(5)},{rv[1].ToString("0.0").PadRight(5)},{rv[2].ToString("0.0").PadRight(5)}";
                    frame.DrawText(Landmarks[Flandmark.LandmarkNose].X + 35, Landmarks[Flandmark.LandmarkNose].Y, msgTv);
                    frame.DrawText(Landmarks[Flandmark.LandmarkNose].X + 35, Landmarks[Flandmark.LandmarkNose].Y + 45, msgRv);
                    Profiler.End("FaceRectProject");
                }
            }
        }

        public VMat ROI(VMat frame)
        {
            return VMat.New(frame, this);
        }
    }
}
