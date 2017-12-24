using MathNet.Numerics.LinearAlgebra;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;
using Vision.Tensorflow;

namespace Vision.Detection
{
    public enum EyeDirection
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3,
        None = -1
    }

    public enum EyeGazeDetectMode
    {
        LeftOnly = 0,
        Both = 1,
        Face = 2,
        FaceMobile = 3,
    }

    public class PointSmoother
    {
        public enum SmoothMethod
        {
            Kalman,
            Mean,
            MeanKalman
        }

        public int QueueCount { get; set; } = 6;
        public SmoothMethod Method { get; set; } = SmoothMethod.MeanKalman;

        PointKalmanFilter kalman = new PointKalmanFilter();
        Queue<Point> q = new Queue<Point>();

        public Point Smooth(Point pt)
        {
            Point ret = pt.Clone();

            q.Enqueue(ret.Clone());
            if (q.Count > QueueCount)
                q.Dequeue();

            if (Method == SmoothMethod.Mean || Method == SmoothMethod.MeanKalman)
            {
                var arr = q.ToArray();
                var xarr = arr.OrderBy((a) => { return a.X; }).ToArray();
                var yarr = arr.OrderBy((a) => { return a.Y; }).ToArray();
                var x = 0.0;
                var y = 0.0;
                var cc = 0.0;
                var count = Math.Max(1, (double)q.Count / 2);
                var start = Math.Round((double)q.Count / 2 - count / 2);
                for (int i = (int)start; i < count; i++)
                {
                    x += xarr[i].X;
                    y += yarr[i].Y;
                    cc++;
                }
                x /= cc;
                y /= cc;
                ret.X = x;
                ret.Y = y;
            }

            if(Method == SmoothMethod.MeanKalman || Method == SmoothMethod.Kalman)
                ret = kalman.Calculate(ret);

            return ret;
        }
    }

    public class EyeGazeDetector : IDisposable
    {
        public const int ImageSize = 60;
        public const int ImageSizeEx = 60;
        public const int ImageSizeFace = 60;
        public const int FaceSizeFace = 60;
        public const int ImageSizeFaceMobile = 64;
        public const int FaceSizeFaceMobile = 64;
        public const double AngleMul = 1;
        public const double DefaultSensitiveX = 1;
        public const double DefaultSensitiveY = 1;
        public const double DefaultOffsetX = 0;
        public const double DefaultOffsetY = 0;

        public readonly static ManifestResource ModelResourceExtend = new ManifestResource("Vision.Detection", "frozen_gazeEx.pb");
        public readonly static ManifestResource ModelResourceSingle = new ManifestResource("Vision.Detection", "frozen_gaze.pb");
        public readonly static ManifestResource ModelResourceFace = new ManifestResource("Vision.Detection", "frozen_gazeFace.pb");
        public readonly static ManifestResource ModelResourceFaceMobile = new ManifestResource("Vision.Detection", "frozen_gazeFaceMobile.pb");

        public static object ModelLocker = new object();
        public static Graph ModelGraphSingle;
        public static Graph ModelGraphExtend;
        public static Graph ModelGraphFace;
        public static Graph ModelGraphFaceMobile;

        static EyeGazeDetector()
        {
            Logger.Log("EyeGazeDetector", "Start model load");

            ModelGraphSingle = new Graph();
            ModelGraphSingle.ImportPb(Storage.LoadResource(ModelResourceSingle, true));

            ModelGraphExtend = new Graph();
            ModelGraphExtend.ImportPb(Storage.LoadResource(ModelResourceExtend, true));

            ModelGraphFace = new Graph();
            ModelGraphFace.ImportPb(Storage.LoadResource(ModelResourceFace, true));

            ModelGraphFaceMobile = new Graph();
            ModelGraphFaceMobile.ImportPb(Storage.LoadResource(ModelResourceFaceMobile, true));

            Logger.Log("EyeGazeDetector", "Finished model load");
        }

        public bool ClipToBound { get; set; } = false;
        public bool UseSmoothing { get; set; } = false;
        public PointSmoother Smoother { get; set; } = new PointSmoother();

        public bool UseModification { get; set; } = true;
        public double SensitiveX { get; set; } = DefaultSensitiveX;
        public double OffsetX { get; set; } = DefaultOffsetX;
        public double SensitiveY { get; set; } = DefaultSensitiveY;
        public double OffsetY { get; set; } = DefaultOffsetY;
        
        public ScreenProperties ScreenProperties { get; set; }

        public bool UseCalibrator { get; set; } = true;
        public EyeGazeCalibrater Calibrator { get; set; }

        public EyeGazeDetectMode DetectMode { get; set; } = EyeGazeDetectMode.FaceMobile;

        Session sess;
        Session sessEx;
        Session sessFace;
        Session sessFaceMobile;
        float[] imgBufferLeft;
        float[] imgBufferRight;
        float[] imgBufferFace;

        public EyeGazeDetector(ScreenProperties screen)
        {
            ScreenProperties = screen ?? throw new ArgumentNullException("screen properites");

            sess = new Session(ModelGraphSingle);
            sessEx = new Session(ModelGraphExtend);
            sessFace = new Session(ModelGraphFace);
            sessFaceMobile = new Session(ModelGraphFaceMobile);

            Calibrator = new EyeGazeCalibrater();
        }

        public Point Detect(FaceRect face, Mat frame)
        {
            var properties = ScreenProperties;

            if (face == null)
                throw new ArgumentNullException("face");
            if (frame == null || frame.IsEmpty)
                throw new ArgumentNullException("frame");
            if (properties == null)
                throw new ArgumentNullException("properties");

            switch (DetectMode)
            {
                case EyeGazeDetectMode.LeftOnly:
                    if (face.LeftEye == null)
                        return null;
                    break;
                case EyeGazeDetectMode.FaceMobile:
                case EyeGazeDetectMode.Face:
                case EyeGazeDetectMode.Both:
                    if (face.LeftEye == null || face.RightEye == null)
                        return null;
                    break;
                default:
                    throw new NotImplementedException();
            }

            Profiler.Start("GazeDetect");

            Point vecPt = null;
            Point result = new Point(0,0);
            Point pt = new Point(0, 0);
            lock (ModelLocker)
            {
                switch (DetectMode)
                {
                    case EyeGazeDetectMode.LeftOnly:
                        using (Mat left = face.LeftEye.RoiCropByPercent(frame))
                            result = DetectLeftEyes(left);
                        break;
                    case EyeGazeDetectMode.Both:
                        using (Mat left = face.LeftEye.RoiCropByPercent(frame))
                        using (Mat right = face.RightEye.RoiCropByPercent(frame))
                            result = DetectBothEyes(left, right);
                        break;
                    case EyeGazeDetectMode.FaceMobile:
                    case EyeGazeDetectMode.Face:
                        using (Mat left = face.LeftEye.RoiCropByPercent(frame, .25))
                        using (Mat right = face.RightEye.RoiCropByPercent(frame, .25))
                        using (Mat faceRoi = face.ROI(frame))
                            result = DetectFace(faceRoi, left, right);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                var x = result.X / AngleMul * -1;
                var y = result.Y / AngleMul * -1;
                if (UseModification)
                {
                    x = (x + OffsetX) * SensitiveX;
                    y = (y + OffsetY) * SensitiveY;
                }

                vecPt = new Point(x, y);
                if (UseSmoothing)
                    vecPt = Smoother.Smooth(vecPt);

                Vector<double> vec = CreateVector.Dense(new double[] { vecPt.X, vecPt.Y, -1 });
                pt = face.SolveRayScreenVector(new Point3D(vec.ToArray()), properties);
            }

            if (ClipToBound)
            {
                pt.X = Util.Clamp(pt.X, 0, ScreenProperties.PixelSize.Width);
                pt.Y = Util.Clamp(pt.Y, 0, ScreenProperties.PixelSize.Height);
            }

            face.GazeInfo = new EyeGazeInfo()
            {
                ScreenPoint = pt,
                Vector = new Point3D(vecPt.X, vecPt.Y, -1),
                ClipToBound = ClipToBound,
            };

            Calibrator.Push(new CalibratingPushData(face));
            if (UseCalibrator)
                Calibrator.Apply(face, ScreenProperties);

            Profiler.End("GazeDetect");
            return face.GazeInfo.ScreenPoint;
        }

        private Point DetectFace(Mat face, Mat left, Mat right)
        {
            int modelImgSize, modelFaceSize;
            Session sess;
            if(DetectMode == EyeGazeDetectMode.Face)
            {
                modelImgSize = ImageSizeFace;
                modelFaceSize = FaceSizeFace;
                sess = sessFace;
            }
            else if(DetectMode == EyeGazeDetectMode.FaceMobile)
            {
                modelImgSize = ImageSizeFaceMobile;
                modelFaceSize = FaceSizeFaceMobile;
                sess = sessFaceMobile;
            }
            else { throw new NotImplementedException("unknown mode"); }

            Profiler.Start("Gaze.Face.Cvt.Resize");
            var imgSize = new Size(modelImgSize, modelImgSize);
            var imgSizeFace = new Size(modelFaceSize, modelFaceSize);
            left.Resize(imgSize);
            right.Resize(imgSize);
            face.Resize(imgSizeFace);
            Profiler.End("Gaze.Face.Cvt.Resize");

            var bufferSize = modelImgSize * modelImgSize * 3;
            var bufferFace = modelFaceSize * modelFaceSize * 3;
            if (imgBufferLeft == null || imgBufferLeft.Length != bufferSize)
                imgBufferLeft = new float[bufferSize];
            if (imgBufferRight == null || imgBufferRight.Length != bufferSize)
                imgBufferRight = new float[bufferSize];
            if (imgBufferFace == null || imgBufferFace.Length != bufferFace)
                imgBufferFace = new float[bufferFace];

            Profiler.Start("Gaze.Face.Cvt");
            var imgTensorLeft = Tools.MatBgr2Tensor(left, NormalizeMode.CenterZero, -1, -1, new long[] { 1, modelImgSize, modelImgSize, 3 }, imgBufferLeft);
            var imgTensorRight = Tools.MatBgr2Tensor(right, NormalizeMode.CenterZero, -1, -1, new long[] { 1, modelImgSize, modelImgSize, 3 }, imgBufferRight);
            var imgTensorFace = Tools.MatBgr2Tensor(face, NormalizeMode.CenterZero, -1, -1, new long[] { 1, modelFaceSize, modelFaceSize, 3 }, imgBufferFace);
            Profiler.End("Gaze.Face.Cvt");

            Profiler.Start("Gaze.Face.Sess");
            Tensor[] fetch = sess.Run(new[] { "output" },
                new Dictionary<string, Tensor>() { { "input_image", imgTensorLeft }, { "input_image_r", imgTensorRight }, { "input_image_f", imgTensorFace }, { "phase_train", new Tensor(false) }, { "keep_prob", new Tensor(0.0f) } });
            Profiler.End("Gaze.Face.Sess");

            var result = fetch[0];
            float[,] output = (float[,])result.GetValue();

            Profiler.Start("Gaze.Face.Dispose");
            try
            {
                foreach (Tensor t in fetch)
                    t.Dispose();
                fetch = null;
                imgTensorLeft.Dispose();
                imgTensorLeft = null;
                imgTensorRight.Dispose();
                imgTensorRight = null;
                imgTensorFace.Dispose();
                imgTensorFace = null;
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
                throw;
            }
            Profiler.End("Gaze.Face.Dispose");

            return new Point(output[0, 0], output[0, 1]);
        }

        private Point DetectBothEyes(Mat left, Mat right)
        {
            var imgSize = new Size(ImageSizeEx, ImageSizeEx);
            left.Resize(imgSize, 0, 0);
            right.Resize(imgSize, 0, 0);

            var bufferSize = ImageSizeEx * ImageSizeEx * 3;
            if (imgBufferLeft == null || imgBufferLeft.Length != bufferSize)
                imgBufferLeft = new float[bufferSize];
            if (imgBufferRight == null || imgBufferRight.Length != bufferSize)
                imgBufferRight = new float[bufferSize];

            var imgTensorLeft = Tools.MatBgr2Tensor(left, NormalizeMode.ZeroMean, -1, -1, new long[] { 1, ImageSizeEx, ImageSizeEx, 3 }, imgBufferLeft);
            var imgTensorRight = Tools.MatBgr2Tensor(right, NormalizeMode.ZeroMean, -1, -1, new long[] { 1, ImageSizeEx, ImageSizeEx, 3 }, imgBufferRight);

            Tensor[] fetch = sessEx.Run(new[] { "output" },
                new Dictionary<string, Tensor>() { { "input_image", imgTensorLeft }, { "input_image_r", imgTensorRight }, { "phase_train", new Tensor(false) }, { "keep_prob", new Tensor(1.0f) } });

            var result = fetch[0];
            float[,] output = (float[,])result.GetValue();

            try
            {
                foreach (Tensor t in fetch)
                    t.Dispose();
                fetch = null;
                imgTensorLeft.Dispose();
                imgTensorLeft = null;
                imgTensorRight.Dispose();
                imgTensorRight = null;
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
                throw;
            }

            return new Point(output[0, 0], output[0, 1]);
        }

        private Point DetectLeftEyes(Mat mat)
        {
            mat.Resize(new Size(ImageSize, ImageSize), 0, 0);

            var bufferSize = ImageSize * ImageSize * 3;
            if (imgBufferLeft == null || imgBufferLeft.Length != bufferSize)
                imgBufferLeft = new float[bufferSize];
            var imgTensorLeft = Tools.MatBgr2Tensor(mat, NormalizeMode.ZeroMean, -1, -1, new long[] { 1, ImageSize, ImageSize, 3 }, imgBufferLeft);
            Tensor[] fetch = sess.Run(new [] { "output" },
                new Dictionary<string, Tensor>() { { "input_image", imgTensorLeft }, { "phase_train", new Tensor(false) }, { "keep_prob", new Tensor(1.0f) } });

            Tensor result = fetch[0];
            float[,] output = (float[,])result.GetValue();

            try
            {
                foreach (Tensor t in fetch)
                    t.Dispose();
                fetch = null;
                imgTensorLeft.Dispose();
                imgTensorLeft = null;
            }
            catch (Exception ex)
            {
                Logger.Error(this, "Error while disposing tensors\n" + ex.ToString());
            }

            return new Point(output[0, 0], output[0, 1]);
        }

        public void Dispose()
        {
            if (imgBufferLeft != null)
            {
                imgBufferLeft = null;
            }

            if(imgBufferRight != null)
            {
                imgBufferRight = null;
            }

            if(imgBufferFace != null)
            {
                imgBufferFace = null;
            }

            if (sess != null)
            {
                sess.Dispose();
                sess = null;
            }

            if(sessEx != null)
            {
                sessEx.Dispose();
                sessEx = null;
            }

            if(sessFace != null)
            {
                sessFace.Dispose();
                sessFace = null;
            }
        }
    }
}