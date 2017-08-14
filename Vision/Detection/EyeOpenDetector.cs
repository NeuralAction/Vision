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
        public bool IsOpen { get; set; }
        public double Percent { get; set; }

        public EyeOpenData(bool isopened, double percent)
        {
            IsOpen = isopened;
            Percent = percent;
        }
    }

    public class EyeOpenDetector : IDisposable
    {
        public static ManifestResource GraphResource = new ManifestResource("Vision.Detection", "frozen_open.pb");
        public static Graph Graph;

        static EyeOpenDetector()
        {
            var loaded = Storage.LoadResource(GraphResource, true);
            Graph = new Graph();
            Graph.ImportPb(loaded);

            Logger.Log("EyeOpenDetector", "Graph Loaded");
        }

        Session sess;
        float[] imgBuffer;
        int imgSize = 25;

        public EyeOpenDetector()
        {
            sess = new Session(Graph);
        }

        public EyeOpenData Detect(EyeRect eye, VMat frame)
        {
            if (eye == null)
                throw new ArgumentNullException("eye");
            if (eye.Parent == null)
                throw new ArgumentNullException("eyeParent");
            if (frame == null)
                throw new ArgumentNullException("frame");
            if (frame.IsEmpty)
                throw new ArgumentNullException("frame is empty");

            using (VMat roi = eye.RoiCropByPercent(frame))
            {
                if (imgBuffer == null)
                    imgBuffer = new float[imgSize * imgSize * 3];

                roi.Resize(new Size(imgSize, imgSize));

                var imgTensor = Tools.VMatBgr2Tensor(roi, NormalizeMode.ZeroMean, -1, -1, new long[] { 1, imgSize, imgSize, 3 }, imgBuffer);
                
                var fetch = sess.Run(new[] { "output" }, new Dictionary<string, Tensor>() { { "input_image", imgTensor }, { "phase_train", new Tensor(false) }, { "keep_prob", new Tensor(1.0f) } });
                var result = (float[,])fetch[0].GetValue();
                bool isOpen = false;
                float percent;
                if(result[0,0] > result[0,1])
                {
                    isOpen = false;
                    percent = result[0,0];
                }
                else
                {
                    isOpen = true;
                    percent = result[0,1];
                }
                Logger.Log(this, $"{result[0,0]}, {result[0,1]} ");

                foreach (var t in fetch)
                    t.Dispose();
                fetch = null;

                imgTensor.Dispose();
                imgTensor = null;

                var data = new EyeOpenData(isOpen, percent);
                eye.OpenData = data;
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
