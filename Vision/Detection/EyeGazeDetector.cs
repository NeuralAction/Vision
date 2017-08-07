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
        public readonly static ManifestResource ModelResourceCpu = new ManifestResource("Vision.Detection", "frozen_cpu.pb");
        public readonly static ManifestResource ModelResourceGpu = new ManifestResource("Vision.Detection", "frozen_gpu.pb");
        public static object ModelLocker = new object();
        public static Graph ModelGraph;

        public int ImageSize { get; set; } = 80;
        public double AngleMul { get; set; } = 1;
        public bool UseModification { get; set; } = false;
        public double SensitiveX { get; set; } = 2;
        public double OffsetX { get; set; } = 0.1;
        public double SensitiveY { get; set; } = 2;
        public double OffsetY { get; set; } = -0.05;
        public EyeDirection Direction { get; set; } = EyeDirection.None;

        Session sess;

        public EyeGazeDetector()
        {
            Logger.Log(this, "Start model load");
            if(ModelGraph == null)
            {
                ModelGraph = new Graph();
                ModelGraph.ImportPb(Storage.LoadResource(ModelResourceGpu, true));
            }
            sess = new Session(ModelGraph);
            Logger.Log(this, "Finished model load");
        }

        public Point Detect(FaceRect face, VMat frame, ScreenProperties properties)
        {
            EyeRect lefteye = face.LeftEye;

            if (lefteye == null && face.Children.Count > 0)
                lefteye = face.Children[0];

            if (lefteye == null)
                return null;

            return Detect(lefteye, frame, properties);
        }

        float[] imgbuffer;
        Tensor imgTensor;
        public Point Detect(EyeRect eye, VMat frame, ScreenProperties properties)
        {
            if (eye == null)
                throw new ArgumentNullException("eye");
            if (frame == null || frame.IsEmpty)
                throw new ArgumentNullException("frame");
            if (properties == null)
                throw new ArgumentNullException("properties");
            if (eye.Parent == null)
                throw new ArgumentNullException("eye.parent");

            Profiler.Start("GazeDetect");

            Point pt = new Point(0, 0);
            lock (ModelLocker)
            {
                using (VMat mat = eye.RoiCropByPercent(frame))
                {
                    if (!mat.IsEmpty)
                    {
                        mat.Resize(new Size(ImageSize, ImageSize), 0, 0, Interpolation.NearestNeighbor);
                        
                        if (imgbuffer == null)
                            imgbuffer = new float[ImageSize * ImageSize * 3];
                        imgTensor = Tools.VMatBgr2Tensor(mat, NormalizeMode.ZeroMean, -1, -1, new long[] { 1, ImageSize, ImageSize, 3 }, imgbuffer);
                        Tensor[] fetch = sess.Run(new string[] { "output" },
                            new Dictionary<string, Tensor>() { { "input_image", imgTensor }, { "phase_train", new Tensor(false) }, { "keep_prob", new Tensor(1.0f) } });
                        //new Dictionary<string, Tensor>() { { "input_image", imgTensor }, { "keep_prob", new Tensor(1.0f) } });

                        Tensor result = fetch[0];
                        float[,] output = (float[,])result.GetValue();
                        var x = output[0, 0] / AngleMul * -1;
                        var y = output[0, 1] / AngleMul * -1;
                        if (UseModification)
                        {
                            x = (x + OffsetX) * SensitiveX;
                            y = (y + OffsetY) * SensitiveY;
                        }
                        Vector<double> vec = CreateVector.Dense(new double[] { x, y, -1 });
                        pt = eye.Parent.SolveRayScreenVector(new Point3D(vec.ToArray()), properties, Flandmark.UnitPerMM);

                        foreach(Tensor t in fetch)
                            t.Dispose();

                        fetch = null;
                        result = null;

                        try
                        {
                            imgTensor.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(this, ex);
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
