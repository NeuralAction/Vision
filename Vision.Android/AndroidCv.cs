using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Widget;
using App = Android.App;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgProc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Vision.Cv;

namespace Vision.Android
{
    public class AndroidCv : Vision.Cv.Cv
    {
        public static AndroidCv Cv { get { return (AndroidCv)Core.Cv; } }

        public Context AppContext { get; set; }
        private LoaderCallback LoaderCallback;
        private ImageView imageView;
        private App.Activity activity;

        public AndroidCv(Context context, App.Activity activity, ImageView imageView = null)
        {
            Logger.WriteMethod = new Logger.WriteMethodDelegate((string s)=> { Log.WriteLine(LogPriority.Debug, "Vision.Android", s); });

            AppContext = context;
            this.imageView = imageView;
            this.activity = activity;

            LoaderCallback = new LoaderCallback();
            LoaderCallback.ManagerConnected += LoaderCallback_ManagerConnected;

            LoadAssambly();
        }

        private void LoaderCallback_ManagerConnected(object sender, int e)
        {
            switch (e)
            {
                case LoaderCallbackInterface.Success:
                    Logger.Log(this, "OpenCV loaded successfully");
                    break;
                default:
                    break;
            }
        }

        public void LoadAssambly()
        {
            if (!OpenCVLoader.InitDebug())
            {
                Logger.Log(this, "Internal OpenCV library not found. Using OpenCV Manager for initialization");
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion300, AppContext, LoaderCallback);
            }
            else
            {
                Logger.Log(this, "OpenCV library found inside package. Using it!");
                LoaderCallback_ManagerConnected (this, LoaderCallbackInterface.Success);
            }
        }

        protected override string GetBuildInformation()
        {
            return OpenCV.Core.Core.BuildInformation;
        }

        public override void ConvertColor(VMat src, VMat output, ColorConversion convertMode)
        {
            Imgproc.CvtColor((Mat)src.Object, (Mat)output.Object, (int)convertMode);
        }

        public override void DrawCircle(VMat img, Point center, double radius, Scalar color, double thickness = 1, LineType lineType = LineType.Link8, int shift = 0)
        {
            Imgproc.Circle((Mat)img.Object, new OpenCV.Core.Point(center.X, center.Y), (int)Math.Round(radius), new OpenCV.Core.Scalar(color.Value1, color.Value2, color.Value3, color.Value4), (int)Math.Round(thickness), (int)lineType, shift);
        }

        public override void DrawEllipse(VMat img, Point center, Size axes, double angle, double startAngle, double endAngle, Scalar color, double thickness = 1, LineType lineType = LineType.Link8, int shift = 0)
        {
            Imgproc.Ellipse((Mat)img.Object, new OpenCV.Core.Point(center.X, center.Y), new OpenCV.Core.Size(axes.Width, axes.Height), angle, startAngle, endAngle, new OpenCV.Core.Scalar(color.Value1, color.Value2, color.Value3, color.Value4), (int)Math.Round(thickness), (int)lineType, shift);
        }

        public override void DrawLine(VMat img, Point start, Point end, Scalar color, int thickness = 1, LineType lineType = LineType.Link8, int shift = 0)
        {
            Imgproc.Line(img.ToCvMat(), start.ToCvPoint(), end.ToCvPoint(), color.ToCvScalar(), thickness, (int)lineType, shift);
        }

        public override void DrawText(VMat img, string text, Point org, FontFace fontFace, double fontScale, Scalar color, int thickness = 1, LineType lineType = LineType.Link8, bool bottomLeftOrigin = false)
        {
            Imgproc.PutText((Mat)img.Object, text, new OpenCV.Core.Point(org.X, org.Y), (int)fontFace, fontScale,
                new OpenCV.Core.Scalar(color.Value1, color.Value2, color.Value3, color.Value4), thickness, (int)lineType, bottomLeftOrigin);
        }

        public override void EqualizeHistogram(VMat input, VMat output)
        {
            Imgproc.EqualizeHist((Mat)input.Object, (Mat)output.Object);
        }

        public override void Transpose(VMat input, VMat output)
        {
            OpenCV.Core.Core.Transpose((Mat)input.Object, (Mat)output.Object);
        }

        public override void ImgShow(string name, VMat img)
        {
            if(imageView != null)
            {
                Bitmap bit = Bitmap.CreateBitmap((int)img.Width, (int)img.Height, Bitmap.Config.Argb8888);

                VMat mat = VMat.New();

                img.ConvertColor(mat, ColorConversion.BgrToRgb);

                Utils.MatToBitmap((Mat)mat.Object, bit);

                activity.RunOnUiThread(() =>
                {
                    imageView.SetImageBitmap(bit);
                });

                mat.Dispose();
            }
        }

        public override char WaitKey(int duration)
        {
            System.Threading.Thread.Sleep(duration);

            return (char)0;
        }

        public override void CloseWindow(string name)
        {

        }

        public override void CloseAllWindows()
        {

        }

        public void Dispose()
        {

        }

