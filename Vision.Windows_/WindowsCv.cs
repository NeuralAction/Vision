using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision;
using Vision.Cv;

namespace Vision.Windows
{
    public class WindowsCv : Cv.Cv
    {
        public WindowsCv()
        {
            OpenCvSharp.Windows.NativeBindings.Init();
            SharpFace.Windows.Native.Init();
        }

        public override void CloseAllWindows()
        {
            Cv2.DestroyAllWindows();
        }

        public override void CloseWindow(string name)
        {
            Cv2.DestroyWindow(name);
        }

        protected override void InternalImgShow(string name, Mat img)
        {
            Cv2.ImShow(name, img);
        }

        protected override Mat InternalImgRead(string path)
        {
            return Cv2.ImRead(path);
        }

        protected override void InternalImgWrite(string name, Mat img, int quality)
        {
            Cv2.ImWrite(name, img, new ImageEncodingParam(ImwriteFlags.JpegQuality, quality));
        }
    }
}
