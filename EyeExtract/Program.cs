using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vision;
using Vision.Cv;
using Vision.Detection;

namespace EyeExtract
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Vision.Windows.WindowsCore.Init();

            while (true)
            {
                Console.Write(">>> ");
                string read = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(read))
                {
                    string readLower = read.ToLower();
                    switch (readLower)
                    {
                        case "eyes":
                            EyeExtract();
                            break;
                        default:
                            Console.WriteLine($"Unknown Command : {read}");
                            break;
                    }
                }
            }
        }

        static void EyeExtract()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            if (DialogResult.OK == ofd.ShowDialog())
            {
                Console.Write("caption? >>> ");
                string caption = Console.ReadLine();

                Console.Write("leftOnly? [Y/n] >>> ");
                string readleft = Console.ReadLine();
                bool useleft = true;
                if (!string.IsNullOrWhiteSpace(readleft))
                {
                    var readleftLower = readleft.ToLower();
                    if (readleftLower == "n")
                    {
                        useleft = false;
                    }
                    else if (readleftLower == "y")
                    {
                        useleft = true;
                    }
                    else
                    {
                        return;
                    }
                }

                var parent = Storage.Root.GetDirectory($"[{DateTime.Now.ToString()}] eyes");
                Storage.FixPathChars(parent);
                if (!parent.IsExist)
                    parent.Create();
                var parentLeft = parent.GetDirectory("left");
                if (!parentLeft.IsExist && !useleft)
                    parentLeft.Create();
                var parentRight = parent.GetDirectory("right");
                if (!parentRight.IsExist && !useleft)
                    parentRight.Create();

                FaceDetector detector = new FaceDetector()
                {
                    LandmarkDetect = true,
                    SmoothVectors = true,
                    EyesDetectCascade = true,
                    EyesDetectLandmark = true,
                    SmoothLandmarks = false,
                    EyesMinFactor = 0.1,
                    EyesMaxFactor = 0.8,
                    EyesScaleFactor = 1.2,
                    FaceMaxFactor = 1,
                    FaceMinFactor = 0.1,
                    FaceScaleFactor = 1.2,
                    LandmarkSolve = true,
                    MaxFaceSize = 200,
                    MaxSize = 400,
                    Interpolation = InterpolationFlags.Cubic
                };

                int id = 0;
                foreach (var file in ofd.FileNames)
                {
                    if (Storage.IsImage(file))
                    {
                        using (Mat mat = Core.Cv.ImgRead(file))
                        {
                            var rects = detector.Detect(mat);
                            if (rects != null && rects.Length > 0)
                            {
                                var face = rects[0];
                                if (useleft)
                                {
                                    if (face.LeftEye != null)
                                    {
                                        using (Mat eye = face.LeftEye.RoiCropByPercent(mat))
                                            Core.Cv.ImgWrite(parent.GetFile($"{id},{caption}.jpg"), eye, 92);
                                    }
                                }
                                else
                                {
                                    if (face.LeftEye != null && face.RightEye != null)
                                    {
                                        var filename = $"{id},{caption}.jpg";

                                        using (Mat eye = face.LeftEye.RoiCropByPercent(mat))
                                            Core.Cv.ImgWrite(parentLeft.GetFile(filename), eye, 92);

                                        using (Mat eye = face.RightEye.RoiCropByPercent(mat))
                                            Core.Cv.ImgWrite(parentRight.GetFile(filename), eye, 92);
                                    }
                                }
                            }
                        }
                        id++;
                        Console.WriteLine($"Extracted[{id}/{ofd.FileNames.Length}]");
                    }
                }

                detector.Dispose();
            }
        }
    }
}
