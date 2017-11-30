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

namespace EyeCalibTest
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Vision.Windows.WindowsCore.Init();

            OpenFileDialog ofd = new OpenFileDialog()
            {
                InitialDirectory = Environment.CurrentDirectory
            };

            if(DialogResult.OK == ofd.ShowDialog())
            {
                var file = new Vision.FileNode(ofd.FileName, true);
                EyeGazeCalibrationLog log = new EyeGazeCalibrationLog(file);
                log.Load();

                LinearEyeGazeCalibratorEngine e = new LinearEyeGazeCalibratorEngine();
                e.SetData(log.Data);
                e.Train();

                var plot = log.Plot(ScreenProperties.CreatePixelScreen(1920, 1080, 96), new EyeGazeCalibrater() { Engine = e });
                Core.Cv.ImgShow("plt", plot, 0, true);
            }
        }
    }
}
