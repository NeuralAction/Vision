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
        Face = 2
    }

    public class EyeGazeDetector : IDisposable
    {
        public const int ImageSize = 60;
        public const int ImageSizeEx = 60;
        public const int ImageSizeFace = 120;
        public const int FaceSizeFace = 120;
        public const double AngleMul = 1;
        public const double DefaultSensitiveX = 1;
        public const double DefaultSensitiveY = 1;
        public const double DefaultOffsetX = 0;
        public const double DefaultOffsetY = 0;

        public readonly static ManifestResource ModelResourceExtend = new ManifestResource("Vision.Detection", "frozen_gazeEx.pb");
        public readonly static ManifestResource ModelResourceSingle = new ManifestResource("Vision.Detection", "frozen_gaze.pb");
        public readonly static ManifestResource ModelResourceFace = new ManifestResource("Vision.Detection", "frozen_gazeFace.pb");

        public static object ModelLocker = new object();
        public static Graph ModelGraphSingle;
        public static Graph ModelGraphExtend;
        public static Graph ModelGraphFace;

        static EyeGazeDetector()
        {
            Logger.Log("EyeGazeDetector", "Start model load");

            ModelGraphSingle = new Graph();
            ModelGraphSingle.ImportPb(Storage.LoadResource(ModelResourceSingle, true));

            ModelGraphExtend = new Graph();
            ModelGraphExtend.ImportPb(Storage.LoadResource(ModelResourceExtend, true));

            ModelGraphFace = new Graph();
            ModelGraphFace.ImportPb(Storage.LoadResource(ModelResourceFace, true));

            Logger.Log("EyeGazeDetector", "Finished model load");
        }

        public bool ClipToBound { get; set; } = false;
        public bool UseSmoothing { get; set; } = false;

        public bool UseModification { get; set; } = true;
        public double SensitiveX { get; set; } = DefaultSensitiveX;
        public double OffsetX { get; set; } = DefaultOffsetX;
        public double SensitiveY { get; set; } = DefaultSensitiveY;
        public double OffsetY { get; set; } = DefaultOffsetY;
        
        public ScreenProperties ScreenProperties { get; set; }

        public EyeGazeDetectMode DetectMode { get; set; } = EyeGazeDetectMode.Both;

        Session sess;
        Session sessEx;
        Session sessFace;
        PointKalmanFilter kalman = new PointKalmanFilter();
        float[] imgBufferLeft;
        float[] imgBufferRight;
        float[] imgBufferFace;

        public EyeGazeDetector(ScreenProperties screen)
        {
            ScreenProperties = screen ?? throw new ArgumentNullException("screen properites");

            sess = new Session(ModelGraphSingle);
            sessEx = new Session(ModelGraphExtend);
            sessFace = new Session(ModelGraphFace);
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
                    vecPt = kalman.Calculate(vecPt);

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
            };

            Profiler.End("GazeDetect");
            return pt;
        }

        private Point DetectFace(Mat face, Mat left, Mat right)
        {
            var imgSize = new Size(ImageSizeFace, ImageSizeFace);
            var imgSizeFace = new Size(FaceSizeFace, FaceSizeFace);
            left.Resize(imgSize);
            right.Resize(imgSize);
            face.Resize(imgSizeFace);

            var bufferSize = ImageSizeFace * ImageSizeFace * 3;
            var bufferFace = FaceSizeFace * FaceSizeFace * 3;
            if (imgBufferLeft == null || imgBufferLeft.Length != bufferSize)
                imgBufferLeft = new float[bufferSize];
            if (imgBufferRight == null || imgBufferRight.Length != bufferSize)
                imgBufferRight = new float[bufferSize];
            if (imgBufferFace == null || imgBufferFace.Length != bufferFace)
                imgBufferFace = new float[bufferFace];

            var imgTensorLeft = Tools.MatBgr2Tensor(left, NormalizeMode.CenterZero, -1, -1, new long[] { 1, ImageSizeFace, ImageSizeFace, 3 }, imgBufferLeft);
            var imgTensorRight = Tools.MatBgr2Tensor(right, NormalizeMode.CenterZero, -1, -1, new long[] { 1, ImageSizeFace, ImageSizeFace, 3 }, imgBufferRight);
            var imgTensorFace = Tools.MatBgr2Tensor(face, NormalizeMode.CenterZero, -1, -1, new long[] { 1, FaceSizeFace, FaceSizeFace, 3 }, imgBufferFace);

            Tensor[] fetch = sessFace.Run(new[] { "output" },
                new Dictionary<string, Tensor>() { { "input_image", imgTensorLeft }, { "input_image_r", imgTensorRight }, { "input_image_f", imgTensorFace }, { "phase_train", new Tensor(false) }, { "keep_prob", new Tensor(0.0f) } });

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
                imgTensorFace.Dispose();
                imgTensorFace = null;
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
                throw;
            }

            return new Point(output[0, 0], output[0, 1]);
        }

        private Point DetectBothEyes(Mat left, Mat right)
        {
            var imgSize = new Size(ImageSizeEx, ImageSizeEx);
            left.Resize(imgSize, 0, 0, InterpolationFlags.Cubic);
            right.Resize(imgSize, 0, 0, InterpolationFlags.Cubic);

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
            mat.Resize(new Size(ImageSize, ImageSize), 0, 0, InterpolationFlags.Cubic);

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