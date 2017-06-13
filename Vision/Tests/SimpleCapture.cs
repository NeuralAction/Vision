using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Tests
{
    public class SimpleCapture
    {
        Capture capture;

        public void Run()
        {
            Logger.Log("E key to exit.");

            capture = Capture.New(0);
            capture.FrameReady += Capture_FrameReady;
            capture.Start();
            capture.Join();
            capture.Dispose();
        }

        private void Capture_FrameReady(object sender, FrameArgs e)
        {
            Profiler.Count("FPS");

            if(e.LastKey == 'e')
            {
                capture.Stop();
                Core.Cv.CloseAllWindows();
                return;
            }

            Core.Cv.ImgShow("mat", e.VMat);
        }
    }
}
