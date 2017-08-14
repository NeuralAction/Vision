using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Cv
{
    public abstract class CascadeClassifier : VirtualObject, IDisposable
    {
        public readonly static ManifestResource HaarCascadeFrontalFaceAlt = new ManifestResource("Vision.Cv", "haarcascade_frontalface_alt.xml");
        public readonly static ManifestResource HaarCascadeEye = new ManifestResource("Vision.Cv", "haarcascade_eye.xml");
        public readonly static ManifestResource HaarCascadeEyeTreeEyeGlasses = new ManifestResource("Vision.Cv", "haarcascade_eye_tree_eyeglasses.xml");
        public readonly static ManifestResource HaarCascadeMcsEyePairSmall = new ManifestResource("Vision.Cv", "haarcascade_mcs_eyepair_small.xml");
        public readonly static ManifestResource LbpCascadeFrontalFaceImproved = new ManifestResource("Vision.Cv", "lbpcascade_frontalface_improved.xml");
        public readonly static ManifestResource DefaultFaceXmlName = HaarCascadeFrontalFaceAlt;
        public readonly static ManifestResource DefaultEyesXmlName = HaarCascadeEyeTreeEyeGlasses;
        
        public abstract Rect[] DetectMultiScale(VMat mat, double scaleFactor = 1.1, int minNeighbors = 3, HaarDetectionType flags = HaarDetectionType.Zero, Size minSize = null, Size maxSize = null);

        public abstract void Dispose();

        public static CascadeClassifier New(ManifestResource resource, bool overwrite = true)
        {
            return Core.Cv.CreateCascadeClassifier(Storage.LoadResource(resource, overwrite).AbosolutePath);
        }

        public static CascadeClassifier New(FileNode file)
        {
            return Core.Cv.CreateCascadeClassifier(file.AbosolutePath);
        }

        public static CascadeClassifier New(string filePath)
        {
            return Core.Cv.CreateCascadeClassifier(filePath);
        }
    }
}
