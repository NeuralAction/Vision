using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vision;
using Vision.Detection;
using Vision.Tests;

namespace WindowsTests
{
    class Program
    {
        static string[] HelpMessages = new string[]
        {
            "Help Informations ===",
            Core.ProjectInfromation,
            Core.VersionInfromation,
            "",
            "FACE    \t Face detection test",
            "VIDEO   \t Video playing test",
            "CAM     \t Camera streaming test",
            "IMGPROC \t Image processing test",
            "THREED  \t 3D transform test",
            "OBJCLS  \t Object classify test (Google Inception)",
            "INFO    \t Build Information of CV",
            "CLR     \t Clear console",
            "EXIT    \t Exit program"
        };

        [STAThread]
        static void Main(string[] args)
        {
            Vision.Windows.WindowsCore.Init();

            Program prg = new Program();
            prg.Run();
        }

        OpenFileDialog ofd = new OpenFileDialog() { Title = "Select File" };

        public void Run()
        {
            while (true)
            {
                Console.Write(">>> ");
                string read_raw = Console.ReadLine();
                string read = read_raw.ToLower();

                switch (read)
                {
                    case "info":
                        Console.WriteLine(Core.Cv.BuildInformation);
                        break;
                    case "clr":
                        Console.Clear();
                        break;
                    case "video":
                        SimpleVideo();
                        break;
                    case "imgproc":
                        ImgProcTest();
                        break;
                    case "threed":
                        ThreeDimTest();
                        break;
                    case "objcls":
                        ObjectClassify();
                        break;
                    case "face":
                        FaceDetection();
                        break;
                    case "cam":
                        SimpleCapture();
                        break;
                    case "help":
                        foreach (string line in HelpMessages)
                        {
                            Console.WriteLine(line);
                        }
                        break;
                    case "exit":
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Unknown Command : \"{0}\"", read_raw);
                        break;
                }
            }
        }

        public void ThreeDimTest()
        {
            ThreeDimTests t = new ThreeDimTests();
            t.Run();
        }

        public void ObjectClassify()
        {
            InceptionTests t = null;
            try
            {
                Console.Write("Index [Press enter to detect from file] >>> ");
                string inp = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(inp))
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    if(DialogResult.OK == ofd.ShowDialog())
                    {
                        t = new InceptionTests(ofd.FileName);
                    }
                }
                else
                {
                    int index = Convert.ToInt32(inp);
                    t = new InceptionTests(index);
                }
            }
            catch
            {
                return;
            }

            if(t!= null)
                t.Run();
        }

        public void ImgProcTest()
        {
            ImageProcTests t = new ImageProcTests();
            t.Run();
        }

        public void SimpleVideo()
        {
            if (DialogResult.OK == ofd.ShowDialog())
            {
                SimpleVideoPlayer player = new SimpleVideoPlayer(ofd.FileName);
                player.Run();
            }
        }

        public void SimpleCapture()
        {
            SimpleCapture cap = new SimpleCapture();
            cap.Run();
        }

        public void FaceDetection()
        {
            FaceDetectionTests detect = null;

            Console.Write("Index [Press enter to detect from file] >>> ");
            string cmd = Console.ReadLine();
            if (!string.IsNullOrEmpty(cmd))
            {
                int ind = -1;
                try
                {
                    ind = Convert.ToInt32(cmd);
                }
                catch
                {
                    return;
                }
                detect = new FaceDetectionTests(ind, new FaceDetectorXmlLoader(), new FlandmarkModelLoader());
            }
            else
            {
                if (DialogResult.OK == ofd.ShowDialog())
                {
                    detect = new FaceDetectionTests(ofd.FileName, new FaceDetectorXmlLoader(), new FlandmarkModelLoader());
                }
            }

            if (detect != null)
            {
                detect.Detected += (obj, arg) =>
                {

                };
                detect.Run();
            }
        }
    }
}
