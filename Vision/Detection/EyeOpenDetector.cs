using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;
using Vision.ONNX;
using Vision.Tensorflow;

namespace Vision.Detection
{
    public class EyeOpenData
    {
        public bool IsOpen { get; private set; }
        public double Percent { get; private set; }

        private double _close = 0;
        public double Close
        {
            get => _close;
            set
            {
                _close = value;
                Update();
            }
        }

        private double _open = 0;
        public double Open
        {
            get => _open;
            set
            {
                _open = value;
                Update();
            }
        }

        public EyeOpenData(double openPercent, double closePercent)
        {
            Close = closePercent;
            Open = openPercent;
        }

        public EyeOpenData Clone()
        {
            return new EyeOpenData(Open, Close);
        }

        private void Update()
        {
            IsOpen = _open > _close;
            if (IsOpen)
                Percent = _open;
            else
                Percent = _close;
        }
    }

    //public enum EyeOpenDetectMode
    //{
    //    V1,
    //    V2,
    //    V3,
    //}

    public abstract class EyeOpenModel : IDisposable
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Accuracy { get; set; }

        public bool IsLoaded { get; private set; }
        public bool IsActivated { get; private set; }

        object loadLocker = new object();
        
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
        
        public void Activate()
        {
            Load();
            if (!IsActivated)
            {
                OnActivate();
                IsActivated = true;
            }
        }
        
        public void Deactivate()
        {
            if (IsActivated)
            {
                OnActivate();
                IsActivated = false;
            }
        }

        public EyeOpenData Forward(Mat frame, EyeRect eye)
        {
            Activate();
            return OnForward(frame, eye);
        }

        protected abstract EyeOpenData OnForward(Mat frame, EyeRect eye);
        protected virtual void OnLoad() { }
        protected virtual void OnActivate() { }
        protected virtual void OnDeactivate() { }

