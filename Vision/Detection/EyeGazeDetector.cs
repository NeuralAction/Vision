using MathNet.Numerics.LinearAlgebra;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;
using Vision.Tensorflow;
using Vision.ONNX;

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

    public abstract class EyeGazeModel : IDisposable
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double ErrorRate { get; set; }

        public bool IsLoaded { get; protected set; } = false;
        public bool IsErrored { get; protected set; } = false;
        public bool IsActivated { get; protected set; }

        public bool FaceRequired { get; set; }
        public bool LeftRequired { get; set; }
        public bool RightRequired { get; set; }

        public Exception InnerException { get; protected set; }

        public abstract Point Forward(Mat frame, FaceRect face);

        object loadLocker = new object();

        protected virtual void OnLoad() { }
        public void Load()
        {
            if (!IsLoaded)
            {
                lock (loadLocker)
                {
                    if (IsLoaded)
                        return;
                    OnLoad();
                    IsLoaded = true;
                }
            }
        }
        protected virtual void OnActivate() { }
        public void Activate() { OnActivate(); IsActivated = true; }
        protected virtual void OnDeactivate() { }
        public void Deactivate() { OnDeactivate(); IsActivated = false; }

        public abstract void Dispose();
    }

    public class MergeONNXEyeGazeModel : EyeGazeModel
    {
        public int ImgSize { get; set; } = 64;
        public ManifestResource Resource { get; set; }
        public ONNXSession Session { get; set; }
        bool isFP16 = false;
        public bool IsFP16 { get => isFP16; set { isFP16 = value; if (Session != null) Session.IsFP16 = value; } }

        ONNXBinding inputName;
        ONNXBinding outputName;

        public MergeONNXEyeGazeModel(string name, string filename)
        {
            LeftRequired = RightRequired = FaceRequired = true;
            ErrorRate = 3.2;
            Description = "ONNX version; Merged channel; Face based.";
            Name = name;

            Resource = new ManifestResource("Vision.Detection.Models", filename);
        }

        protected override void OnLoad()
        {
            buffer = new float[ImgSize * ImgSize * 9];
            channelBuffer = new float[9][];
            for (int i = 0; i < channelBuffer.Length; i++)
            {
                channelBuffer[i] = new float[ImgSize * ImgSize];
            }

            using (var stream = Resource.GetStream())
                Session = ONNXRuntime.Instance.GetSession(stream);
            Session.IsFP16 = IsFP16;
            inputName = Session.GetInputs()[0];
            outputName = Session.GetOutputs()[0];
        }

        public override void Dispose()
        {
            Session?.Dispose();
            Session = null;
        }

        Mat[] ResizeMatArray(Mat m)
        {
            m.Resize(new Size(ImgSize));
            var split = Cv2.Split(m);
            m.Dispose();
            return split;
        }


        float[] buffer;
        float[][] channelBuffer;
        public override Point Forward(Mat frame, FaceRect rect)
        {
            Profiler.Start("Gaze.Face.Cvt");
            var leye = ResizeMatArray(rect.LeftEye.RoiCropByPercent(frame, 0.25));
            var reye = ResizeMatArray(rect.RightEye.RoiCropByPercent(frame, 0.25));
            var face = ResizeMatArray(rect.ROI(frame));
            var channels = new List<Mat>();
            channels.AddRange(leye);
            channels.AddRange(reye);
            channels.AddRange(face);

            var tensor = ONNX.Util.MatArray2ONNXTensor(channels.ToArray(), buffer, channelBuffer);
            //var tensor = new ONNXTensor() { Buffer = Random.R.FloatArray(buffer.Length, buffer), Shape = new long[] { 1, 9, ImgSize, ImgSize } };
            Profiler.End("Gaze.Face.Cvt");

            Profiler.Start("Gaze.Face.Sess");
            var result = Session.Run(new[] { outputName.Name }, new Dictionary<string, ONNXTensor>() { { inputName.Name, tensor } });
            var vector = result[0].Buffer;
            var point = new Point(vector[0], vector[1]);
            Profiler.End("Gaze.Face.Sess");

            foreach (var item in channels)
            {
                item.Dispose();
            }

            return point;
        }
    }

    public class TensorFlowEyeGazeModel : EyeGazeModel
    {
        public ManifestResource GraphResource { get; set; }
        public Graph Graph { get; set; }
        public Session Session { get; set; }

        public string FaceOpName { get; set; }
        public string LeftOpName { get; set; }
        public string RightOpName { get; set; }
        public string OutputOpName { get; set; }
        public string KeepProbOpName { get; set; }
        public string PhaseTrainOpName { get; set; }

        public float KeepProb { get; set; }

        public NormalizeMode ImageNormMode { get; set; }

        public double EyeCropPercent { get; set; } = 0.25;

        public int EyeSize { get; set; }
        public int FaceSize { get; set; }

        public TensorFlowEyeGazeModel(string name, string filename) : this(name, new ManifestResource("Vision.Detection.Models", filename))
        {

        }

        public TensorFlowEyeGazeModel(string name, ManifestResource resource)
        {
            Name = name;
            GraphResource = resource;
        }

        protected override void OnLoad()
        {
            try
            {
                Graph = new Graph();
                Graph.ImportPb(GraphResource);
                Session = new Session(Graph);
            }
            catch (Exception e)
            {
                IsLoaded = false;
                IsErrored = true;
                InnerException = e;

                Logger.Throw(this, e);
            }

            var bufLen = (int)Math.Pow(EyeSize, 2) * 3;
            if (imgBufferLeft == null || imgBufferLeft.Length != bufLen)
                imgBufferLeft = new float[bufLen];

            bufLen = (int)Math.Pow(EyeSize, 2) * 3;
            if (imgBufferRight == null || imgBufferRight.Length != bufLen)
                imgBufferRight = new float[bufLen];

            bufLen = (int)Math.Pow(FaceSize, 2) * 3;
            if (imgBufferFace == null || imgBufferFace.Length != bufLen)
                imgBufferFace = new float[bufLen];
        }

        public override void Dispose()
        {
            Graph?.Dispose();
            Graph = null;
            Session?.Dispose();
            Session = null;
        }

        float[] imgBufferLeft;
        float[] imgBufferRight;
        float[] imgBufferFace;

        public override Point Forward(Mat frame, FaceRect face)
        {
            Profiler.Start("Gaze.Face.Cvt");
            Mat leftRoi = null, rightRoi = null, faceRoi = null;
            Tensor leftTensor = null, rightTensor = null, faceTensor = null;
            if (LeftRequired)
            {
                leftRoi = face.LeftEye.RoiCropByPercent(frame, EyeCropPercent);
                leftRoi.Resize(new Size(EyeSize));
                leftTensor = Tools.MatBgr2Tensor(leftRoi, ImageNormMode, -1, -1, new long[] { 1, EyeSize, EyeSize, 3 }, imgBufferLeft);
            }
            if (RightRequired)
            {
                rightRoi = face.RightEye.RoiCropByPercent(frame, EyeCropPercent);
                rightRoi.Resize(new Size(EyeSize));
                rightTensor = Tools.MatBgr2Tensor(rightRoi, ImageNormMode, -1, -1, new long[] { 1, EyeSize, EyeSize, 3 }, imgBufferRight);
            }
            if (FaceRequired)
            {
                faceRoi = face.ROI(frame);
                faceRoi.Resize(new Size(FaceSize));
                faceTensor = Tools.MatBgr2Tensor(faceRoi, ImageNormMode, -1, -1, new long[] { 1, FaceSize, FaceSize, 3 }, imgBufferFace);
            }
            Profiler.End("Gaze.Face.Cvt");

            Profiler.Start("Gaze.Face.Sess");
            Dictionary<string, Tensor> feedDict = new Dictionary<string, Tensor>();
            if (LeftRequired) feedDict.Add(LeftOpName, leftTensor);
            if (RightRequired) feedDict.Add(RightOpName, rightTensor);
            if (FaceRequired) feedDict.Add(FaceOpName, faceTensor);
            if (!string.IsNullOrEmpty(PhaseTrainOpName)) feedDict.Add(PhaseTrainOpName, new Tensor(false));
            if (!string.IsNullOrEmpty(KeepProbOpName)) feedDict.Add(KeepProbOpName, new Tensor(KeepProb));

            var fetch = Session.Run(new[] { OutputOpName }, feedDict);
            Profiler.End("Gaze.Face.Sess");

            var resultTensor = fetch[0];
            float[,] output = (float[,])resultTensor.GetValue();

            var result = new Point(output[0, 0], output[0, 1]);

            Profiler.Start("Gaze.Face.Dispose");
            leftTensor?.Dispose();
            rightTensor?.Dispose();
            faceTensor?.Dispose();
            leftRoi?.Dispose();
            rightRoi?.Dispose();
            faceRoi?.Dispose();
            Profiler.End("Gaze.Face.Dispose");

            return result;
        }
    }

    public class EyeGazeDetector : IDisposable
    {
        public const double DefaultSensitiveX = 1;
        public const double DefaultSensitiveY = 1;
        public const double DefaultOffsetX = 0;
        public const double DefaultOffsetY = 0;
        public const int DefaultModelIndex = 8;

        public List<EyeGazeModel> Models { get; set; } = new List<EyeGazeModel>()
        {
            new TensorFlowEyeGazeModel("LeftOnly", "frozen_gaze.pb")
            {
                Description = "Using left eye only.",
                ErrorRate = 12.40,
                LeftRequired = true,
                RightRequired = false,
                FaceRequired = false,
                EyeSize = 60,
                FaceSize = -1,
                EyeCropPercent = 0.33,
                LeftOpName = "input_image",
                RightOpName = "",
                FaceOpName = "",
                OutputOpName = "output",
                KeepProbOpName = "keep_prob",
                PhaseTrainOpName = "phase_train",
                KeepProb = 1.0f,
                ImageNormMode = NormalizeMode.ZeroMean,
            },
            new TensorFlowEyeGazeModel("LeftOnlyV2", "frozen_gazeV2.pb")
            {
                Description = "Using left eye only. version 2",
                ErrorRate = 5.67,
                LeftRequired = true,
                RightRequired = false,
                FaceRequired = false,
                EyeSize = 64,
                FaceSize = -1,
                EyeCropPercent = 0.25,
                LeftOpName = "input_left",
                RightOpName = "",
                FaceOpName = "",
                OutputOpName = "output",
                KeepProbOpName = "keep_prob",
                PhaseTrainOpName = "phase_train",
                KeepProb = 0.0f,
                ImageNormMode = NormalizeMode.CenterZero,
            },
            new TensorFlowEyeGazeModel("LeftOnlyV2Mobile", "frozen_gazeV2Mobile.pb")
            {
                Description = "Using left eye only. version 2 for mobile",
                ErrorRate = 5.81,
                LeftRequired = true,
                RightRequired = false,
                FaceRequired = false,
                EyeSize = 64,
                FaceSize = -1,
                EyeCropPercent = 0.25,
                LeftOpName = "input_left",
                RightOpName = "",
                FaceOpName = "",
                OutputOpName = "output",
                KeepProbOpName = "keep_prob",
                PhaseTrainOpName = "phase_train",
                KeepProb = 0.0f,
                ImageNormMode = NormalizeMode.CenterZero,
            },
            new TensorFlowEyeGazeModel("Both", "frozen_gazeEx.pb")
            {
                Description = "Using left and right eyes.",
                ErrorRate = 11.30,
                LeftRequired = true,
                RightRequired = true,
                FaceRequired = false,
                EyeSize = 60,
                FaceSize = -1,
                EyeCropPercent = 0.33,
                LeftOpName = "input_image",
                RightOpName = "input_image_r",
                FaceOpName = "input_image_f",
                OutputOpName = "output",
                KeepProbOpName = "keep_prob",
                PhaseTrainOpName = "phase_train",
                KeepProb = 1.0f,
                ImageNormMode = NormalizeMode.ZeroMean,
            },
            new TensorFlowEyeGazeModel("Face", "frozen_gazeFace.pb")
            {
                Description = "Using left and right eyes, and face.",
                ErrorRate = 4.00,
                LeftRequired = true,
                RightRequired = true,
                FaceRequired = true,
                EyeSize = 60,
                FaceSize = 60,
                EyeCropPercent = 0.25,
                LeftOpName = "input_image",
                RightOpName = "input_image_r",
                FaceOpName = "input_image_f",
                OutputOpName = "output",
                KeepProbOpName = "keep_prob",
                PhaseTrainOpName = "phase_train",
                KeepProb = 0.0f,
                ImageNormMode = NormalizeMode.CenterZero,
            },
            new TensorFlowEyeGazeModel("FaceMobile", "frozen_gazeFaceMobile.pb")
            {
                Description = "Mobile version of `Face` model.",
                ErrorRate = 4.85,
                LeftRequired = true,
                RightRequired = true,
                FaceRequired = true,
                EyeSize = 64,
                FaceSize = 64,
                EyeCropPercent = 0.25,
                LeftOpName = "input_image",
                RightOpName = "input_image_r",
                FaceOpName = "input_image_f",
                OutputOpName = "output",
                KeepProbOpName = "keep_prob",
                PhaseTrainOpName = "phase_train",
                KeepProb = 0.0f,
                ImageNormMode = NormalizeMode.CenterZero,
            },
            new TensorFlowEyeGazeModel("FaceV2", "frozen_gazeFaceV2.pb")
            {
                Description = "Improved version of `Face`. Using ResNet arch.",
                ErrorRate = 3.23,
                LeftRequired = true,
                RightRequired = true,
                FaceRequired = true,
                EyeSize = 60,
                FaceSize = 32,
                EyeCropPercent = 0.25,
                LeftOpName = "input_left",
                RightOpName = "input_right",
                FaceOpName = "input_face",
                OutputOpName = "output",
                KeepProbOpName = "keep_prob",
                PhaseTrainOpName = "phase_train",
                KeepProb = 0.0f,
                ImageNormMode = NormalizeMode.CenterZero,
            },
            new TensorFlowEyeGazeModel("FaceV2Mobile", "frozen_gazeFaceV2Mobile.pb")
            {
                Description = "Mobile version of `FaceV2`",
                ErrorRate = 3.56,
                LeftRequired = true,
                RightRequired = true,
                FaceRequired = true,
                EyeSize = 60,
                FaceSize = 32,
                EyeCropPercent = 0.25,
                LeftOpName = "input_left",
                RightOpName = "input_right",
                FaceOpName = "input_face",
                OutputOpName = "output",
                KeepProbOpName = "keep_prob",
                PhaseTrainOpName = "phase_train",
                KeepProb = 0.0f,
                ImageNormMode = NormalizeMode.CenterZero,
            },
            new MergeONNXEyeGazeModel("MergeChannel", "torch_gazeMerge.onnx")
            {
                IsFP16 = false,
            },
            //new MergeONNXEyeGazeModel("MergeChannelFP16", "torch_gazeMergeFP16.onnx")
            //{
            //    IsFP16 = true,
            //}
        };

        int modelIndex = -1;
        public int ModelIndex
        {
            get => modelIndex; set
            {
                if (value < 0 || value >= Models.Count)
                    throw new IndexOutOfRangeException();
                if (modelIndex != value)
                {
                    modelIndex = value;
                    CurrentModel.Load();
                    CurrentModel.Activate();
                }
            }
        }
        public EyeGazeModel CurrentModel => Models[ModelIndex];

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

        public EyeGazeDetector(ScreenProperties screen)
        {
            ModelIndex = DefaultModelIndex;

            ScreenProperties = screen ?? throw new ArgumentNullException("screen properites");

            Calibrator = new EyeGazeCalibrater();
        }

        public Point Detect(FaceRect face, Mat frame)
        {
            var model = CurrentModel;
            var properties = ScreenProperties;

            if (face == null)
                throw new ArgumentNullException("face");
            if (frame == null || frame.IsEmpty)
                throw new ArgumentNullException("frame");
            if (properties == null)
                throw new ArgumentNullException("properties");

            Profiler.Start("GazeDetect");

            if (!model.IsLoaded)
            {
                var timer = new System.Diagnostics.Stopwatch();
                timer.Start();
                model.Load();
                timer.Stop();
                Logger.Log($"Model[{model.Name}] load time: {timer.ElapsedMilliseconds} ms");
            }

            if (model.LeftRequired && (face.LeftEye == null || face.RightEye == null))
                return null;
            if (model.RightRequired && face.RightEye == null)
                return null;

            var result = model.Forward(frame, face);

            var x = result.X * -1;
            var y = result.Y * -1;
            if (UseModification)
            {
                x = (x + OffsetX) * SensitiveX;
                y = (y + OffsetY) * SensitiveY;
            }

            var vecPt = new Point(x, y);
            if (UseSmoothing && !Calibrator.IsCalibrating)
                vecPt = Smoother.Smooth(vecPt);

            Vector<double> vec = CreateVector.Dense(new double[] { vecPt.X, vecPt.Y, -1 });
            var pixelPt = face.SolveRayScreenVector(new Point3D(vec.ToArray()), properties);

            if (ClipToBound)
            {
                pixelPt.X = Util.Clamp(pixelPt.X, 0, ScreenProperties.PixelSize.Width);
                pixelPt.Y = Util.Clamp(pixelPt.Y, 0, ScreenProperties.PixelSize.Height);
            }

            face.GazeInfo = new EyeGazeInfo()
            {
                ScreenPoint = pixelPt,
                Vector = new Point3D(vecPt.X, vecPt.Y, -1),
                ClipToBound = ClipToBound,
            };

            Calibrator.Push(new CalibratingPushData(face, frame));
            if (UseCalibrator)
                Calibrator.Apply(face, ScreenProperties);

            Profiler.End("GazeDetect");
            return face.GazeInfo.ScreenPoint;
        }

        public void Dispose()
        {
            foreach (var item in Models)
                item.Dispose();
        }
    }
}