        public override void Canny(VMat Input, VMat output, double thresold1, double thresold2, int apertureSize = 3, bool L2gradient = false)
        {
            OpenCV.ImgProc.Imgproc.Canny((Mat)Input.Object, (Mat)output.Object, thresold1, thresold2, apertureSize, L2gradient);
        }

        public override void ProjectPoints(List<Point3D> objectPoints, double[] rvec, double[] tvec, double[,] cameraMatrix, double[] distCoeffs, out Point[] imagePoints, out double[,] jacobian)
        {
            Point3[] objpoints_buffer = new Point3[objectPoints.Count];
            for (int i = 0; i < objectPoints.Count; i++)
            {
                var pt = objectPoints[i];
                objpoints_buffer[i] = new Point3(pt.X, pt.Y, pt.Z);
            }

            using (MatOfPoint3f objpoints = new MatOfPoint3f(objpoints_buffer))
            using (Mat rvecMat = Converter.ToCvMat(3, 1, rvec))
            using (Mat tvecMat = Converter.ToCvMat(3, 1, tvec))
            using (Mat cameraMatrixMat = Converter.ToCvMat(3, 3, cameraMatrix))
            using (MatOfDouble distCoeffsMat = new MatOfDouble(distCoeffs))
            using (MatOfPoint2f imgpoints = new MatOfPoint2f())
            using (Mat jacobianMat = new Mat())
            {
                OpenCV.Calib3d.Calib3d.ProjectPoints(objpoints, rvecMat, tvecMat, cameraMatrixMat, distCoeffsMat, imgpoints, jacobianMat, 0);

                OpenCV.Core.Point[] ret = imgpoints.ToArray();
                imagePoints = new Point[ret.Length];
                for (int i = 0; i < ret.Length; i++)
                {
                    imagePoints[i] = new Point(ret[i].X, ret[i].Y);
                }
                jacobian = null;
            }
        }

        public override void SolvePnP(List<Point3D> model_points, List<Point> image_point, double[,] cameraMatrix, double[] distCoeffs, out double[] rvec, out double[] tvec)
        {
            Point3[] modelpt_buffer = new Point3[model_points.Count];
            for(int i=0; i<model_points.Count; i++)
            {
                var pt = model_points[i];
                modelpt_buffer[i] = new Point3(pt.X, pt.Y, pt.Z);
            }
            OpenCV.Core.Point[] imagept_buffer = new OpenCV.Core.Point[image_point.Count];
            for (int i = 0; i < imagept_buffer.Length; i++)
            {
                var pt = image_point[i];
                imagept_buffer[i] = new OpenCV.Core.Point(pt.X, pt.Y);
            }

            using (MatOfPoint3f modelpt = new MatOfPoint3f(modelpt_buffer))
            using (MatOfPoint2f imagept = new MatOfPoint2f(imagept_buffer))
            using (Mat cameraMatrixMat = Converter.ToCvMat(3, 3, cameraMatrix))
            using (MatOfDouble distCoeffsMat = new MatOfDouble(distCoeffs))
            using (Mat rvecMat = new Mat())
            using (Mat tvecMat = new Mat())
            {
                OpenCV.Calib3d.Calib3d.SolvePnP(modelpt, imagept, cameraMatrixMat, distCoeffsMat, rvecMat, tvecMat);

                rvec = new double[3];
                rvecMat.Get(0, 0, rvec);
                tvec = new double[3];
                tvecMat.Get(0, 0, tvec);
            }
        }

        public override void Resize(VMat input, VMat dist, Size size, double fx = 0, double fy = 0, Interpolation inter = Interpolation.Linear)
        {
            Imgproc.Resize((Mat)input.Object, (Mat)dist.Object, new OpenCV.Core.Size(size.Width, size.Height), fx, fy, (int)inter);
        }

        protected override Cv.CLAHE CreateCLAHE(double clip, Size gridSize)
        {
            return new AndroidCLAHE(clip, gridSize);
        }

        protected override Capture CreateCapture(int index)
        {
            return new AndroidCapture(index);
        }

        protected override Capture CreateCapture(string filePath)
        {
            return new AndroidCapture(filePath);
        }

        protected override CascadeClassifier CreateCascadeClassifier(string filePath)
        {
            return new AndroidCascadeClassifier(filePath);
        }

        protected override VMat CreateMat()
        {
            return new AndroidMat();
        }

        protected override VMat CreateMat(Size size)
        {
            return new AndroidMat(size);
        }

        protected override VMat CreateMat(Size size, MatType type)
        {
            return new AndroidMat(size, type);
        }

        protected override VMat CreateMat(VMat mat, Rect rect)
        {
            return new AndroidMat(mat, rect);
        }

        protected override VMat InternalImgRead(string path)
        {
            return new AndroidMat(OpenCV.ImgCodecs.Imgcodecs.Imread(path));
        }

        protected override void InternalImgWrite(string name, VMat img, int quality)
        {
            using (MatOfInt mat = new MatOfInt())
            {
                mat.Put(0, 0, new int[] { OpenCV.ImgCodecs.Imgcodecs.ImwriteJpegQuality, quality });
                OpenCV.ImgCodecs.Imgcodecs.Imwrite(name, (Mat)img.Object, mat);
            }
        }
    }
}