        public abstract void Dispose();
    }

    public class TensorFlowEyeOpenModel : EyeOpenModel
    {
        public int ImgSize { get; set; } = 36;
        public bool FeedKeepProb { get; set; } = true;
        public string InputName { get; set; } = "input_image";
        public NormalizeMode NormalizeMode { get; set; }

        ManifestResource resource;
        Session sess;
        Graph graph;
        float[] imgBuffer;

        public TensorFlowEyeOpenModel(string name, string filename) 
            : this(name, new ManifestResource("Vision.Detection.Models", filename)) { } 

        public TensorFlowEyeOpenModel(string name, ManifestResource resource)
        {
            this.resource = resource;
            Name = name;
        }

        protected override EyeOpenData OnForward(Mat frame, EyeRect eye)
        {
            EyeOpenData data;
            using (Mat roi = eye.RoiCropByPercent(frame))
            {
                var imgSize = ImgSize;
                var normalizeMode = NormalizeMode;
                
                roi.Resize(new Size(imgSize, imgSize));
                var imgTensor = Tools.MatBgr2Tensor(roi, normalizeMode, -1, -1, new long[] { 1, roi.Width, roi.Height, 3 }, imgBuffer);

                var feedDict = new Dictionary<string, Tensor>();
                feedDict.Add(InputName, imgTensor);
                feedDict.Add("phase_train", new Tensor(false));
                if(FeedKeepProb) feedDict.Add("keep_prob", new Tensor(1.0f));
                string outputName = "output";

                Profiler.Start("Open.Sess");
                var fetch = sess.Run(new[] { outputName }, feedDict);
                Profiler.End("Open.Sess");
                var result = (float[,])fetch[0].GetValue();

                foreach (var t in fetch)
                    t.Dispose();
                fetch = null;

                imgTensor.Dispose();
                imgTensor = null;

                data = new EyeOpenData(result[0, 1], result[0, 0]);
            }
            return data;
        }

        protected override void OnLoad()
        {
            var bufferSize = ImgSize * ImgSize * 3;
            imgBuffer = new float[bufferSize];

            graph = new Graph();
            using (var s = resource.GetStream())
                graph.ImportPb(s);
            sess = new Session(graph);
        }

        public override void Dispose()
        {
            imgBuffer = null;

            sess?.Dispose();
            sess = null;

            graph?.Dispose();
            graph = null;
        }
    }

    public class ONNXEyeOpenModel : EyeOpenModel
    {
        public int ImgSize { get; set; } = 64;

        ManifestResource resource;
        ONNXSession session;

        public ONNXEyeOpenModel(string name, string filename) 
            : this(name, new ManifestResource("Vision.Detection.Models", filename)) { }

        public ONNXEyeOpenModel(string name, ManifestResource resource)
        {
            Name = name;
            this.resource = resource;
        }

        float[] outputBuffer;
        float[][] channelBuffer;
        string outputName;
        string inputName;
        protected override void OnLoad()
        {
            using (var s = resource.GetStream())
                session = ONNXRuntime.Instance.GetSession(s);

            outputBuffer = new float[ImgSize * ImgSize * 3];
            channelBuffer = new float[3][];
            for (int i = 0; i < channelBuffer.Length; i++)
            {
                channelBuffer[i] = new float[ImgSize * ImgSize];
            }
            outputName = session.GetOutputs()[0].Name;
            inputName = session.GetInputs()[0].Name;
        }

        protected override EyeOpenData OnForward(Mat frame, EyeRect eye)
        {
            EyeOpenData data;
            using (Mat roi = eye.RoiCropByPercent(frame))
            {
                var imgSize = ImgSize;
                roi.Resize(new Size(imgSize, imgSize));
                var split = Cv2.Split(roi);
                var imgTensor = ONNX.Util.MatArray2ONNXTensor(split, outputBuffer, channelBuffer);

                Profiler.Start("Open.Sess");
                var result = session.Run(new[] { outputName }, 
                    new Dictionary<string, ONNXTensor>() { { inputName, imgTensor } })[0];
                var vector = result.Buffer;
                Profiler.End("Open.Sess");

                data = new EyeOpenData(vector[1], vector[0]);

                foreach (var item in split)
                    item.Dispose();
            }
            return data;
        }

        public override void Dispose()
        {
            session?.Dispose();
            session = null;

            outputBuffer = null;
            channelBuffer = null;
        }
    }

    public class EyeOpenDetector : IDisposable
    {
        public const int DefaultModelIndex = 3;

        public List<EyeOpenModel> Models { get; set; } = new List<EyeOpenModel>()
        {
            new TensorFlowEyeOpenModel("OpenV1", "frozen_open.pb")
            {
                Description = "LeNet5 based tiny model",
                Accuracy = "80%",
                FeedKeepProb = true,
                ImgSize = 25,
                InputName = "input_image",
                NormalizeMode = NormalizeMode.ZeroMean,
            },
            new TensorFlowEyeOpenModel("OpenV2", "frozen_openV2.pb")
            {
                Description = "Version 2",
                Accuracy = "90%",
                FeedKeepProb = false,
                ImgSize = 36,
                InputName = "input_image",
                NormalizeMode = NormalizeMode.ZeroOne,
            },
            new TensorFlowEyeOpenModel("OpenV3", "frozen_openV3.pb")
            {
                Description = "Version 3",
                Accuracy = "92%",
                FeedKeepProb = true,
                ImgSize = 32,
                InputName = "input",
                NormalizeMode = NormalizeMode.CenterZero,
            },
            new ONNXEyeOpenModel("OpenV4", "torch_open.onnx")
            {
                Description = "Version 4; ONNX enabled",
                Accuracy = "95%",
                ImgSize = 64,
            },
        };

        int modelIndex = DefaultModelIndex;
        public int ModelIndex 
        {
            get => modelIndex;
            set
            {
                if(modelIndex != value)
                {
                    CurrentModel.Deactivate();
                    modelIndex = value;
                    CurrentModel.Activate();
                }
            }
        }

        public EyeOpenModel CurrentModel => Models[modelIndex];

        public EyeOpenData Detect(EyeRect eye, Mat frame)
        {
            var model = CurrentModel;
            if (eye == null)
                throw new ArgumentNullException("eye");
            if (eye.Parent == null)
                throw new ArgumentNullException("eyeParent");
            if (frame == null)
                throw new ArgumentNullException("frame");
            if (frame.IsEmpty)
                throw new ArgumentNullException("frame is empty");

            Profiler.Start("OpenALL");
            var data = model.Forward(frame, eye);
            eye.OpenData = data;
            Profiler.End("OpenALL");

            return data;
        }

        public void Dispose()
        {
            foreach (var item in Models)
                item.Dispose();
        }
    }
}
