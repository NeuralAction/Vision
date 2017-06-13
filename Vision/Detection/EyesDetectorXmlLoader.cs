using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public class EyesDetectorXmlLoader
    {
        public readonly static ManifestResource HaarCascadeFrontalFaceAlt = new ManifestResource("Vision.Cv", "haarcascade_frontalface_alt.xml");
        public readonly static ManifestResource HaarCascadeEye = new ManifestResource("Vision.Cv", "haarcascade_eye.xml");
        public readonly static ManifestResource HaarCascadeEyeTreeEyeGlasses = new ManifestResource("Vision.Cv", "haarcascade_eye_tree_eyeglasses.xml");
        public readonly static ManifestResource HaarCascadeMcsEyePairSmall = new ManifestResource("Vision.Cv", "haarcascade_mcs_eyepair_small.xml");
        public readonly static ManifestResource LbpCascadeFrontalFaceImproved = new ManifestResource("Vision.Cv", "lbpcascade_frontalface_improved.xml");
        public readonly static ManifestResource DefaultFaceXmlName = LbpCascadeFrontalFaceImproved;
        public readonly static ManifestResource DefaultEyesXmlName = HaarCascadeEyeTreeEyeGlasses;

        public string FaceXmlPath { get; private set; }
        public string EyeXmlPath { get; private set; }

        public EyesDetectorXmlLoader(string facePath, string eyesPath)
        {
            FaceXmlPath = facePath;
            EyeXmlPath = eyesPath;
        }

        public EyesDetectorXmlLoader(ManifestResource FaceResource, ManifestResource EyesResource)
        {
            FaceXmlPath = Storage.LoadResource(DefaultFaceXmlName, true).AbosolutePath;
            EyeXmlPath = Storage.LoadResource(DefaultEyesXmlName, true).AbosolutePath;
        }

        public EyesDetectorXmlLoader() : this(DefaultFaceXmlName, DefaultEyesXmlName)
        {

        }
    }
}
