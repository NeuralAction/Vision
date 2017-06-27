using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Vision;

namespace EyeGazeGen
{
    /// <summary>
    /// ModelViewer.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ModelViewer : Window
    {
        EyeGazeModel model;
        public ModelViewer(Window wnd, EyeGazeModel model)
        {
            this.model = model;

            InitializeComponent();

            Owner = wnd;
            Title = model.SessionName;

            viewControl.Model = model;
            viewControl.Update();
        }

        private void Bt_Create_EyesModel_Click(object sender, RoutedEventArgs e)
        {
            DirectoryNode dir = model.Directory.GetDirectory($"[{DateTime.Now.ToString()}] EyesSubModule");
            Storage.FixPathChars(dir);
            if (!dir.IsExist)
                dir.Create();
            if (dir != null)
            {
                FileNode text = dir.NewFile("model.txt");
                StringBuilder build = new StringBuilder();
                build.AppendLine($"scr:{model.ScreenSize.Width},{model.ScreenSize.Height}");
                build.AppendLine($"sub:{model.Directory.Name}");
                text.WriteText(build);

                using (EyesDetector detector = new EyesDetector(new EyesDetectorXmlLoader()))
                {
                    int count = 0;
                    detector.MaxSize = 480;
                    detector.MaxFaceSize = 420;
                    detector.EyesScaleFactor = 1.05;
                    detector.FaceScaleFactor = 1.05;
                    detector.EyesMinFactor = 0.01;
                    detector.EyesMaxFactor = 1;

                    foreach (EyeGazeModelElement ele in model.Elements)
                    {
                        count++;
                        using (VMat frame = Core.Cv.ImgRead(ele.File))
                        {
                            FaceRect[] faces = detector.Detect(frame);
                            if (faces.Length > 0 && faces[0].LeftEye != null)
                            {
                                using(VMat eyeROI = faces[0].LeftEye.RoiCropByPercent(frame))
                                {
                                    FileNode eyeFile = dir.GetFile($"{ele.Index},{ele.Point.X},{ele.Point.Y}.jpg");
                                    eyeROI.NormalizeRGB();
                                    Core.Cv.ImgWrite(eyeFile, eyeROI, 92);
                                }
                            }
                        }
                        Logger.Log($"Extracted [{count}/{model.Elements.Count}]");
                    }
                }
            }
            else
            {
                Logger.Error("Cant Create Dir");
                MessageBox.Show("CantCreateDir");
            }
        }

        private void Bt_ChangeSize_Click(object sender, RoutedEventArgs e)
        {
            double newWidth;
            double newHeight;
            try
            {
                string[] spl = Tb_NewSize.Text.Split(',');
                newWidth = Convert.ToDouble(spl[0]);
                newHeight = Convert.ToDouble(spl[1]);
            }
            catch (Exception ex)
            {
                Logger.Error("Cant convert input");
                return;
            }

            Vision.Size oldSize = model.ScreenSize;
            Vision.Size newSize = new Vision.Size(newWidth, newHeight);

            foreach(EyeGazeModelElement ele in model.Elements)
            {
                Logger.Log($"{ele.Point} To");
                ele.Point = LayoutHelper.ResizePoint(ele.Point, oldSize, newSize, Vision.Stretch.Uniform);
                Logger.Log($"{ele.Point}");

                FileNode newFile = model.Directory.GetFile(EyeGazeModelElement.GetFileName(ele));
                TryMove(ele.File, newFile);
                ele.File = newFile;
            }

            model.ScreenSize = newSize;

            FileNode node = model.ModelTxt;
            if(node.IsExist)
                node.Delete();
            node.Create();
            using(Stream stream = node.Open())
                model.WriteModelText(stream);
            Logger.Log("ModelTXT writed");
        }

        private void TryMove(FileNode source, FileNode dist, int retry = 0, Exception innerException = null)
        {
            if(retry > 5)
            {
                throw new Exception("FileCopy Error", innerException);
            }

            try
            {
                source.Move(dist);
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
                Core.Sleep(500);
                TryMove(source, dist, retry + 1, ex);
            }
        }
    }
}
