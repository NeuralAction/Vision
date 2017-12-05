using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vision;
using Vision.Cv;
using Vision.Detection;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using OpenCvSharp;
using OpenCvSharp.Native;

namespace Vision.Tests
{
    public class FaceDetectionTests : IDisposable
    {
        public class FaceDetectedArgs : EventArgs
        {
            public Mat Frame { get; set; }
            public FaceRect[] Results { get; set; }

            public FaceDetectedArgs(Mat frame, FaceRect[] result)
            {
                Frame = frame;
                Results = result;
            }
        }

        public FaceDetectionProvider FaceProvider { get; set; }
        private FaceDetector FaceDetector { get; set; }
        private OpenFaceDetector OpenFaceDetector { get; set; }
        
        public EyeGazeDetector GazeDetector { get; set; }
        public EyeOpenDetector OpenDetector { get; set; }

        ScreenProperties _screen;
        public ScreenProperties ScreenProperties
        {
            get => _screen;
            set
            {
                _screen = value;
                if(GazeDetector != null)
                {
                    GazeDetector.ScreenProperties = _screen;
                }
            }
        }
        public bool DrawOn { get; set; } = true;
        public bool DetectGaze { get; set; } = false;
        public bool GazeSmooth { get; set; } = false;

        public bool LandmarkDetect
        {
            get
            {
                if (FaceProvider is FaceDetector)
                {
                    return ((FaceDetector)FaceProvider).LandmarkDetect;
                }
                else if (FaceProvider is OpenFaceDetector)
                {
                    return true;
                }
                else
                {
                    throw new Exception();
                }
            }
            set
            {
                if (FaceProvider is FaceDetector)
                {
                    ((FaceDetector)FaceProvider).LandmarkDetect = value;
                }
                else if (FaceProvider is OpenFaceDetector)
                {
                    return;
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public bool SmoothLandmarks
        {
            get
            {
                if (FaceProvider is FaceDetector)
                {
                    return ((FaceDetector)FaceProvider).SmoothLandmarks;
                }
                else if (FaceProvider is OpenFaceDetector)
                {
                    return ((OpenFaceDetector)FaceProvider).UseSmooth;
                }
                else
                {
                    throw new Exception();
                }
            }
            set
            {
                if (FaceProvider is FaceDetector)
                {
                    ((FaceDetector)FaceProvider).SmoothLandmarks = value;
                }
                else if (FaceProvider is OpenFaceDetector)
                {
                    ((OpenFaceDetector)FaceProvider).UseSmooth = value;
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public event EventHandler<FaceDetectedArgs> Detected;

        string FilePath;
        int index = -1;
        Capture capture;

        bool fullscreen = false;
        object renderLock = new object();
        double yoffset = 0;
        int frameMax = 0;
        int frameOk = 0;
        Task FaceDetectionTask;
        Task GazeDetectionTask;
        FaceRect[] rect = null;
        Queue<Point> trail = new Queue<Point>();
        PointKalmanFilter filter = new PointKalmanFilter();
        CalibratingArgs calibratingArgs;

        List<UIObject> UiList = new List<UIObject>();
        Point3DLinePlot facePosePlot;
        Point3DLinePlot faceVecPlot;

        private FaceDetectionTests(string faceXml, string eyeXml, FileNode flandmarkModel)
        {
            Logger.Log(this, "Press E to Exit");

            facePosePlot = new Point3DLinePlot
            {
                Point = new Point(10, 135)
            };

            faceVecPlot = new Point3DLinePlot
            {
                Point = new Point(270, 135)
            };

            UiList.Add(faceVecPlot);
            UiList.Add(facePosePlot);

            UpdateGraph(1500,5000);

            ScreenProperties = new ScreenProperties()
            {
                Origin = new Point3D(-205, 0, 0),
                PixelSize = new Size(1920, 1080),
                Size = new Size(410, 285)
            };
            OpenFaceDetector = new OpenFaceDetector()
            {
                UseSmooth = true
            };
            FaceDetector = new FaceDetector(faceXml, eyeXml, flandmarkModel);
            FaceProvider = OpenFaceDetector;

            GazeDetector = new EyeGazeDetector(ScreenProperties);
            GazeDetector.Calibrator.Calibarting += GazeCalibrater_Calibarting;
            GazeDetector.Calibrator.CalibrateBegin += GazeCalibrater_CalibrateBegin;
            GazeDetector.Calibrator.Calibrated += GazeCalibrater_Calibrated;
            OpenDetector = new EyeOpenDetector();
        }

        public FaceDetectionTests(string filePath, string faceXml, string eyeXml, FileNode flandmarkModel) : this(faceXml, eyeXml, flandmarkModel)
        {
            FilePath = filePath;
            capture = Capture.New(FilePath);
            capture.FrameReady += Capture_FrameReady;
        }

        public FaceDetectionTests(int index, string faceXml, string eyeXml, FileNode flandmarkModel) : this(faceXml, eyeXml, flandmarkModel)
        {
            this.index = index;
            capture = Capture.New(index);
            capture.FrameReady += Capture_FrameReady;
        }

        public FaceDetectionTests(int index, FaceDetectorXmlLoader loader, FlandmarkModelLoader floader) : this(index, loader.FaceXmlPath, loader.EyeXmlPath, floader.Data)
        {

        }

        public FaceDetectionTests(string filepath, FaceDetectorXmlLoader loader, FlandmarkModelLoader floader) : this(filepath, loader.FaceXmlPath, loader.EyeXmlPath, floader.Data)
        {

        }

        public void Start()
        {
            capture.Start();
        }

        public void Stop()
        {
            capture.Stop();
        }

        public void Run()
        {
            capture.Start();
            capture.Wait();
        }

        private void UpdateGraph(double transX, double transZ)
        {
            facePosePlot.PlotX.Min = -transX;
            facePosePlot.PlotX.Max = transX;
            facePosePlot.PlotY.Min = -transX;
            facePosePlot.PlotY.Max = transX;
            facePosePlot.PlotZ.Min = 0;
            facePosePlot.PlotZ.Max = transZ;

            faceVecPlot.PlotX.Min = -1;
            faceVecPlot.PlotX.Max = 1;
            faceVecPlot.PlotY.Min = -1;
            faceVecPlot.PlotY.Max = 1;
            faceVecPlot.PlotZ.Min = -2;
            faceVecPlot.PlotZ.Max = 0;
        }

        private void Capture_FrameReady(object sender, FrameArgs e)
        {
            Mat mat = e.Mat;

            if (mat != null && !mat.IsEmpty)
            {
                if (FaceDetectionTask == null || FaceDetectionTask.IsFaulted || FaceDetectionTask.IsCanceled || FaceDetectionTask.IsCompleted)
                {
                    if(FaceDetectionTask != null && FaceDetectionTask.Exception != null)
                    {
                        Logger.Error(this, FaceDetectionTask.Exception);
                    }

                    Profiler.Start("DetectionALL");

                    Profiler.Start("DetectionFaceTaskStart");
                    e.MatDispose = true;
                    Mat cloned = mat.Clone();
                    FaceDetectionTask = Task.Factory.StartNew(() =>
                    {
                        FaceDetectProc(cloned);
                    });
                    Profiler.End("DetectionFaceTaskStart");
                }

                if (DrawOn)
                {
                    Profiler.Start("Draw");
                    Draw(mat);
                    Profiler.End("Draw");

                    Profiler.Start("imshow");
                    if (fullscreen)
                    {
                        var leftTop = ScreenToMat(mat, new Point(0, 0));
                        var rightBot = ScreenToMat(mat, new Point(ScreenProperties.PixelSize.Width - 1, ScreenProperties.PixelSize.Height - 1));
                        using (Mat m = new Mat(mat, new Rect(leftTop, rightBot).ToCvRect()))
                        {
                            Core.Cv.ImgShow("camera", m);
                        }
                    }
                    else
                    {
                        Core.Cv.ImgShow("camera", mat);
                    }
                    Profiler.End("imshow");
                }
            }

            switch (e.LastKey)
            {
                case 'e':
                    Core.Cv.CloseAllWindows();
                    e.Break = true;
                    break;
                case 'd':
                    LandmarkDetect = !LandmarkDetect;
                    break;
                case 's':
                    SmoothLandmarks = !SmoothLandmarks;
                    break;
                case 'g':
                    DetectGaze = !DetectGaze;
                    break;
                case 'h':
                    GazeSmooth = !GazeSmooth;
                    break;
                case 'j':
                    GazeDetector.DetectMode++;
                    GazeDetector.DetectMode = (EyeGazeDetectMode)((int)GazeDetector.DetectMode % Enum.GetNames(typeof(EyeGazeDetectMode)).Length);
                    break;
                case 'k':
                    GazeDetector.UseModification = !GazeDetector.UseModification;
                    break;
                case '1':
                    FaceProvider = FaceDetector;
                    break;
                case '2':
                    FaceProvider = OpenFaceDetector;
                    break;
                case ' ':
                    Core.Cv.WaitKey(0);
                    break;
                case 'r':
                    GazeDetector.SensitiveX = 1;
                    GazeDetector.SensitiveY = 1;
                    GazeDetector.OffsetX = 0;
                    GazeDetector.OffsetY = 0;
                    break;
                case 't':
                    GazeDetector.SensitiveX -= 0.02;
                    break;
                case 'y':
                    GazeDetector.SensitiveX += 0.02;
                    break;
                case 'u':
                    GazeDetector.SensitiveY -= 0.02;
                    break;
                case 'i':
                    GazeDetector.SensitiveY += 0.02;
                    break;
                case 'o':
                    GazeDetector.OffsetX -= 0.02;
                    break;
                case 'p':
                    GazeDetector.OffsetX += 0.02;
                    break;
                case '[':
                    GazeDetector.OffsetY -= 0.02;
                    break;
                case ']':
                    GazeDetector.OffsetY += 0.02;
                    break;
                case 'c':
                    if(!GazeDetector.Calibrator.IsStarted && DetectGaze)
                        GazeDetector.Calibrator.Start(ScreenProperties);
                    break;
                case 'v':
                    GazeDetector.Calibrator.Stop();
                    break;
                case 'b':
                    GazeDetector.Calibrator.Start(ScreenProperties, false);
                    break;
                case '`':
                    fullscreen = !fullscreen;
                    if (fullscreen)
                    {
                        Cv2.DestroyAllWindows();
                        Cv2.NamedWindow("camera", WindowMode.Normal);
                        Cv2.SetWindowProperty("camera", WindowProperty.Fullscreen, 1);
                    }
                    else
                    {
                        Cv2.DestroyAllWindows();
                    }
                    break;
                default:
                    break;
            }
        }

        private void FaceDetectProc(Mat mat)
        {
            Profiler.Count("FaceFPS");

            Profiler.Start("DetectionFace");
            FaceRect[] rect = FaceProvider.Detect(mat);

            Profiler.Start("DetectionGazeTaskStart");
            if(GazeDetectionTask != null)
            {
                Profiler.Start("DetectionGazeTaskStart.Wait");
                GazeDetectionTask.Wait();
                Profiler.End("DetectionGazeTaskStart.Wait");
            }

            Profiler.Start("DetectionGazeTaskStart.Gap");
            GazeDetectionTask = Task.Factory.StartNew(() =>
            {
                Profiler.End("DetectionGazeTaskStart.Gap");
                GazeDetectProc(mat, rect);
            });
            Profiler.End("DetectionGazeTaskStart");

            Profiler.End("DetectionFace");

            FaceDetectionTask = null;
        }

        private void GazeDetectProc(Mat mat, FaceRect[] rect)
        {
            if (rect != null && rect.Length > 0 && DetectGaze)
            {
                Profiler.Start("GazeALL");
                Point info = GazeDetector.Detect(rect[0], mat);
                if (info != null)
                {
                    info.X = Util.Clamp(info.X, 0, ScreenProperties.PixelSize.Width);
                    info.Y = Util.Clamp(info.Y, 0, ScreenProperties.PixelSize.Height);

                    info = ScreenToMat(mat, info);

                    lock (renderLock)
                    {
                        trail.Enqueue(info);
                    }
                }
                Profiler.End("GazeALL");

                Profiler.Start("OpenALL");
                foreach (var face in rect)
                {
                    if (face.LeftEye != null)
                    {
                        OpenDetector.Detect(face.LeftEye, mat);
                        face.Smoother.SmoothLeftEye(face.LeftEye);
                    }

                    if (face.RightEye != null)
                    {
                        OpenDetector.Detect(face.RightEye, mat);
                        face.Smoother.SmoothRightEye(face.RightEye);
                    }
                }
                Profiler.End("OpenALL");
            }

            lock (renderLock)
            {
                this.rect = rect;
            }

            Profiler.End("DetectionALL");

            Detected?.Invoke(this, new FaceDetectedArgs(mat, rect));

            mat.Dispose();
        }

        private void GazeCalibrater_Calibrated(object sender, CalibratedArgs e)
        {
            Logger.Log(this, "Calibrated");

            EyeGazeCalibrationLog logger = new EyeGazeCalibrationLog(e.Data);
            logger.Save();

            using (Mat frame = logger.Plot(ScreenProperties, GazeDetector.Calibrator))
            {
                var savepath = logger.File.AbosolutePath;
                savepath = savepath.Replace(".clb", ".jpg");
                Core.Cv.ImgWrite(savepath, frame);

                while (true)
                {
                    Core.Cv.ImgShow("calib_result", frame);
                    var c = Core.Cv.WaitKey(1);
                    if (c != 255 || e.Token.IsCancellationRequested)
                    {
                        Core.Cv.CloseWindow("calib_result");
                        return;
                    }
                }
            }
        }

        private void GazeCalibrater_CalibrateBegin(object sender, EventArgs e)
        {
            Logger.Log(this, "Calibrate begin");
        }

        private void GazeCalibrater_Calibarting(object sender, CalibratingArgs e)
        {
            calibratingArgs = e;
        }

        private Point ScreenToMat(Mat mat, Point onScreen)
        {
            return LayoutHelper.ResizePoint(onScreen, ScreenProperties.PixelSize, mat.Size().ToSize(), Stretch.Uniform);
        }

        Point preCalibPt;
        public void Draw(Mat mat)
        {
            lock (renderLock)
            {
                Profiler.Count("DrawFPS");

                Core.Cv.DrawLine(mat, new Point(0, mat.Height / 2), new Point(mat.Width, mat.Height / 2), Scalar.BgrBlack);
                Core.Cv.DrawLine(mat, new Point(mat.Width / 2, 0), new Point(mat.Width / 2, mat.Height), Scalar.BgrBlack);

                var pt1 = ScreenToMat(mat, new Point(0, 0));
                var pt4 = ScreenToMat(mat, new Point(ScreenProperties.PixelSize.Width, ScreenProperties.PixelSize.Height));
                Core.Cv.DrawRectangle(mat, new Rect(pt1, pt4), Scalar.BgrMagenta);

                //update face
                if (rect != null && rect.Length > 0)
                {
                    foreach (FaceRect f in rect)
                    {
                        f.Draw(mat, 3, true, true);
                    }
                    FaceRect face = rect[0];

                    if (face != null && face.Landmarks != null && face.LandmarkTransformVector != null)
                    {
                        //Draw rois
                        List<Mat> rois = new List<Mat>();

                        if (MatTool.ROIValid(mat, face))
                        {
                            Mat roiFace = face.ROI(mat);
                            rois.Add(roiFace);
                        }
                        if (face.RightEye != null && MatTool.ROIValid(mat, face.RightEye.Absolute))
                        {
                            Mat roi = face.RightEye.ROI(mat);
                            rois.Add(roi);
                        }
                        if (face.LeftEye != null && MatTool.ROIValid(mat, face.LeftEye.Absolute))
                        {
                            Mat roi = face.LeftEye.ROI(mat);
                            rois.Add(roi);
                        }

                        var roiMargin = 10;
                        Size roiSize = new Size(100);
                        Point roiPt = new Point(mat.Width - roiSize.Width - roiMargin, mat.Height - roiSize.Height - 50);
                        foreach (var item in rois)
                        {
                            Cv2.Resize(item, item, roiSize.ToCvSize(), 0, 0, InterpolationFlags.Cubic);
                            Core.Cv.DrawMatAlpha(mat, item, roiPt);
                            Core.Cv.DrawRectangle(mat, new Rect(roiPt, roiSize), Scalar.BgrWhite);
                            item.Dispose();
                            roiPt.X -= roiSize.Width + roiMargin;
                        }

                        //Slove Unit Test
                        var scrPt = new Point(960, 0);

                        try
                        {
                            var rod = face.SolveLookScreenRodrigues(scrPt, ScreenProperties);
                            var vec = face.SolveLookScreenVector(scrPt, ScreenProperties);
                            var rodPt = face.SolveRayScreenRodrigues(rod, ScreenProperties);
                            var vecPt = face.SolveRayScreenVector(vec, ScreenProperties);
                            faceVecPlot.Step(vec);

                            if (Point.EucludianDistance(rodPt, scrPt) > 0.01)
                            {
                                Logger.Error(this, $"SolveLook/RayScreen Rodrigues Test Fails / target: {scrPt}, result: {rodPt}");
                            }
                            if (Point.EucludianDistance(vecPt, scrPt) > 0.01)
                            {
                                Logger.Error(this, $"SolveLook/RayScreen Vector Test Fails / target: {scrPt}, result: {vecPt}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(this, "SloveLook/RayScreen Test Fails\n" + ex);
                        }

                        facePosePlot.Step(new Point3D(face.LandmarkTransformVector));

                        List<Point3D> rays = new List<Point3D>()
                        {
                            new Point3D(0, 0, -1),
                            new Point3D(0.1, 0, -1),
                            new Point3D(0, 0.1, -1),
                            new Point3D(-0.1, 0, -1),
                            new Point3D(0, -0.1, -1),
                            new Point3D(0.05, 0, -1),
                            new Point3D(0, 0.05, -1),
                            new Point3D(-0.05, 0, -1),
                            new Point3D(0, -0.05, -1),
                        };

                        foreach (var ray in rays)
                        {
                            var tempPt = face.SolveRayScreenVector(ray, ScreenProperties);
                            tempPt.X = Util.Clamp(tempPt.X, 0, ScreenProperties.PixelSize.Width);
                            tempPt.Y = Util.Clamp(tempPt.Y, 0, ScreenProperties.PixelSize.Height);
                            tempPt = ScreenToMat(mat, tempPt);
                            Core.Cv.DrawCircle(mat, tempPt, 4, Scalar.BgrCyan, -1);
                        }
                    }

                    // calibrating draw
                    if (GazeDetector.Calibrator.IsStarted && calibratingArgs != null)
                    {
                        var calibPt = ScreenToMat(mat, calibratingArgs.Data);
                        Scalar color;
                        switch (calibratingArgs.State)
                        {
                            case CalibratingState.Point:
                                if (preCalibPt == null)
                                {
                                    preCalibPt = ScreenToMat(mat, new Point(ScreenProperties.PixelSize.Width / 2, ScreenProperties.PixelSize.Height / 2));
                                }
                                calibPt = preCalibPt = preCalibPt + (calibPt - preCalibPt) / 3;
                                color = Scalar.BgrGreen;
                                break;
                            case CalibratingState.Wait:
                                preCalibPt = calibPt;
                                color = Scalar.BgrYellow;
                                break;
                            case CalibratingState.SampleWait:
                                color = Scalar.BgrOrange;
                                break;
                            case CalibratingState.Sample:
                                color = Scalar.BgrRed;
                                break;
                            default:
                                color = null;
                                Logger.Throw("unknow state");
                                break;
                        }
                        mat.DrawCircle(calibPt, 15, color, -1, LineTypes.AntiAlias);
                        Core.Cv.DrawText(mat, $"{(calibratingArgs.Percent * 100).ToString("0.00")}%", 
                            new Point(calibPt.X + 20, calibPt.Y + Core.Cv.GetFontSize(HersheyFonts.HersheyPlain) / 2), HersheyFonts.HersheyPlain, 1, color, 2);
                    }

                    if (frameMax > 300)
                        frameMax = frameOk = 0;
                    if (rect.Length > 0 && rect[0].Children.Count > 0)
                        frameOk++;
                }

                UpdateGraph(100 * FaceProvider.UnitPerMM, 600 * FaceProvider.UnitPerMM);

                foreach (var item in UiList)
                {
                    item.Draw(mat);
                }

                //update gaze trail
                if (trail.Count > 20)
                    trail.Dequeue();
                double size = 1;
                foreach (Point pt in trail)
                {
                    if (size == trail.Count - 1)
                    {
                        Core.Cv.DrawCircle(mat, new Point(pt.X, pt.Y), 2, Scalar.BgrCyan, 4);
                    }
                    Core.Cv.DrawCircle(mat, new Point(pt.X, pt.Y), size, Scalar.BgrYellow, 2);
                    size++;
                }

                //update hello wrold
                frameMax++;
                yoffset += 0.02;
                yoffset %= 1;

                //draw texts
                double detectionTime;
                var detectFps = Profiler.Get("DetectionALL");
                if (double.IsInfinity(detectFps) || detectFps == 0)
                    detectionTime = 10000000000;
                else
                    detectionTime = detectFps;
                string demo = $"DetectFPS: {Profiler.Get("FaceFPS")} ({detectionTime.ToString("0.00")}ms/{(1000 / detectionTime).ToString("0.00")}fps)\n" +
                    $"Frame: {frameOk}/{frameMax} ({((double)frameOk / frameMax * 100).ToString("0.00")}%)\n" +
                    $"LndSmt: {SmoothLandmarks} GzSmt: {GazeSmooth} GzMode: {GazeDetector.DetectMode}\n" +
                    $"GzMod: Sx:{GazeDetector.SensitiveX} Sy:{GazeDetector.SensitiveY} Ox:{GazeDetector.OffsetX} Oy:{GazeDetector.OffsetY}";
                mat.DrawText(50, 50, demo, Scalar.BgrGreen);
                mat.DrawText(50, 400 + 250 * Math.Pow(Math.Sin(2 * Math.PI * yoffset), 3), "HELLO WORLD");
                mat.DrawText(50, mat.Height - 50, $"DrawFPS: {Profiler.Get("DrawFPS")}", Scalar.BgrGreen);
            }
        }

        public void Dispose()
        {
            if(capture != null)
            {
                capture.Dispose();
                capture = null;
            }

            if(FaceProvider != null)
            {
                FaceProvider.Dispose();
                FaceProvider = null;
            }
        }
    }
}
