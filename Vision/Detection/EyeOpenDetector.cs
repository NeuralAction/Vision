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

    public enum EyeOpenDetectMode
    {
        V1,
        V2,
        V3,
    }

    public class EyeOpenDetector : IDisposable
    {
        public const int ImgSize = 25;
        public const int ImgSizeV2 = 36;
        public const int ImgSizeV3 = 32;

        public static ManifestResource GraphResource = new ManifestResource("Vision.Detection", "frozen_open.pb");
        public static ManifestResource GraphV2Resource = new ManifestResource("Vision.Detection", "frozen_openV2.pb");
        public static ManifestResource GraphV3Resource = new ManifestResource("Vision.Detection", "frozen_openV3.pb");
        public static Graph Graph;
        public static Graph GraphV2;
        public static Graph GraphV3;

        static EyeOpenDetector()
        {
            Graph = new Graph();
            using(var s = GraphResource.GetStream())
                Graph.ImportPb(s);

            GraphV2 = new Graph();
            using(var s = GraphV2Resource.GetStream())
                GraphV2.ImportPb(s);

            GraphV3 = new Graph();
            using(var s = GraphV3Resource.GetStream())
                GraphV3.ImportPb(s);

            Logger.Log("EyeOpenDetector", "Graph Loaded");
        }

        public EyeOpenDetectMode DetectMode { get; set; } = EyeOpenDetectMode.V3;

        Session sess;
        Session sessV2;
        Session sessV3;
        float[] imgBuffer;

        public EyeOpenDetector()
        {
            sess = new Session(Graph);
            sessV2 = new Session(GraphV2);
            sessV3 = new Session(GraphV3);
        }

        public EyeOpenData Detect(EyeRect eye, Mat frame)
        {
            var mode = DetectMode;
            if (eye == null)
                throw new ArgumentNullException("eye");
            if (eye.Parent == null)
                throw new ArgumentNullException("eyeParent");
            if (frame == null)
                throw new ArgumentNullException("frame");
            if (frame.IsEmpty)
                throw new ArgumentNullException("frame is empty");

            Profiler.Start("OpenALL");
            using (Mat roi = eye.RoiCropByPercent(frame))
            {
                Session sess;
                var imgSize = 0;
                var normalizeMode = NormalizeMode.None;
                switch (mode)
                {
                    case EyeOpenDetectMode.V1:
                        imgSize = ImgSize;
                        sess = this.sess;
                        normalizeMode = NormalizeMode.ZeroMean;
                        break;
                    case EyeOpenDetectMode.V2:
                        imgSize = ImgSizeV2;
                        sess = sessV2;
                        normalizeMode = NormalizeMode.ZeroOne;
                        break;
                    case EyeOpenDetectMode.V3:
                        imgSize = ImgSizeV3;
                        sess = sessV3;
                        normalizeMode = NormalizeMode.CenterZero;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                roi.Resize(new Size(imgSize, imgSize));
                var bufferSize = roi.Width * roi.Height * 3;
                if (imgBuffer == null || imgBuffer.Length != bufferSize)
                    imgBuffer = new float[bufferSize];
                
                var imgTensor = Tools.MatBgr2Tensor(roi, normalizeMode, -1, -1, new long[] { 1, roi.Width, roi.Height, 3 }, imgBuffer);
                Dictionary<string, Tensor> feedDict;
                string outputName = "output";
                switch (mode)
                {
                    case EyeOpenDetectMode.V1:
                        feedDict = new Dictionary<string, Tensor>()
                        {
                            { "input_image", imgTensor },
                            { "phase_train", new Tensor(false) },
                            { "keep_prob", new Tensor(1.0f) }
                        };
                        break;
                    case EyeOpenDetectMode.V2:
                        feedDict = new Dictionary<string, Tensor>()
                        {
                            { "input_image", imgTensor },
                            { "phase_train", new Tensor(false) },
                            //{ "keep_prob", new Tensor(1.0f) }
                        };
                        break;
                    case EyeOpenDetectMode.V3:
                        feedDict = new Dictionary<string, Tensor>()
                        {
                            { "input", imgTensor },
                            { "phase_train", new Tensor(false) },
                            { "keep_prob", new Tensor(1.0f) }
                        };
                        break;
                    default:
                        throw new NotImplementedException();
                }
                Profiler.Start("Open.Sess");
                var fetch = sess.Run(new[] { outputName }, feedDict);
                Profiler.End("Open.Sess");
                var result = (float[,])fetch[0].GetValue();

                foreach (var t in fetch)
                    t.Dispose();
                fetch = null;

                imgTensor.Dispose();
                imgTensor = null;

                var data = new EyeOpenData(result[0,1], result[0,0]);
                eye.OpenData = data;
                Profiler.End("OpenALL");
                return data;
            }
        }

        public void Dispose()
        {
            if (sess != null)
            {
                sess.Dispose();
                sess = null;
            }
        }
    }
}
