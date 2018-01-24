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
    }

    public class EyeOpenDetector : IDisposable
    {
        public const int ImgSize = 25;
        public const int ImgSizeV2 = 36;

        public static ManifestResource GraphResource = new ManifestResource("Vision.Detection", "frozen_open.pb");
        public static ManifestResource GraphV2Resource = new ManifestResource("Vision.Detection", "frozen_openV2.pb");
        public static Graph Graph;
        public static Graph GraphV2;

        static EyeOpenDetector()
        {
            Graph = new Graph();
            Graph.ImportPb(Storage.LoadResource(GraphResource, true));

            GraphV2 = new Graph();
            GraphV2.ImportPb(Storage.LoadResource(GraphV2Resource, true));

            Logger.Log("EyeOpenDetector", "Graph Loaded");
        }

        public EyeOpenDetectMode DetectMode { get; set; } = EyeOpenDetectMode.V2;

        Session sess;
        Session sessV2;
        float[] imgBuffer;

        public EyeOpenDetector()
        {
            sess = new Session(Graph);
            sessV2 = new Session(GraphV2);
        }

        public EyeOpenData Detect(EyeRect eye, Mat frame)
        {
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
                switch (DetectMode)
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
                    default:
                        throw new NotImplementedException();
                }
                roi.Resize(new Size(imgSize, imgSize));
                var bufferSize = roi.Width * roi.Height * 3;
                if (imgBuffer == null || imgBuffer.Length != bufferSize)
                    imgBuffer = new float[bufferSize];
                
                var imgTensor = Tools.MatBgr2Tensor(roi, normalizeMode, -1, -1, new long[] { 1, roi.Width, roi.Height, 3 }, imgBuffer);
                Dictionary<string, Tensor> feedDict;
                switch (DetectMode)
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
                    default:
                        throw new NotImplementedException();
                }
                Profiler.Start("Open.Sess");
                var fetch = sess.Run(new[] { "output" }, feedDict);
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
