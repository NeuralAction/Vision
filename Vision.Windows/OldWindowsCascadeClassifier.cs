//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Vision.Cv;

//namespace Vision.Windows
//{
//    public class WindowsCascadeClassifier : Cv.CascadeClassifier
//    {
//        public OpenCvSharp.CascadeClassifier InnerCascade;
//        public override object Object
//        {
//            get { return InnerCascade; }
//            set { throw new NotImplementedException(); }
//        }

//        public WindowsCascadeClassifier(string filePath)
//        {
//            InnerCascade = new OpenCvSharp.CascadeClassifier();
//            InnerCascade.Load(filePath);
//        }

//        public override Rect[] DetectMultiScale(VMat mat, double scaleFactor = 1.1, int minNeighbors = 3, HaarDetectionType flags = HaarDetectionType.Zero, Size minSize = null, Size maxSize = null)
//        {
//            OpenCvSharp.Rect[] rects = InnerCascade.DetectMultiScale((OpenCvSharp.Mat)mat.Object, scaleFactor, minNeighbors, (OpenCvSharp.HaarDetectionType)flags, 
//                (minSize == null) ? OpenCvSharp.Size.Zero : new OpenCvSharp.Size(minSize.Width, minSize.Height), 
//                (maxSize == null) ? OpenCvSharp.Size.Zero : new OpenCvSharp.Size(maxSize.Width, maxSize.Height));

//            Rect[] retrun = new Rect[rects.Length];
//            for(int i =0; i<rects.Length; i++)
//            {
//                OpenCvSharp.Rect rect = rects[i];
//                retrun[i] = new Rect(rect.X, rect.Y, rect.Width, rect.Height);
//            }

//            return retrun;
//        }

//        public override void Dispose()
//        {
//            if (InnerCascade != null)
//            {
//                InnerCascade.Dispose();
//                InnerCascade = null;
//            }
//        }
//    }
//}
