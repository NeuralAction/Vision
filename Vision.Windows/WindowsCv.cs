using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision;
using Vision.Cv;

namespace Vision.Windows
{
    public class WindowsCv : Vision.Cv.Cv
    {
        public WindowsCv()
        {

        }

        protected override string GetBuildInformation()
        {
            return Cv2.GetBuildInformation();
        }

        public override void ConvertColor(VMat src, VMat output, ColorConversion convertMode)
        {
            Cv2.CvtColor((Mat)src.Object, (Mat)output.Object, (ColorConversionCodes)convertMode);
        }

        public override void CloseAllWindows()
        {
            Cv2.DestroyAllWindows();
        }

        public override void CloseWindow(string name)
        {
            Cv2.DestroyWindow(name);
        }

        public override void DrawCircle(VMat img, Point center, double radius, Scalar color, double thickness = 1, LineType lineType = LineType.Link8, int shift = 0)
        {
            Cv2.Circle((Mat)img.Object, 
                new OpenCvSharp.Point(center.X, center.Y), (int)Math.Round(radius), 
                new OpenCvSharp.Scalar(color.Value1, color.Value2, color.Value3), 
                (int)Math.Round(thickness), (LineTypes)lineType, shift);
        }

        public override void DrawEllipse(VMat img, Point center, Size axes, double angle, double startAngle, double endAngle, Scalar color, double thickness = 1, LineType lineType = LineType.Link8, int shift = 0)
        {
            Cv2.Ellipse((Mat)img.Object, 
                new OpenCvSharp.Point(center.X, center.Y), 
                new OpenCvSharp.Size(axes.Width, axes.Height), 
                angle, startAngle, endAngle, 
                new OpenCvSharp.Scalar(color.Value1, color.Value2, color.Value3), 
                (int)Math.Round(thickness), (LineTypes)lineType, shift);
        }

        public override void DrawText(VMat img, string text, Point org, FontFace fontFace, double fontScale, Scalar color, int thickness = 1, LineType lineType = LineType.Link8, bool bottomLeftOrigin = false)
        {
            Cv2.PutText((Mat)img.Object, text, new OpenCvSharp.Point(org.X, org.Y), (HersheyFonts)fontFace, fontScale,
                new OpenCvSharp.Scalar(color.Value1, color.Value2, color.Value3, color.Value4), thickness, (LineTypes)lineType, bottomLeftOrigin);
        }

        public override void DrawLine(VMat img, Point start, Point end, Scalar color, int thickness = 1, LineType lineType = LineType.Link8, int shift = 0)
        {
            Cv2.Line(img.ToCvMat(), start.ToCvPoint(), end.ToCvPoint(), color.ToCvScalar(), thickness, (LineTypes)lineType, shift);
        }

        public override void EqualizeHistogram(VMat input, VMat output)
        {
            Cv2.EqualizeHist((Mat)input.Object, (Mat)input.Object);
        }

        public override void Canny(VMat Input, VMat output, double thresold1, double thresold2, int apertureSize = 3, bool L2gradient = false)
        {
            Cv2.Canny((Mat)Input.Object, (Mat)output.Object, thresold1, thresold2, apertureSize, L2gradient);
        }

        public override void Transpose(VMat input, VMat output)
        {
            Cv2.Transpose((Mat)input.Object, (Mat)output.Object);
        }

        public override void Resize(VMat input, VMat dist, Size size, double fx = 0, double fy = 0, Interpolation inter = Interpolation.Linear)
        {
            Cv2.Resize((Mat)input.Object, (Mat)dist.Object, new OpenCvSharp.Size(size.Width, size.Height), fx, fy, (InterpolationFlags)inter);
        }

        public override void ProjectPoints(List<Point3D> objectPoints, double[] rvec, double[] tvec, double[,] cameraMatrix, double[] distCoeffs, out Point[] imagePoints, out double[,] jacobian)
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

        public override void SolvePnP(List<Point3D> model_points, List<Point> image_point, double[,] cameraMatrix, double[] distCoeffs, out double[] rvec, out double[] tvec)
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

        public override void ImgShow(string name, VMat img)
        {
            Cv2.ImShow(name, (Mat)img.Object);
        }

        public override char WaitKey(int duration)
        {
            return (char)Cv2.WaitKey(duration);
        }

        protected override Cv.CLAHE CreateCLAHE(double clip, Size gridSize)
        {
            return new WindowsCLAHE(clip, gridSize);
        }

        protected override Capture CreateCapture(int index)
        {
            return new WindowsCapture(index);
        }

        protected override Capture CreateCapture(string filePath)
        {
            return new WindowsCapture(filePath);
        }

        protected override Cv.CascadeClassifier CreateCascadeClassifier(string filePath)
        {
            return new WindowsCascadeClassifier(filePath);
        }

        protected override VMat CreateMat()
        {
            return new WindowsMat();
        }

        protected override VMat CreateMat(Size size)
        {
            return new WindowsMat(size);
        }

        protected override VMat CreateMat(Size size, Cv.MatType type)
        {
            return new WindowsMat(size, type);
        }

        protected override VMat CreateMat(VMat mat, Rect rect)
        {
            return new WindowsMat(mat, rect);
        }

        protected override VMat InternalImgRead(string path)
        {
            return new WindowsMat(Cv2.ImRead(path));
        }

        protected override void InternalImgWrite(string name, VMat img, int quality)
        {
            Cv2.ImWrite(name, (Mat)img.Object, new ImageEncodingParam(ImwriteFlags.JpegQuality, 80));
        }
    }
}
