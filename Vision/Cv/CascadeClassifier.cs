using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public abstract class CascadeClassifier : VirtualObject, IDisposable
    {
        public abstract Rect[] DetectMultiScale(VMat mat, double scaleFactor = 1.1, int minNeighbors = 3, HaarDetectionType flags = HaarDetectionType.Zero, Size minSize = null, Size maxSize = null);

        public abstract void Dispose();

        public static CascadeClassifier New(string filePath)
        {
            return Core.Cv.CreateCascadeClassifier(filePath);
        }
    }
}
