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
using Vision.Cv;
using Vision.Detection;

namespace EyeGazeGen
{
    /// <summary>
    /// ModelViewer.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ModelViewer : Window
    {
        EyeGazeModel model;
        ScreenProperties ScreenProperties;
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
            var dir = model.Directory.GetDirectory($"[{DateTime.Now.ToString()}] EyesSubModule");
            Storage.FixPathChars(dir);
            if (!dir.IsExist)
                dir.Create();

            var dirLeft = dir.GetDirectory("left");
            if (!dirLeft.IsExist)
                dirLeft.Create();

            var dirRight = dir.GetDirectory("right");
            if (!dirRight.IsExist)
                dirRight.Create();

            if (dir != null)
            {
                FileNode text = dir.NewFile("model.txt");
                using(var stream = text.Open())
                {
                    model.WriteModelText(stream);
                }
                var xmlloader = new FaceDetectorXmlLoader();

                List<FaceDetector> detectors = new List<FaceDetector>();
                for (int i = 0; i < Environment.ProcessorCount; i++)
                {
                    detectors.Add(new FaceDetector(xmlloader)
                    {
                        MaxSize = 400,
                        MaxFaceSize = 400,
                        EyesScaleFactor = 1.1,
                        FaceScaleFactor = 1.1,
                        EyesMinFactor = 0.1,
                        EyesMaxFactor = 0.8,
                        EyesDetectCascade = true,
                        EyesDetectLandmark = true,
                        FaceMaxFactor = 0.957,
                        FaceMinFactor = 0.15,
                        Interpolation = Interpolation.Cubic,
                        LandmarkDetect = true,
                        LandmarkSolve = true,
                        SmoothLandmarks = false,
                        SmoothVectors = true
                    });
                }

                ScreenProperties = new ScreenProperties()
                {
                    Origin = model.ScreenOrigin,
                    PixelSize = model.ScreenPixelSize,
                    Size = model.ScreenSize
                };

                object countLocker = new object();
                int count = 0;

                Parallel.ForEach(model.Elements, new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount }, (ele) =>
                {
                    int detectorInd;
                    FaceDetector detector;
                    lock (countLocker)
                    {
                        count++;
                        detectorInd = count % Environment.ProcessorCount;
                        detector = detectors[detectorInd];
                    }

                    using (VMat frame = Core.Cv.ImgRead(ele.File))
                    {
                        if (frame != null && !frame.IsEmpty)
                        {
                            try
                            {
                                FaceRect[] faces = detector.Detect(frame);
                                if (faces != null && faces.Length > 0)
                                {
                                    foreach (var face in faces)
                                    {
                                        if (Math.Abs(face.LandmarkTransformVector[2]) < 7500 && face.LeftEye != null && face.RightEye != null)
                                        {
                                            var rod = face.SolveLookScreenVector(ele.Point, ScreenProperties, Flandmark.UnitPerMM).ToArray();

                                            var filename = $"{ele.Index},{rod[0]},{rod[1]},{rod[2]}.jpg";
                                            FileNode eyeFileLeft = dirLeft.GetFile(filename);
                                            FileNode eyeFileRight = dirRight.GetFile(filename);

                                            using (VMat eyeROI = face.LeftEye.RoiCropByPercent(frame))
                                                Core.Cv.ImgWrite(eyeFileLeft, eyeROI, 92);

                                            using (VMat eyeROI = face.RightEye.RoiCropByPercent(frame))
                                                Core.Cv.ImgWrite(eyeFileRight, eyeROI, 92);
                                        }
                                    }
                                }
                            }
                            catch(Exception ex)
                            {
                                Logger.Error(ex.ToString());
                            }
                        }
                    }
                    Logger.Log($"Extracted [{count}/{model.Elements.Count}]");
                });

                foreach(var d in detectors)
                    d.Dispose();
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

            Vision.Size oldSize = model.ScreenPixelSize;
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

            model.ScreenPixelSize = newSize;

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
