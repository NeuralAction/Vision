using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Vision;
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
                var file = new FileNode(ofd.FileName, true);
                EyeGazeCalibrationLog log = new EyeGazeCalibrationLog(file);
                log.Load();
                var plot = log.Plot(ScreenProperties.CreatePixelScreen(1920, 1080, 96));
                Core.Cv.ImgShow("plt", plot);
                Core.Cv.WaitKey(0);
                Core.Cv.CloseAllWindows();

                LinearEyeGazeCalibratorEngine e = new LinearEyeGazeCalibratorEngine();
                e.SetData(log.Data);
                e.Train();

                foreach (var item in log.Data)
                {
                    var vec = item.Value.Face.GazeInfo.Vector;
                    item.Value.Face.GazeInfo.Vector = e.Apply(vec);
                }
                plot = log.Plot(ScreenProperties.CreatePixelScreen(1920, 1080, 96));
                Core.Cv.ImgShow("plt", plot);
                Core.Cv.WaitKey(0);
                Core.Cv.CloseAllWindows();
            }

            Console.ReadLine();
        }
    }
}
