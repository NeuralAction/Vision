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
        FaceV2 = 4,
        FaceV2Mobile = 5
    }

    public class EyeGazeDetector : IDisposable
    {
        public const int ImageSize = 60;
        public const int ImageSizeEx = 60;
        public const int ImageSizeFace = 60;
        public const int FaceSizeFace = 60;
        public const int ImageSizeFaceMobile = 64;
        public const int FaceSizeFaceMobile = 64;
        public const int ImageSizeFaceV2 = 60;
        public const int FaceSizeFaceV2 = 32;
        public const int ImageSizeFaceV2Mobile = 60;
        public const int FaceSizeFaceV2Mobile = 32;
        public const double DefaultSensitiveX = 1;
        public const double DefaultSensitiveY = 1;
        public const double DefaultOffsetX = 0;
        public const double DefaultOffsetY = 0;

        public readonly static ManifestResource ModelResourceExtend = new ManifestResource("Vision.Detection", "frozen_gazeEx.pb");
        public readonly static ManifestResource ModelResourceSingle = new ManifestResource("Vision.Detection", "frozen_gaze.pb");
        public readonly static ManifestResource ModelResourceFace = new ManifestResource("Vision.Detection", "frozen_gazeFace.pb");
        public readonly static ManifestResource ModelResourceFaceMobile = new ManifestResource("Vision.Detection", "frozen_gazeFaceMobile.pb");
        public readonly static ManifestResource ModelResourceFaceV2 = new ManifestResource("Vision.Detection", "frozen_gazeFaceV2.pb");
        public readonly static ManifestResource ModelResourceFaceV2Mobile = new ManifestResource("Vision.Detection", "frozen_gazeFaceV2Mobile.pb");

        public static Graph ModelGraphSingle;
        public static Graph ModelGraphExtend;
        public static Graph ModelGraphFace;
        public static Graph ModelGraphFaceMobile;
        public static Graph ModelGraphFaceV2;
        public static Graph ModelGraphFaceV2Mobile;

        static EyeGazeDetector()
        {
            Logger.Log("EyeGazeDetector", "Start model load");

            ModelGraphSingle = new Graph();
            ModelGraphSingle.ImportPb(ModelResourceSingle);

            ModelGraphExtend = new Graph();
            ModelGraphExtend.ImportPb(ModelResourceExtend);

            ModelGraphFace = new Graph();
            ModelGraphFace.ImportPb(ModelResourceFace);

            ModelGraphFaceMobile = new Graph();
            ModelGraphFaceMobile.ImportPb(ModelResourceFaceMobile);

            ModelGraphFaceV2 = new Graph();
            ModelGraphFaceV2.ImportPb(ModelResourceFaceV2);

            ModelGraphFaceV2Mobile = new Graph();
            ModelGraphFaceV2Mobile.ImportPb(ModelResourceFaceV2Mobile);

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

        public EyeGazeDetectMode DetectMode { get; set; } = EyeGazeDetectMode.FaceV2Mobile;

        EyeGazeDetectMode mode;
        int FaceSize
        {
            get
            {
                switch (mode)
                {
                    case EyeGazeDetectMode.Face:
                        return FaceSizeFace;
                    case EyeGazeDetectMode.FaceMobile:
                        return FaceSizeFaceMobile;
                    case EyeGazeDetectMode.FaceV2:
                        return FaceSizeFaceV2;
                    case EyeGazeDetectMode.FaceV2Mobile:
                        return FaceSizeFaceV2Mobile;
                    case EyeGazeDetectMode.LeftOnly:
                    case EyeGazeDetectMode.Both:
                    default:
                        throw new Exception();
                }
            }
        }

        int EyeSize
        {
            get
            {
                switch (mode)
                {
                    case EyeGazeDetectMode.LeftOnly:
                        return ImageSize;
                    case EyeGazeDetectMode.Both:
                        return ImageSizeEx;
                    case EyeGazeDetectMode.Face:
                        return ImageSizeFace;
                    case EyeGazeDetectMode.FaceMobile:
                        return ImageSizeFaceMobile;
                    case EyeGazeDetectMode.FaceV2:
                        return ImageSizeFaceV2;
                    case EyeGazeDetectMode.FaceV2Mobile:
                        return ImageSizeFaceV2Mobile;
                    default:
                        throw new Exception();
                }
            }
        }

        Session Sess
        {
            get
            {
                switch (mode)
                {
                    case EyeGazeDetectMode.LeftOnly:
                        return sess;
                    case EyeGazeDetectMode.Both:
                        return sessEx;
                    case EyeGazeDetectMode.Face:
                        return sessFace;
                    case EyeGazeDetectMode.FaceMobile:
                        return sessFaceMobile;
                    case EyeGazeDetectMode.FaceV2:
                        return sessFaceV2;
                    case EyeGazeDetectMode.FaceV2Mobile:
                        return sessFaceV2Mobile;
                    default:
                        throw new Exception();
                }
            }
        }

        Session sess;
        Session sessEx;
        Session sessFace;
        Session sessFaceMobile;
        Session sessFaceV2;
        Session sessFaceV2Mobile;
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
            sessFaceV2 = new Session(ModelGraphFaceV2);
            sessFaceV2Mobile = new Session(ModelGraphFaceV2Mobile);

            Calibrator = new EyeGazeCalibrater();
        }

        public Point Detect(FaceRect face, Mat frame)
        {
            mode = DetectMode;
            var properties = ScreenProperties;

            if (face == null)
                throw new ArgumentNullException("face");
            if (frame == null || frame.IsEmpty)
                throw new ArgumentNullException("frame");
            if (properties == null)
                throw new ArgumentNullException("properties");

            switch (mode)
            {
                case EyeGazeDetectMode.LeftOnly:
                    if (face.LeftEye == null)
                        return null;
                    break;
                case EyeGazeDetectMode.FaceV2Mobile:
                case EyeGazeDetectMode.FaceV2:
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
            Point result = new Point(0, 0);
            Point pt = new Point(0, 0);
            switch (mode)
            {
                case EyeGazeDetectMode.LeftOnly:
                    using (Mat left = face.LeftEye.RoiCropByPercent(frame, .33))
                        result = DetectLeftEyes(left);
                    break;
                case EyeGazeDetectMode.Both:
                    using (Mat left = face.LeftEye.RoiCropByPercent(frame, .33))
                    using (Mat right = face.RightEye.RoiCropByPercent(frame, .33))
                        result = DetectBothEyes(left, right);
                    break;
                case EyeGazeDetectMode.FaceV2Mobile:
                case EyeGazeDetectMode.FaceV2:
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

            var x = result.X * -1;
            var y = result.Y * -1;
            if (UseModification)
            {
                x = (x + OffsetX) * SensitiveX;
                y = (y + OffsetY) * SensitiveY;
            }

            vecPt = new Point(x, y);
            if (UseSmoothing && !Calibrator.IsCalibrating)
                vecPt = Smoother.Smooth(vecPt);

            Vector<double> vec = CreateVector.Dense(new double[] { vecPt.X, vecPt.Y, -1 });
            pt = face.SolveRayScreenVector(new Point3D(vec.ToArray()), properties);

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
            int modelImgSize = EyeSize, modelFaceSize = FaceSize;
            Session sess = Sess;

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
            Dictionary<string, Tensor> feedDict = null;
            switch (mode)
            {
                case EyeGazeDetectMode.Face:
                case EyeGazeDetectMode.FaceMobile:
                    feedDict = new Dictionary<string, Tensor>()
                    {
                        { "input_image", imgTensorLeft },
                        { "input_image_r", imgTensorRight },
                        { "input_image_f", imgTensorFace },
                        { "phase_train", new Tensor(false) },
                        { "keep_prob", new Tensor(0.0f) }
                    };
                    break;
                case EyeGazeDetectMode.FaceV2Mobile:
                case EyeGazeDetectMode.FaceV2:
                    feedDict = new Dictionary<string, Tensor>()
                    {
                        { "input_left", imgTensorLeft },
                        { "input_right", imgTensorRight },
                        { "input_face", imgTensorFace },
                        { "phase_train", new Tensor(false) },
                        { "keep_prob", new Tensor(0.0f) }
                    };
                    break;
            }
            Tensor[] fetch = sess.Run(new[] { "output" }, feedDict);
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
            Tensor[] fetch = sess.Run(new[] { "output" },
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

            if (imgBufferRight != null)
            {
                imgBufferRight = null;
            }

            if (imgBufferFace != null)
            {
                imgBufferFace = null;
            }

            if (sess != null)
            {
                sess.Dispose();
                sess = null;
            }

            if (sessEx != null)
            {
                sessEx.Dispose();
                sessEx = null;
            }

            if (sessFace != null)
            {
                sessFace.Dispose();
                sessFace = null;
            }
        }
    }
}