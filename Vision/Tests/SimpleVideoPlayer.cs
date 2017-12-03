using OpenCvSharp;
using OpenCvSharp.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision;
using Vision.Cv;

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

            using (Capture capture = Capture.New(path))
            {
                capture.FrameReady += (o, arg) =>
                {
                    if (arg.Mat != null && !arg.Mat.IsEmpty)
                    {
                        Core.Cv.ImgShow(windowName, arg.Mat);
                    }

                    char c = arg.LastKey;

                    if (c == 'e')
                    {
                        Core.Cv.CloseAllWindows();
                        return;
                    }
                };
                capture.Start();
                capture.Wait();
            }
        }
    }
}
