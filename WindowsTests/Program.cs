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
            "FACE \t Face detection test",
            "VIDEO \t Video playing test",
            "CAM \t Camera streaming test",
            "INFO \t Build Information of CV",
            "CLR \t Clear console",
            "EXIT \t Exit program"
        };

        [STAThread]
        static void Main(string[] args)
        {
            Core.Init(new Vision.Windows.WindowsCore());

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
            FaceDetection detect = null;

            Console.Write("Index [Press enter to detect from file] >>> ");
            string cmd = Console.ReadLine();
            if (!string.IsNullOrEmpty(cmd))
            {
                try
                {
                    int ind = Convert.ToInt32(cmd);
                    detect = new FaceDetection(ind, new EyesDetectorXmlLoader());
                }
                catch
                {
                    Logger.Log("Enter Correct Index");
                }
            }
            else
            {
                if (DialogResult.OK == ofd.ShowDialog())
                {
                    detect = new FaceDetection(ofd.FileName, new EyesDetectorXmlLoader());
                }
            }

            if (detect != null)
            {
                detect.Detected += (obj, arg) =>
                {
                    if (arg.Results != null && arg.Results.Length > 0)
                    {
                        string print = arg.Results.Length + " faces detected.";
                        double sum = 0;
                        double count = 0;
                        foreach (var item in arg.Results[0].Children)
                        {
                            count++;
                            sum += item.Width;
                        }
                        print += " eyes size mean: " + (sum / count).ToString("0.00");
                        Logger.Log(print);
                    }
                };
                detect.Run();
            }
        }
    }
}
