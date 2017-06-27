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
                ModelGraph.ImportPb(Storage.LoadResource(ModelResourceCpu, true));
            }
            sess = new Session(ModelGraph);
            Logger.Log(this, "Finished model load");
        }

        public Point Detect(FaceRect face, VMat frame)
        {
            EyeRect lefteye = face.LeftEye;

            if (lefteye == null && face.Children.Count > 0)
                lefteye = face.Children[0];

            if (lefteye == null)
                return null;

            return Detect(lefteye, frame);
        }

        public Point Detect(EyeRect eye, VMat frame)
        {
            Profiler.Start("GazeDetect");

            Point pt = new Point(0, 0);
            lock (ModelLocker)
            {
                using (VMat mat = eye.RoiCropByPercent(frame))
                {
                    if (!mat.IsEmpty)
                    {
                        mat.Resize(new Size(160, 160));
                        mat.NormalizeRGB();

                        Tensor imgTensor = Tools.VMatRGB2Tensor(mat, -1, -1, new long[] { 1, 160, 160, 3 });
                        Tensor[] fetch = sess.Run(new string[] { "output" },
                                                    new Dictionary<string, Tensor>() { { "input_image", imgTensor }, { "phase_train", new Tensor(true) }, { "keep_prob", new Tensor(1.0f) } });

                        Tensor result = fetch[0];
                        float[,] output = (float[,])result.GetValue();
                        pt.X = Math.Max(0, Math.Min(1, (output[0, 0] / 360)));
                        pt.Y = Math.Max(0, Math.Min(1, (output[0, 1] / 360)));
                        foreach(Tensor t in fetch)
                        {
                            t.Dispose();
                        }
                        fetch = null;
                        result = null;
                        imgTensor.Dispose();
                        imgTensor = null;
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
