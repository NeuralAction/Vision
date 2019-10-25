using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Tensorflow;
using Vision.Cv;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;

namespace Vision.ONNX
{
    public static class Util
    {
        public static ONNXTensor MatArray2ONNXTensor(Mat[] array, float[] outputBuffer, float[][] channelBuffer, NormalizeMode normalizeMode = NormalizeMode.ZeroOne)
        {
            var w = array[0].Width;
            var h = array[0].Height;

            if (outputBuffer.Length != array.Length * w * h)
                throw new Exception("size not matched");
            if (channelBuffer.Length != array.Length)
                throw new Exception("channels not matched");
            if (channelBuffer[0].Length != w * h)
                throw new Exception("size not matched");

            for (int c = 0; c < array.Length; c++)
            {
                var channel = array[c];
                
                if (channel.Width != w || channel.Height != h)
                    throw new Exception("nope");

                var chbuf = channel.GetArray(channelBuffer[c]);
                if (chbuf.Length != w * h)
                    throw new Exception("something is wrong");

                Array.Copy(chbuf, 0, outputBuffer, c * w * h, w * h);
            }

            var imgBuf = CreateVector.Dense(outputBuffer);
            switch (normalizeMode)
            {
                case NormalizeMode.ZeroMean:
                    imgBuf.Divide(127.5f, imgBuf);
                    imgBuf.Subtract(1.0f, imgBuf);
                    break;
                case NormalizeMode.ZeroOne:
                    imgBuf.Divide(255.0f, imgBuf);
                    break;
                case NormalizeMode.CenterZero:
                    imgBuf.Subtract(imgBuf.Average(), imgBuf);
                    imgBuf.Divide((float)Statistics.StandardDeviation(imgBuf), imgBuf);
                    break;
                default:
                    throw new NotImplementedException("unknown one");
            }
            outputBuffer = imgBuf.Storage.AsArray();

            var tensor = new ONNXTensor() { Buffer = outputBuffer, Shape = new long[] { 1, array.Length, h, w } };
            return tensor;
        }
    }
    public class ONNXBinding
    {
        public string Name { get; set; }
    }

    public class ONNXTensor
    {
        public long[] Shape { get; set; }
        public float[] Buffer { get; set; }
    }

    public abstract class ONNXSession : IDisposable
    {
        public bool IsFP16 { get; set; } = false;
        public abstract List<ONNXBinding> GetInputs();
        public abstract List<ONNXBinding> GetOutputs();
        public abstract List<ONNXTensor> Run(IEnumerable<string> outputs, Dictionary<string, ONNXTensor> feedDict);
        public abstract void Dispose();
    }

    public abstract class ONNXRuntime
    {
        public virtual bool UseGPU { get; protected set; }

        public static ONNXRuntime Instance;

        public abstract ONNXSession GetSession(Stream stream);
    }
}
