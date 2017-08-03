using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Detection
{
    public class EyeGazeModel
    {
        public static bool IsModel(DirectoryNode node)
        {
            return node.GetFile("model.txt").IsExist;
        }

        public DirectoryNode Directory { get; set; }
        public FileNode ModelTxt { get { return Directory.GetFile("model.txt"); } }

        public string SessionName { get; set; }
        public string TimeStamp { get; set; }
        public string SubmoduleDesc { get; set; }
        public Size ScreenPixelSize { get; set; }
        public Size ScreenSize { get; set; }
        public Point3D ScreenOrigin { get; set; }

        public List<EyeGazeModelElement> Elements { get; set; } = new List<EyeGazeModelElement>();

        public EyeGazeModel()
        {

        }

        public EyeGazeModel(DirectoryNode node)
        {
            Load(node);
        }

        public void Load(DirectoryNode node)
        {
            if (!IsModel(node))
                throw new Exception("this directory is not eye gaze model");

            Directory = node;

            //read metadata
            string dirname = System.IO.Path.GetFileName(node.AbosolutePath);
            StringBuilder builder = new StringBuilder();
            bool time = true;
            foreach (char c in dirname)
            {
                builder.Append(c);
                if (c == ']' && time)
                {
                    TimeStamp = builder.ToString().Trim();
                    builder.Clear();
                    time = false;
                }
            }
            SessionName = builder.ToString().Trim();

            FileNode modelTXT = Directory.GetFile("model.txt");
            ReadModelTxt(modelTXT);

            //read model
            Elements.Clear();
            FileNode[] files = Directory.GetFiles();
            foreach(FileNode file in files)
            {
                EyeGazeModelElement ele = new EyeGazeModelElement(file);
                if (ele.Loaded)
                    Elements.Add(ele);
            }
        }

        public void ReadModelTxt(FileNode file)
        {
            string[] lines = file.ReadLines();
            foreach (string line in lines)
            {
                string[] spl = line.Split(':');
                string content = spl[1];
                if (line.StartsWith("scr:"))
                {
                    string[] screenSizes = content.Split(',');
                    ScreenPixelSize = new Size(Convert.ToDouble(screenSizes[0]), Convert.ToDouble(screenSizes[1]));
                }
                else if (line.StartsWith("sub:"))
                {
                    SubmoduleDesc = content;
                }
                else if (line.StartsWith("scrmm:"))
                {
                    string[] screenSizes = content.Split(',');
                    ScreenSize = new Size(Convert.ToDouble(screenSizes[0]), Convert.ToDouble(screenSizes[1]));
                }
                else if (line.StartsWith("scrorigin:"))
                {
                    string[] screenSizes = content.Split(',');
                    ScreenOrigin = new Point3D(Convert.ToDouble(screenSizes[0]), Convert.ToDouble(screenSizes[1]), Convert.ToDouble(screenSizes[2]));
                }
                else
                {
                    Logger.Error("Exception while reading model.txt: " + line);
                }
            }

            if(ScreenPixelSize == null)
            {
                ScreenPixelSize = new Size(1920, 1080);
                Logger.Error("ScreenPixelSize == null !");
            }

            if(ScreenSize == null)
            {
                ScreenSize = new Size(1920 / 3.84, 1080 / 3.84);
                Logger.Error("ScreenSize == null !");
            }

            if (ScreenOrigin == null)
            {
                ScreenOrigin = new Point3D(-ScreenSize.Width / 2, 0, 0);
                Logger.Error("ScreenOrigin == null !");
            }
        }

        public void WriteModelText(Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                if(ScreenPixelSize != null)
                    writer.WriteLine($"scr:{ScreenPixelSize.Width},{ScreenPixelSize.Height}");
                if (ScreenSize != null)
                    writer.WriteLine($"scrmm:{ScreenSize.Width},{ScreenSize.Height}");
                if (ScreenOrigin != null)
                    writer.WriteLine($"scrorigin:{ScreenOrigin.X},{ScreenOrigin.Y},{ScreenOrigin.Z}");
                if (SubmoduleDesc != null)
                    writer.WriteLine($"sub:{SubmoduleDesc}");
            }
        }
    }

    public class EyeGazeModelElement
    {
        public static string GetFileName(EyeGazeModelElement ele)
        {
            return $"{ele.Index},{ele.Point.X},{ele.Point.Y}.jpg";
        }

        public bool Loaded { get; set; } = false;
        public int Index { get; set; }
        public Point Point { get; set; }
        public FileNode File { get; set; }

        public EyeGazeModelElement(FileNode node)
        {
            File = node;
            string name = File.Name;
            if (name.EndsWith(".jpg"))
            {
                name = name.Replace(".jpg", "");
                string[] args = name.Split(',');
                Index = Convert.ToInt32(args[0]);
                Point = new Point(Convert.ToDouble(args[1]), Convert.ToDouble(args[2]));
                Loaded = true;
            }
        }
    }
}
