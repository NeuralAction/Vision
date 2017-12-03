using OpenCvSharp.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;

namespace Vision.Tests
{
    public class ImageProcTests
    {
        Capture c;

        public void Run()
        {
            Logger.Log(this, "E key to exit");
            Logger.Log(this, "G key to gray");
            Logger.Log(this, "N key to RGB normalization");
            Logger.Log(this, "+ key and - key to change value");

            using (c = Capture.New(0))
            {
                c.FrameReady += C_FrameReady;
                c.Start();
                c.Wait();
            }
        }

        bool fclahe = false;
        bool fgray = false;
        double vclip = 1;
        private void C_FrameReady(object sender, FrameArgs e)
        {
            switch (e.LastKey)
            {
                case 'e':
                    e.Break = true;
                    Core.Cv.CloseAllWindows();
                    return;
                case 'n':
                    fclahe = !fclahe;
                    break;
                case 'g':
                    fgray = !fgray;
                    break;
                case '+':
                case '=':
                    if (fclahe)
                        vclip++;
                    break;
                case '-':
                    if (fclahe)
                        vclip--;
                    break;
            }

            if (!e.Mat.IsEmpty)
            {
                if (fclahe)
                {
                    vclip = Math.Max(1, vclip);
                    e.Mat.NormalizeRGB(e.Mat, vclip);
                }
                if (fgray)
                    e.Mat.ConvertColor(OpenCvSharp.ColorConversionCodes.BGR2GRAY);

                Core.Cv.ImgShow("imgproc", e.Mat);
            }
        }
    }
}
