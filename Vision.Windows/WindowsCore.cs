using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vision.Windows
{
    public class WindowsCore : Core
    {
        public override void Initialize()
        {
            InitLogger(new Logger.WriteMethodDelegate(Console.Write));

            InitStorage(new WindowsStorage());

            InitCv(new WindowsCv());

            TensorFlowSharp.Windows.NativeBinding.Init();
            TensorFlow.NativeBinding.PrintFunc = new TensorFlow.NativeBinding.Print((s) => { Logger.Log(s); });
        }

        protected override void InternalSleep(int duration)
        {
            Thread.Sleep(duration);
        }
    }
}
