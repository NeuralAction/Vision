using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;

namespace Vision.Detection
{
    public class FlandmarkModelLoader
    {
        public static ManifestResource DefaultModel => new ManifestResource("Vision.Cv", "flandmark_model.dat");

        public FileNode Data { get; set; }

        public FlandmarkModelLoader()
        {
            Data = Storage.LoadResource(DefaultModel, true);
        }
    }

    public class FaceLandmarkDetector
    {
        public Interpolation Interpolation { get => detector.Inter; set => detector.Inter = value; }

        Flandmark detector;

        public FaceLandmarkDetector(FileNode node)
        {
            detector = new Flandmark(node);
        }

        public FaceLandmarkDetector(FlandmarkModelLoader loader) : this(loader.Data)
        {

        }

        public FaceLandmarkDetector() : this(new FlandmarkModelLoader())
        {

        }

        public Point[] Detect(VMat mat, FaceRect face, bool calcEyes = false, int[] margin = null)
        {
            if(margin == null)
            {
                margin = new int[] { 10, 10 };
            }

            var pt = detector.Detect(mat, new int[] { (int)face.X, (int)face.Y, (int)(face.X + face.Width), (int)(face.Y + face.Height) }, margin);
            face.Landmarks = pt;

            if(calcEyes)
            {
                CalcEyes(face);
            }

            return pt;
        }

        public static void CalcEyes(FaceRect face)
        {
            var pt = face.Landmarks;

            if (face.Children.Count == 0 && pt != null)
            {
                EyeRect left = GetEyeRect(face, new Point[] { pt[1], pt[5] });
                EyeRect right = GetEyeRect(face, new Point[] { pt[2], pt[6] });
                face.Children.Add(left);
                face.Children.Add(right);
            }
        }

        private static EyeRect GetEyeRect(FaceRect face, Point[] landmark)
        {
            Point center = new Point(landmark[0].X + landmark[1].X, landmark[0].Y + landmark[0].Y);
            center.X = center.X / 2 - face.X;
            center.Y = center.Y / 2 - face.Y;
            double diff = Math.Abs(landmark[0].X - landmark[1].X);
            double width = face.Width * 0.25;
            EyeRect r = new EyeRect(face, new Rect(center.X - width / 2, center.Y - width / 2, width, width));
            return r;
        }
    }
}
