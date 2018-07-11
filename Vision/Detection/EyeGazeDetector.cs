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

    public class EyeGazeModel : IDisposable
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double ErrorRate { get; set; }

        public ManifestResource GraphResource { get; set; }
        public Graph Graph { get; set; }
        public Session Session { get; set; }

        public bool FaceRequired { get; set; }
        public bool LeftRequired { get; set; }
        public bool RightRequired { get; set; }

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

        public bool IsLoaded { get; set; } = false;

        public EyeGazeModel(string name, string filename) : this(name, new ManifestResource("Vision.Detection", filename))
        {

        }

        public EyeGazeModel(string name, ManifestResource resource)
        {
            Name = name;
            GraphResource = resource;
        }

        public void Load()
        {
            IsLoaded = true;
            try
            {
                Graph = new Graph();
                Graph.ImportPb(GraphResource);
                Session = new Session(Graph);
            }
            catch (Exception e)
            {
                IsLoaded = false;

                Logger.Throw(this, e);
            }
        }

        public void Dispose()
        {
            Graph?.Dispose();
            Graph = null;
            Session?.Dispose();
            Session = null;
        }
    }

    public class EyeGazeDetector : IDisposable
    {
        public const double DefaultSensitiveX = 1;
        public const double DefaultSensitiveY = 1;
        public const double DefaultOffsetX = 0;
        public const double DefaultOffsetY = 0;

        public List<EyeGazeModel> Models { get; set; } = new List<EyeGazeModel>()
        {
            new EyeGazeModel("LeftOnly", "frozen_gaze.pb")
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
            new EyeGazeModel("LeftOnlyV2", "frozen_gazeV2.pb")
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
            new EyeGazeModel("LeftOnlyV2Mobile", "frozen_gazeV2Mobile.pb")
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
            new EyeGazeModel("Both", "frozen_gazeEx.pb")
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
            new EyeGazeModel("Face", "frozen_gazeFace.pb")
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
            new EyeGazeModel("FaceMobile", "frozen_gazeFaceMobile.pb")
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
            new EyeGazeModel("FaceV2", "frozen_gazeFaceV2.pb")
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
            new EyeGazeModel("FaceV2Mobile", "frozen_gazeFaceV2Mobile.pb")
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
        };
        public int ModelIndex { get; set; } = 0;
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

        float[] imgBufferLeft;
        float[] imgBufferRight;
        float[] imgBufferFace;

        public EyeGazeDetector(ScreenProperties screen)
        {
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

            if (model.LeftRequired && (face.LeftEye == null || face.RightEye == null))
                return null;
            if (model.RightRequired && face.RightEye == null)
                return null;

            Profiler.Start("GazeDetect");

            if (!model.IsLoaded)
            {
                var timer = new System.Diagnostics.Stopwatch();
                timer.Start();
                model.Load();
                timer.Stop();
                Logger.Log($"Model[{model.Name}] load time: {timer.ElapsedMilliseconds} ms");
            }

            Point vecPt = null;
            Point result = new Point(0, 0);
            Point pt = new Point(0, 0);

            Profiler.Start("Gaze.Face.Cvt");
            Mat leftRoi = null, rightRoi = null, faceRoi = null;
            Tensor leftTensor = null, rightTensor = null, faceTensor = null;
            if (model.LeftRequired)
            {
                leftRoi = face.LeftEye.RoiCropByPercent(frame, model.EyeCropPercent);
                leftRoi.Resize(new Size(model.EyeSize));
                var bufLen = (int)Math.Pow(model.EyeSize, 2) * 3;
                if (imgBufferLeft == null || imgBufferLeft.Length != bufLen)
                    imgBufferLeft = new float[bufLen];
                leftTensor = Tools.MatBgr2Tensor(leftRoi, model.ImageNormMode, -1, -1, new long[] { 1, model.EyeSize, model.EyeSize, 3 }, imgBufferLeft);
            }
            if (model.RightRequired)
            {
                rightRoi = face.RightEye.RoiCropByPercent(frame, model.EyeCropPercent);
                rightRoi.Resize(new Size(model.EyeSize));
                var bufLen = (int)Math.Pow(model.EyeSize, 2) * 3;
                if (imgBufferRight == null || imgBufferRight.Length != bufLen)
                    imgBufferRight = new float[bufLen];
                rightTensor = Tools.MatBgr2Tensor(rightRoi, model.ImageNormMode, -1, -1, new long[] { 1, model.EyeSize, model.EyeSize, 3 }, imgBufferRight);
            }
            if (model.FaceRequired)
            {
                faceRoi = face.ROI(frame);
                faceRoi.Resize(new Size(model.FaceSize));
                var bufLen = (int)Math.Pow(model.FaceSize, 2) * 3;
                if (imgBufferFace == null || imgBufferFace.Length != bufLen)
                    imgBufferFace = new float[bufLen];
                faceTensor = Tools.MatBgr2Tensor(faceRoi, model.ImageNormMode, -1, -1, new long[] { 1, model.FaceSize, model.FaceSize, 3 }, imgBufferFace);
            }
            Profiler.End("Gaze.Face.Cvt");

            Profiler.Start("Gaze.Face.Sess");
            Dictionary<string, Tensor> feedDict = new Dictionary<string, Tensor>();
            if (model.LeftRequired)
                feedDict.Add(model.LeftOpName, leftTensor);
            if (model.RightRequired)
                feedDict.Add(model.RightOpName, rightTensor);
            if (model.FaceRequired)
                feedDict.Add(model.FaceOpName, faceTensor);
            if (!string.IsNullOrEmpty(model.PhaseTrainOpName))
                feedDict.Add(model.PhaseTrainOpName, new Tensor(false));
            if (!string.IsNullOrEmpty(model.KeepProbOpName))
                feedDict.Add(model.KeepProbOpName, new Tensor(model.KeepProb));

            var fetch = model.Session.Run(new[] { model.OutputOpName }, feedDict);
            Profiler.End("Gaze.Face.Sess");

            var resultTensor = fetch[0];
            float[,] output = (float[,])resultTensor.GetValue();

            result = new Point(output[0, 0], output[0, 1]);

            Profiler.Start("Gaze.Face.Dispose");
            leftTensor?.Dispose();
            rightTensor?.Dispose();
            faceTensor?.Dispose();
            leftRoi?.Dispose();
            rightRoi?.Dispose();
            faceRoi?.Dispose();
            Profiler.End("Gaze.Face.Dispose");

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

        public void Dispose()
        {
            imgBufferLeft = null;
            imgBufferRight = null;
            imgBufferFace = null;

            foreach (var item in Models)
                item.Dispose();
        }
    }
}