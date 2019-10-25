using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision;
using Vision.ONNX;
using Windows.AI.MachineLearning;
using Windows.Storage.Streams;

namespace Vision.Windows
{
    public class WindowsONNXSession : ONNXSession
    {
        static int cacheCount = 0;

        public bool IsGPU { get; set; }

        LearningModel model;
        LearningModelSession sess;
        public WindowsONNXSession(Stream stream, bool isGPU = false)
        {
            var filename = $"ONNXCache_{++cacheCount}.tmp";
            if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
            File.WriteAllBytes(filename, stream.ReadAll());
            model = LearningModel.LoadFromFilePath(filename);
            foreach(var item in model.Metadata)
            {
                Logger.Log($"{item.Key}, {item.Value.GetType().FullName}, {item.Value}");
            }
            sess = new LearningModelSession(model, new LearningModelDevice(LearningModelDeviceKind.Cpu));
        }

        public override void Dispose()
        {
            sess?.Dispose();
            sess = null;
            model?.Dispose();
            model = null;
        }

        List<ONNXBinding> ConvertFeatureDesc(IReadOnlyList<ILearningModelFeatureDescriptor> features)
        {
            var list = new List<ONNXBinding>();
            foreach (var item in features)
            {
                list.Add(new ONNXBinding() { Name = item.Name, });
            }
            return list;
        }

        public override List<ONNXBinding> GetInputs()
        {
            return ConvertFeatureDesc(model.InputFeatures);
        }

        public override List<ONNXBinding> GetOutputs()
        {
            return ConvertFeatureDesc(model.OutputFeatures);
        }

        int evalCount = 0;
        public override List<ONNXTensor> Run(IEnumerable<string> outputs, Dictionary<string, ONNXTensor> feedDict)
        {
            var binding = new LearningModelBinding(sess);
            foreach (var item in feedDict)
            {
                object tensor;
                if (!IsFP16)
                {
                    tensor = TensorFloat.CreateFromArray(item.Value.Shape, item.Value.Buffer);
                    if (IsGPU) {
                        //TODO: Move SoftwareTensor to DX12Tensor
                    }
                }
                else
                {
                    tensor = TensorFloat16Bit.CreateFromArray(item.Value.Shape, item.Value.Buffer);
                }
                binding.Bind(item.Key, tensor);
            }

            var result = sess.Evaluate(binding, $"eval{++evalCount}");
            
            var ret = new List<ONNXTensor>();
            foreach (var item in outputs)
            {
                var tensor = result.Outputs[item] as TensorFloat;
                var vector = tensor.GetAsVectorView().ToArray();
                ret.Add(new ONNXTensor() { Buffer = vector, Shape = tensor.Shape.ToArray() });
            }
            
            return ret;
        }
    }

    public class WindowsONNXRuntime : ONNXRuntime
    {
        public override bool UseGPU { get; protected set; }
        public WindowsONNXRuntime(bool isGPU = false)
        {
            UseGPU = isGPU;
        }
        public override ONNXSession GetSession(Stream stream)
        {
            return new WindowsONNXSession(stream);
        }
    }
}
