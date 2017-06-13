using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision;

namespace Vision.Tests
{
    public class SimpleVideoPlayer
    {
        string path;
        string windowName = "Vision - Windows Tests - SimpleVideoPlayer";

        public SimpleVideoPlayer(string filePath)
        {
            path = filePath;
        }

        public void Run()
        {
            Logger.Log("Press E to Exit");

            Capture capture = Capture.New(path);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            long lastMs = 0;

            while (true)
            {
                lastMs = sw.ElapsedMilliseconds;

                if (capture.IsOpened)
                {
                    using (VMat mat = capture.QueryFrame())
                    {
                        if (mat != null && !mat.IsEmpty)
                        {
                            Core.Cv.ImgShow(windowName, mat);
                        }
                    }
                }

                char c = Core.Cv.WaitKey((int)Math.Max(1, Math.Min((1000 / capture.FPS) - (sw.ElapsedMilliseconds - lastMs), 1000)));

                if(c == 'e')
                {
                    Core.Cv.CloseAllWindows();

                    return;
                }
            }
        }
    }
}
