using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vision.Detection;

namespace Vision.Windows
{
    public class WindowsCore : Core
    {
        public static bool IsGpu { get; private set; }

        public static void Init(bool isGpu = false)
        {
            IsGpu = isGpu;

            Init(new WindowsCore());
        }

        public override void Initialize()
        {
            InitLogger(new Logger.WriteMethodDelegate(Console.Write));

            InitStorage(new WindowsStorage());

            InitCv(new WindowsCv());

            TensorFlow.NativeBinding.PrintFunc = new TensorFlow.NativeBinding.Print((s) => { Logger.Log(s); });
            TensorFlowSharp.Windows.NativeBinding.Init(IsGpu);
            if (TensorFlow.NativeBinding.Current.IsGpu)
                Logger.Log("Tensorflow Working With GPU");
            else
                Logger.Log("Tensorflow Working With CPU");

            InitONNX(new WindowsONNXRuntime(IsGpu));
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,
        }

        protected override ScreenProperties InternalGetDefaultScreen()
        {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();
            int LogicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            int PhysicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);

            var scaleFactor = PhysicalScreenHeight / (double)LogicalScreenHeight;
            var dpi = 96 * scaleFactor;

            var scr = Screen.PrimaryScreen.Bounds.Size;

            var ret = ScreenProperties.CreatePixelScreen(new Size(scr.Width * scaleFactor, scr.Height * scaleFactor), dpi);

            return ret;
        }

        protected override void InternalSleep(int duration)
        {
            Thread.Sleep(duration);
        }
    }
}
