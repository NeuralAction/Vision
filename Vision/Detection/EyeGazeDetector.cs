using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public class EyeGazeDetector : IDisposable
    {
        public readonly static ManifestResource ModelResourceCpu = new ManifestResource("Vision.Detection", "frozen_cpu.pb");
        public readonly static ManifestResource ModelResourceGpu = new ManifestResource("Vision.Detection", "frozen_gpu.pb");
        public static object ModelLocker = new object();
        public static Graph ModelGraph;

        Session sess;

        public EyeGazeDetector()
        {
            Logger.Log(this, "Start model load");
            if(ModelGraph == null)
            {
                ModelGraph = new Graph();
                ModelGraph.ImportPb(Storage.LoadResource(ModelResourceCpu));
            }
            sess = new Session(ModelGraph);
            Logger.Log(this, "Finished model load");
        }

        public Point Detect(EyeRect eye, VMat frame)
        {
            Profiler.Start("GazeDetect");

            Point pt = new Point(0, 0);
            lock (ModelLocker)
            {
                using (VMat mat = eye.ROI(frame))
                {
                    if (!mat.IsEmpty)
                    {
                        using (Tensor imgTensor = Tools.VMatRGB2Tensor(mat, 224, 224, new long[] { 1, 224, 224, 3 }))
                        {
                            Tensor[] fetch = sess.Run(new string[] { "output" },
                                                        new Dictionary<string, Tensor>() { { "input_image", imgTensor }, { "phase_train", new Tensor(true) }, { "keep_prob", new Tensor(1.0f) } });

                            Tensor result = fetch[0];
                            Logger.Log(this, $"{result.ToString()}");
                            float[,] output = (float[,])result.GetValue();
                            pt.X = (output[0, 0] / 360);
                            pt.Y = (output[0, 1] / 360);
                        }
                    }
                }
            }

            Profiler.End("GazeDetect");
            return pt;
        }

        public void Dispose()
        {
            sess.Dispose();
        }
    }
}
