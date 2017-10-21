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

        }

        protected override string GetBuildInformation()
        {
            return Cv2.GetBuildInformation();
        }

        public override void CloseAllWindows()
        {
            Cv2.DestroyAllWindows();
        }

        public override void CloseWindow(string name)
        {
            Cv2.DestroyWindow(name);
        }

        public override char WaitKey(int duration)
        {
            return (char)Cv2.WaitKey(duration);
        }

        protected override Capture CreateCapture(int index)
        {
            return new WindowsCapture(index);
        }

        protected override Capture CreateCapture(string filePath)
        {
            return new WindowsCapture(filePath);
        }

        protected override int GetNumThreads()
        {
            return Cv2.GetNumThreads();
        }

        protected override void SetNumThreads(int t)
        {
            Cv2.SetNumThreads(t);
        }

        protected override bool GetUseOptimized()
        {
            return Cv2.UseOptimized();
        }

        protected override void SetUseOptimized(bool b)
        {
            Cv2.SetUseOptimized(b);
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
            throw new NotImplementedException();
        }
    }
}
