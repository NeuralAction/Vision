using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using OpenCV.Core;

namespace Vision.Android
{
    public class AndroidCascadeClassifier : CascadeClassifier
    {
        public override object Object
        {
            get { return InnerCascade; }
            set { throw new NotImplementedException(); }
        }
        public OpenCV.ObjDetect.CascadeClassifier InnerCascade;

        string filePath;

        public AndroidCascadeClassifier(string filepath)
        {
            filePath = filepath;

            InnerCascade = new OpenCV.ObjDetect.CascadeClassifier(filepath);
            if (InnerCascade.Empty())
            {
                throw new ArgumentException("Errored while create cascade");
            }
        }

        public override Rect[] DetectMultiScale(VMat mat, double scaleFactor = 1.1, int minNeighbors = 3, HaarDetectionType flags = HaarDetectionType.ScaleImage, Size minSize = null, Size maxSize = null)
        {
            MatOfRect result = new MatOfRect();

            InnerCascade.DetectMultiScale((Mat)mat.Object, result, scaleFactor, minNeighbors, (int)flags,
                (minSize == null) ? new OpenCV.Core.Size(0,0) : new OpenCV.Core.Size(minSize.Width, minSize.Height),
                (maxSize == null) ? new OpenCV.Core.Size(5000,5000) : new OpenCV.Core.Size(maxSize.Width, maxSize.Height));

            OpenCV.Core.Rect[] rects = result.ToArray();
            Rect[] ret = new Rect[rects.Length];
            for (int i = 0; i < ret.Length; i++)
            {
                OpenCV.Core.Rect rect = rects[i];
                ret[i] = new Rect(rect.X, rect.Y, rect.Width, rect.Height);
            }

            return ret;
        }

        public override void Dispose()
        {
            if(InnerCascade != null)
            {
                InnerCascade.Dispose();
                InnerCascade = null;
            }
        }
    }
}