using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision;
using Vision.ONNX;

namespace Vision.Windows
{
    public class WindowsONNXSession : ONNXSession
    {
        public WindowsONNXSession(Stream stream)
        {

        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override List<ONNXBinding> GetInputs()
        {
            throw new NotImplementedException();
        }

        public override List<ONNXBinding> GetOutputs()
        {
            throw new NotImplementedException();
        }

        public override List<ONNXTensor> Run(List<string> outputs, Dictionary<string, ONNXTensor> feedDict)
        {
            throw new NotImplementedException();
        }
    }

    public class WindowsONNXRuntime : ONNXRuntime
    {
        public override ONNXSession GetSession(Stream stream)
        {
            return new WindowsONNXSession(stream);
        }
    }
}
