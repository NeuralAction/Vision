using MathNet.Numerics.LinearAlgebra;
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

    public class EyeGazeDetector : IDisposable
    {
        public const int ImageSize = 60;
        public const int ImageSizeEx = 60;
        public const double AngleMul = 1;
        public const double DefaultSensitiveX = 1.85;
        public const double DefaultSensitiveY = 2;
        public const double DefaultOffsetX = 0.02;
        public const double DefaultOffsetY = -0.12;

        public readonly static ManifestResource ModelResourceExtend = new ManifestResource("Vision.Detection", "frozen_gazeEx.pb");
        public readonly static ManifestResource ModelResourceSingle = new ManifestResource("Vision.Detection", "frozen_gaze.pb");

        public static object ModelLocker = new object();
        public static Graph ModelGraphSingle;
        public static Graph ModelGraphExtend;

        static EyeGazeDetector()
        {
            Logger.Log("EyeGazeDetector", "Start model load");

            ModelGraphSingle = new Graph();
            ModelGraphSingle.ImportPb(Storage.LoadResource(ModelResourceSingle, true));

            ModelGraphExtend = new Graph();
            ModelGraphExtend.ImportPb(Storage.LoadResource(ModelResourceExtend, true));

            Logger.Log("EyeGazeDetector", "Finished model load");
        }

        public bool ClipToBound { get; set; } = false;
        public bool UseBothEyes { get; set; } = true;
        public bool UseSmoothing { get; set; } = false;

        public bool UseModification { get; set; } = true;
        public double SensitiveX { get; set; } = DefaultSensitiveX;
        public double OffsetX { get; set; } = DefaultOffsetX;
        public double SensitiveY { get; set; } = DefaultSensitiveY;
        public double OffsetY { get; set; } = DefaultOffsetY;

        public ScreenProperties ScreenProperties { get; set; }

        Session sess;
        Session sessEx;
        Tensor imgTensorLeft;
        Tensor imgTensorRight;
        PointKalmanFilter kalman = new PointKalmanFilter();
        float[] imgBufferLeft;
        float[] imgBufferRight;

        public EyeGazeDetector(ScreenProperties screen)
        {
            ScreenProperties = screen ?? throw new ArgumentNullException("screen properites");

            sess = new Session(ModelGraphSingle);
            sessEx = new Session(ModelGraphExtend);
        }

        public Point Detect(FaceRect face, VMat frame)
        {
            var properties = ScreenProperties;

            if (face == null)
                throw new ArgumentNullException("face");
            if (frame == null || frame.IsEmpty)
                throw new ArgumentNullException("frame");
            if (properties == null)
                throw new ArgumentNullException("properties");

            if (face.LeftEye == null)
                return null;
            if (UseBothEyes && face.RightEye == null)
                return null;

            Profiler.Start("GazeDetect");

            Point pt = new Point(0, 0);
            lock (ModelLocker)
            {
                Point result;
                if (UseBothEyes)
                {
                    using (VMat left = face.LeftEye.RoiCropByPercent(frame))
                    using (VMat right = face.RightEye.RoiCropByPercent(frame))
                        result = DetectBothEyes(left, right);
                }
                else
                {
                    using (VMat left = face.LeftEye.RoiCropByPercent(frame))
                        result = DetectLeftEyes(left);
                }

                var x = result.X / AngleMul * -1;
                var y = result.Y / AngleMul * -1;
                if (UseModification)
                {
                    x = (x + OffsetX) * SensitiveX;
                    y = (y + OffsetY) * SensitiveY;
                }

                Vector<double> vec = CreateVector.Dense(new double[] { x, y, -1 });
                pt = face.SolveRayScreenVector(new Point3D(vec.ToArray()), properties, Flandmark.UnitPerMM);
            }

            if (ClipToBound)
            {
                pt.X = Util.Clamp(pt.X, 0, ScreenProperties.PixelSize.Width);
                pt.Y = Util.Clamp(pt.Y, 0, ScreenProperties.PixelSize.Height);
            }

            if (UseSmoothing)
            {
                pt = kalman.Calculate(pt);
            }

            Profiler.End("GazeDetect");
            return pt;
        }

        private Point DetectBothEyes(VMat left, VMat right)
        {
            left.Resize(new Size(ImageSizeEx, ImageSizeEx), 0, 0, Interpolation.Cubic);
            right.Resize(new Size(ImageSizeEx, ImageSizeEx), 0, 0, Interpolation.Cubic);

            if (imgBufferLeft == null)
                imgBufferLeft = new float[ImageSizeEx * ImageSizeEx * 3];
            if (imgBufferRight == null)
                imgBufferRight = new float[ImageSizeEx * ImageSizeEx * 3];

            imgTensorLeft = Tools.VMatBgr2Tensor(left, NormalizeMode.ZeroMean, -1, -1, new long[] { 1, ImageSizeEx, ImageSizeEx, 3 }, imgBufferLeft);
            imgTensorRight = Tools.VMatBgr2Tensor(right, NormalizeMode.ZeroMean, -1, -1, new long[] { 1, ImageSizeEx, ImageSizeEx, 3 }, imgBufferRight);

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

        private Point DetectLeftEyes(VMat mat)
        {
            mat.Resize(new Size(ImageSize, ImageSize), 0, 0, Interpolation.Cubic);

            if (imgBufferLeft == null)
                imgBufferLeft = new float[ImageSize * ImageSize * 3];
            imgTensorLeft = Tools.VMatBgr2Tensor(mat, NormalizeMode.ZeroMean, -1, -1, new long[] { 1, ImageSize, ImageSize, 3 }, imgBufferLeft);
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
        }
    }
}