using Microsoft.Win32;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Vision;
using Vision.Detection;
using Vision.Windows;
using Vision.Cv;
using OpenCvSharp;
using System.Threading;

namespace VideoDetectingWithCalib
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        Vision.Detection.EyeGazeDetector gazeDetector;
        Vision.Detection.FaceDetectionProvider faceDetector;
        Vision.Detection.ScreenProperties properties
        {
            get => gazeDetector.ScreenProperties;
            set => gazeDetector.ScreenProperties = value;
        }
        Queue<Mat> matQueue = new Queue<Mat>();
        double rotateAngle = 0;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                Vision.Windows.WindowsCore.Init(true);
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Exception:{ex.ToString()}", "Error while Startup", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }

            faceDetector = new OpenFaceDetector(new OpenFaceModelLoader());
            faceDetector.UseSmooth = true;

            gazeDetector = new EyeGazeDetector(Core.GetDefaultScreen());
            gazeDetector.ClipToBound = false;
            gazeDetector.Smoother.Method = PointSmoother.SmoothMethod.Kalman;
            gazeDetector.UseCalibrator = gazeDetector.UseSmoothing = true;

            CompositionTarget.Rendering += delegate
            {
                if (matQueue.Count > 0)
                {
                    var img = matQueue.Dequeue();
                    while (matQueue.Count > 0)
                        matQueue.Dequeue().Dispose();
                    Img_Show.Source = img.ToBitmapSource();
                    img.Dispose();
                }
            };

            Closed += delegate { Environment.Exit(0); };
        }
        void SaveFile(string path, string content)
        {
            try
            {
                File.WriteAllText(path, content);
            }
            catch (IOException ex)
            {
                if (MessageBox.Show(ex.Message + "\nRetry?", "Error while saving data!", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    SaveFile(path, content);
            }
        }

        void StartProcessing()
        {
            var ofd = new OpenFileDialog();
            ofd.Title = "Load mp4";
            ofd.Filter = "Video File|*.mp4;*.mpeg;*.avi;*.wmv;*.mov;*.mkv|All Files|*.*";
            ofd.Multiselect = true;

            if ((bool)ofd.ShowDialog())
            {
                foreach (var filename in ofd.FileNames)
                {
                    using (var cap = Core.Cv.CreateCapture(filename))
                    {
                        var builder = new StringBuilder();

                        builder.AppendLine("Filename");
                        builder.AppendLine($"\"{filename}\"");

                        builder.AppendLine("Frame per second");
                        builder.AppendLine(cap.FPS.ToString());

                        var calib = (LinearEyeGazeCalibratorEngine)gazeDetector.Calibrator.Engine;
                        builder.AppendLine("Calibration X, Calibration Y");
                        builder.AppendLine($"{calib.X},{calib.Y}");

                        builder.AppendLine("Screen Size W,H,Pixel Size W,H,Screen Origin X,Y,Z");
                        builder.AppendLine($"{properties.Size.Width},{properties.Size.Height}," +
                            $"{properties.PixelSize.Width},{properties.PixelSize.Height}," +
                            $"{properties.Origin.X},{properties.Origin.Y},{properties.Origin.Z}");

                        builder.AppendLine("Frame Index (0~),Screen Point X,Screen Point Y, Gaze Vector X, Gaze Vector Y, Gaze Vector Z");
                        int index = 0;
                        cap.FrameReady += (o, arg) =>
                        {
                            var frame = arg.Mat;

                            frame.Rotate(rotateAngle);

                            var faces = faceDetector.Detect(frame);
                            if (faces.Length > 0)
                            {
                                var target = faces[0];
                                var gaze = gazeDetector.Detect(target, frame);

                                target.Draw(frame, 1, true, true);

                                if (gaze != null)
                                {
                                    builder.AppendLine($"{index},{target.GazeInfo.ScreenPoint.X},{target.GazeInfo.ScreenPoint.Y}," +
                                        $"{target.GazeInfo.Vector.X},{target.GazeInfo.Vector.Y},{target.GazeInfo.Vector.Z}");
                                    Logger.Log(target.GazeInfo.ScreenPoint);
                                }
                            }

                            matQueue.Enqueue(frame.Clone());
                            index += 1;
                        };

                        cap.CaptureStopped += delegate
                        {
                            var fullText = builder.ToString();
                            SaveFile(filename + ".csv", fullText);
                        };

                        cap.Start();
                        cap.Wait();
                    }
                }
            }
        }

        void LoadCalibration()
        {
            var ofd = new OpenFileDialog();
            ofd.Title = "Load calibration json";
            ofd.Filter = "JSON File (*.json)|*.json";
            if ((bool)ofd.ShowDialog())
            {
                var json = JObject.Parse(File.ReadAllText(ofd.FileName));

                var username = json["User"].ToString();
                var file = json["File"].ToString();
                var fps = json["FPS"].ToObject<double>();
                if (json.ContainsKey("RotateAngle"))
                    rotateAngle = json["RotateAngle"].ToObject<double>();
                else
                    rotateAngle = 0;

                var scrMmX = json["ScrMilliWidth"].ToObject<double>();
                var scrMmY = json["ScrMilliHeight"].ToObject<double>();
                var scrPixelX = json["ScrPixelWidth"].ToObject<double>();
                var scrPixelY = json["ScrPixelHeight"].ToObject<double>();
                var scrOriginX = json["ScrOriginX"].ToObject<double>();
                var scrOriginY = json["ScrOriginY"].ToObject<double>();
                var scrOriginZ = json["ScrOriginZ"].ToObject<double>();
                var scrProperties = new ScreenProperties()
                {
                    Size = new Vision.Size(scrMmX, scrMmY),
                    PixelSize = new Vision.Size(scrPixelX, scrPixelY),
                    Origin = new Point3D(scrOriginX, scrOriginY, scrOriginZ),
                };
                properties = scrProperties;

                var strCalibPoints = json["CalibPoints"];
                var calibPoints = new Queue<(double ms, double x, double y)>();
                foreach (var item in strCalibPoints)
                {
                    calibPoints.Enqueue((
                        item["Millisecond"].ToObject<double>(),
                        item["ScrPixelX"].ToObject<double>(),
                        item["ScrPixelY"].ToObject<double>()));
                }

                var filepath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ofd.FileName), file);
                using (var cap = Core.Cv.CreateCapture(filepath))
                {
                    double elapsed = 0;
                    var dataset = new Dictionary<Point3D, CalibratingPushData>();
                    var frameIndex = 0;
                    cap.FrameReady += (o, e) =>
                    {
                        var w = e.Mat.Width;
                        var h = e.Mat.Height;

                        e.Mat.Rotate(rotateAngle);

                        if (elapsed >= calibPoints.First().ms)
                        {
                            (double ms, double x, double y) point = (-1, 0, 0);
                            var tempi = 0;
                            while (elapsed >= calibPoints.First().ms)
                            {
                                point = calibPoints.Dequeue();
                                if (calibPoints.Count < 1)
                                    break;
                                tempi++;
                            }
                            if (point.ms == -1)
                                throw new Exception();

                            var faces = faceDetector.Detect(e.Mat);
                            if (faces.Length > 0)
                            {
                                var target = faces[0];

                                gazeDetector.Detect(target, e.Mat);

                                var label = target.SolveLookScreenVector(new Vision.Point(point.x, point.y), properties);
                                dataset.Add(label, new CalibratingPushData(target, e.Mat));

                                target.Draw(e.Mat, 3, true, true);

                                //if(point.y > 1400)
                                //{
                                //    Core.Cv.ImgShow($"{frameIndex}|{elapsed}", e.Mat);
                                //    Core.Cv.WaitKey(1);
                                //}
                                
                                matQueue.Enqueue(e.Mat.Clone());
                            }
                        }

                        if (calibPoints.Count < 1)
                            e.Break = true;
                        elapsed += 1000.0 / fps;
                        frameIndex += 1;
                    };

                    gazeDetector.UseCalibrator = false;
                    gazeDetector.UseSmoothing = false;
                    cap.Start();
                    cap.Wait();
                    //gazeDetector.UseSmoothing = true;
                    gazeDetector.UseCalibrator = true;

                    var engine = gazeDetector.Calibrator.Engine;
                    if(dataset.Count < 4)
                    {
                        MessageBox.Show("Too few frames were detected.", "Calibration Failed");
                        return;
                    }
                    engine.SetData(dataset);
                    engine.Train();

                    var log = new EyeGazeCalibrationLog(dataset);
                    var plt = log.Plot(properties, gazeDetector.Calibrator);
                    for (int i = 0; i < 10; i++)
                    {
                        matQueue.Enqueue(plt.Clone());
                    }
                }
            }
        }

        private void Bt_Proc_File_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            Task.Factory.StartNew(() =>
            {
                StartProcessing();
                Dispatcher.Invoke(() => { this.IsEnabled = true; });
            });
        }

        private void Bt_Proc_Load_Calib_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            Task.Factory.StartNew(() =>
            {
                LoadCalibration();
                Dispatcher.Invoke(() => { this.IsEnabled = true; });
            });
        }
    }
}
