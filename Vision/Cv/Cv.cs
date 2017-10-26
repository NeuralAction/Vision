using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Cv
{
    public abstract class Cv
    {
        //Properties
        private static Cv _context;
        public static Cv Context
        {
            get
            {
                return _context;
            }
        }

        public virtual string BuildInformation => GetBuildInformation();
        public virtual int NumThreads { get => GetNumThreads(); set => SetNumThreads(value); }
        public virtual bool UseOptimized { get => GetUseOptimized(); set => SetUseOptimized(value); }

        //Get
        protected abstract string GetBuildInformation();
        protected abstract int GetNumThreads();
        protected abstract void SetNumThreads(int t);
        protected abstract bool GetUseOptimized();
        protected abstract void SetUseOptimized(bool b);

        internal protected abstract Capture CreateCapture(int index);
        internal protected abstract Capture CreateCapture(string filePath);

        //Global Functions
        internal static void Init(Cv Context)
        {
            if (_context == null)
            {
                _context = Context;

                Logger.Log("Cv", $"Context is created. New Context: [{Context.ToString()}]");
                Logger.Log("Cv", $"NumThreads: {Context.NumThreads}");
                Logger.Log("Cv", $"UseOptimized: {Context.UseOptimized}");
            }
            else
            {
                Logger.Error("Cv", "Context is already created");
            }
        }

        protected abstract void InternalImgShow(string name, Mat img);
        public void ImgShow(string name, Mat img)
        {
            try
            {
                if (name == null)
                    throw new ArgumentNullException();
                if (img == null || img.IsEmpty)
                    throw new ArgumentNullException();
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Error(this, "img is disposed");
                return;
            }

            InternalImgShow(name, img);
        }

        protected abstract Mat InternalImgRead(string path);
        public Mat ImgRead(FileNode node)
        {
            return ImgRead(node.AbosolutePath);
        }
        public Mat ImgRead(string path)
        {
            Logger.Log(this, "Image Read : " + path);

            return InternalImgRead(path);
        }

        protected abstract void InternalImgWrite(string name, Mat img, int quality);
        public void ImgWrite(FileNode node, Mat img, int quality = 80)
        {
            ImgWrite(node.AbosolutePath, img, quality);
        }
        public void ImgWrite(string path, Mat img, int quality = 80)
        {
            Logger.Log(this, "Image Write : " + path);

            InternalImgWrite(path, img, quality);
        }

        public abstract void CloseWindow(string name);
        public abstract void CloseAllWindows();
        public abstract char WaitKey(int duration);

        //Draw
        public void DrawRectangle(Mat self, Rect rect, Scalar color, double thickness = 1, LineTypes lineType = LineTypes.Link8, int shift = 0)
        {
            Cv2.Rectangle(self, rect.ToCvRect(), color.ToCvScalar(), (int)thickness, lineType, shift);
        }

        public void DrawCircle(Mat img, Point center, double radius, Scalar color, double thickness = 1, LineTypes lineType = LineTypes.Link8, int shift = 0)
        {
            Cv2.Circle(img,
                new OpenCvSharp.Point(center.X, center.Y), (int)Math.Round(radius),
                new OpenCvSharp.Scalar(color.Value1, color.Value2, color.Value3),
                (int)Math.Round(thickness), lineType, shift);
        }

        public void DrawEllipse(Mat img, Point center, Size axes, double angle, double startAngle, double endAngle, Scalar color, double thickness = 1, LineTypes lineType = LineTypes.Link8, int shift = 0)
        {
            Cv2.Ellipse(img,
                new OpenCvSharp.Point(center.X, center.Y),
                new OpenCvSharp.Size(axes.Width, axes.Height),
                angle, startAngle, endAngle,
                new OpenCvSharp.Scalar(color.Value1, color.Value2, color.Value3),
                (int)Math.Round(thickness), lineType, shift);
        }

        public void DrawText(Mat img, string text, Point org, HersheyFonts fontFace, double fontScale, Scalar color, int thickness = 1, LineTypes lineType = LineTypes.AntiAlias, bool bottomLeftOrigin = false)
        {
            string[] lines;
            if (text.Contains("\n"))
            {
                lines = text.Split('\n');
            }
            else
            {
                lines = new string[] { text };
            }

            var pt = org.Clone();
            foreach (var line in lines)
            {
                Cv2.PutText(img, line, pt.ToCvPoint(), fontFace, fontScale, color.ToCvScalar(), thickness, lineType, bottomLeftOrigin);
                pt.Y += 35;
            }
        }

        public void DrawLine(Mat img, Point start, Point end, Scalar color, int thickness = 1, LineTypes lineType = LineTypes.Link8, int shift = 0)
        {
            Cv2.Line(img, start.ToCvPoint(), end.ToCvPoint(), color.ToCvScalar(), thickness, lineType, shift);
        }

        public void DrawMatAlpha(Mat target, Mat img, Point pt, double alpha = 1)
        {
            using(Mat roi = new Mat(target, new OpenCvSharp.Rect((int)pt.X, (int)pt.Y, img.Width, img.Height)))
            {
                Cv2.AddWeighted(roi, 1 - alpha, img, alpha, 0, roi);
            }
        }

        //Math
        public void ProjectPoints(List<Point3D> objectPoints, double[] rvec, double[] tvec, double[,] cameraMatrix, double[] distCoeffs, out Point[] imagePoints, out double[,] jacobian)
        {
            List<Point3f> objpoints = new List<Point3f>(objectPoints.Count);
            foreach (var pt in objectPoints)
                objpoints.Add(new Point3f((float)pt.X, (float)pt.Y, (float)pt.Z));

            Point2f[] point2D;
            Cv2.ProjectPoints(objpoints, rvec, tvec, cameraMatrix, distCoeffs, out point2D, out jacobian);

            imagePoints = new Point[point2D.Length];
            for (int i = 0; i < point2D.Length; i++)
                imagePoints[i] = new Point(point2D[i].X, point2D[i].Y);
        }

        public void SolvePnP(List<Point3D> model_points, List<Point> image_point, double[,] cameraMatrix, double[] distCoeffs, out double[] rvec, out double[] tvec)
        {
            List<Point3f> modelpoints = new List<Point3f>(model_points.Count);
            foreach (var pt in model_points)
                modelpoints.Add(new Point3f((float)pt.X, (float)pt.Y, (float)pt.Z));

            List<Point2f> imagepoints = new List<Point2f>(image_point.Count);
            foreach (var pt in image_point)
                imagepoints.Add(new Point2f((float)pt.X, (float)pt.Y));

            using (Mat rotation_vector = new Mat())
            using (Mat translation_vector = new Mat())
            {
                Cv2.SolvePnP(InputArray.Create(modelpoints), InputArray.Create(imagepoints), InputArray.Create(cameraMatrix), InputArray.Create(distCoeffs), rotation_vector, translation_vector);

                rvec = new double[3];
                rotation_vector.GetArray(0, 0, rvec);
                tvec = new double[3];
                translation_vector.GetArray(0, 0, tvec);
            }
        }

        public void Rodrigues(double[] vector, out double[,] matrix)
        {
            double[,] jacobian;
            Rodrigues(vector, out matrix, out jacobian);
        }

        public void Rodrigues(double[] vector, out double[,] matrix, out double[,] jacobian)
        {
            Cv2.Rodrigues(vector, out matrix, out jacobian);
        }

        public void Rodrigues(double[,] matrix, out double[] vector)
        {
            double[,] jacobian;
            Rodrigues(matrix, out vector, out jacobian);
        }

        public void Rodrigues(double[,] matrix, out double[] vector, out double[,] jacobian)
        {
            Cv2.Rodrigues(matrix, out vector, out jacobian);
        }

        public double RodriguesTheta(double[] vector)
        {
            if (vector == null)
                throw new ArgumentNullException();
            if (vector.Length != 3)
                throw new ArgumentOutOfRangeException();

            return Math.Sqrt(vector[0] * vector[0] + vector[1] * vector[1] + vector[2] * vector[2]);
        }

        public double[] RodriguesVector(double[] vector)
        {
            if (vector == null)
                throw new ArgumentNullException();
            if (vector.Length != 3)
                throw new ArgumentOutOfRangeException();

            double theta = RodriguesTheta(vector);

            return new double[] { vector[0] / theta, vector[1] / theta, vector[2] / theta };
        }

        public void WarpPerspective(Mat src, Mat dst, Mat transform, Size dsize, InterpolationFlags flags = InterpolationFlags.Linear, BorderTypes borderMode = BorderTypes.Constant, Scalar borderValue = null)
        {
            OpenCvSharp.Scalar? s = null;
            if (borderValue != null)
                s = borderValue.ToCvScalar();
            Cv2.WarpPerspective(src, dst, transform, dsize.ToCvSize(), flags, borderMode, s);
        }
    }
}
