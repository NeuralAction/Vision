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

namespace Vision.Tests
{
    public class FaceDetection : IDisposable
    {
        public class FaceDetectedArgs : EventArgs
        {
            public VMat Frame { get; set; }
            public FaceRect[] Results { get; set; }

            public FaceDetectedArgs(VMat frame, FaceRect[] result)
            {
                Frame = frame;
                Results = result;
            }
        }

        public bool DrawOn { get; set; } = true;

        public bool DetectGaze { get; set; } = false;

        public event EventHandler<FaceDetectedArgs> Detected;

        string FilePath;
        int index = -1;
        FaceDetector detector;
        EyeGazeDetector gazeDetector;
        Capture capture;

        double yoffset = 0;
        int frameMax = 0;
        int frameOk = 0;
        object renderLock = new object();
        Task FaceDetectionTask;
        Task GazeDetectionTask;
        FaceRect[] rect = null;
        Queue<Point> trail = new Queue<Point>();
        PointKalmanFilter filter = new PointKalmanFilter();

        private FaceDetection(string faceXml, string eyeXml, FileNode flandmarkModel)
        {
            Logger.Log(this, "Press E to Exit");

            detector = new FaceDetector(faceXml, eyeXml);

            gazeDetector = new EyeGazeDetector();
        }

        public FaceDetection(string filePath, string faceXml, string eyeXml, FileNode flandmarkModel) : this(faceXml, eyeXml, flandmarkModel)
        {
            FilePath = filePath;
            capture = Capture.New(FilePath);
            capture.FrameReady += Capture_FrameReady;
        }

        public FaceDetection(int index, string faceXml, string eyeXml, FileNode flandmarkModel) : this(faceXml, eyeXml, flandmarkModel)
        {
            this.index = index;
            capture = Capture.New(index);
            capture.FrameReady += Capture_FrameReady;
        }

        public FaceDetection(int index, FaceDetectorXmlLoader loader, FlandmarkModelLoader floader) : this(index, loader.FaceXmlPath, loader.EyeXmlPath, floader.Data)
        {

        }

        public FaceDetection(string filepath, FaceDetectorXmlLoader loader, FlandmarkModelLoader floader) : this(filepath, loader.FaceXmlPath, loader.EyeXmlPath, floader.Data)
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
            capture.Join();
        }

        private void Capture_FrameReady(object sender, FrameArgs e)
        {
            VMat mat = e.VMat;

            if (mat != null && !mat.IsEmpty)
            {
                if (FaceDetectionTask == null)
                {
                    Profiler.Start("DetectionALL");

                    Profiler.Start("DetectionFaceTaskStart");
                    VMat cloned = mat.Clone();
                    FaceDetectionTask = new Task(() =>
                    {
                        FaceDetectProc(cloned);
                    });
                    FaceDetectionTask.Start();
                    Profiler.End("DetectionFaceTaskStart");
                }

                if (DrawOn)
                {
                    Profiler.Start("Draw");
                    Draw(mat);
                    Profiler.End("Draw");

                    Profiler.Start("imshow");
                    Core.Cv.ImgShow("camera", mat);
                    Profiler.End("imshow");
                }
            }

            switch (e.LastKey)
            {
                case 'e':
                    Core.Cv.CloseAllWindows();
                    e.Break = true;
                    break;
                default:
                    break;
            }
        }

        public void FaceDetectProc(VMat mat)
        {
            Profiler.Count("FaceFPS");

            Profiler.Start("DetectionFace");
            FaceRect[] rect = detector.Detect(mat);

            Profiler.Start("DetectionGazeTaskStart");
            if(GazeDetectionTask != null)
            {
                GazeDetectionTask.Wait();
            }
            
            GazeDetectionTask = new Task(() => 
            {
                GazeDetectProc(mat, rect);
            });
            GazeDetectionTask.Start();
            Profiler.End("DetectionGazeTaskStart");

            Profiler.End("DetectionFace");

            FaceDetectionTask = null;
        }

        public void GazeDetectProc(VMat mat, FaceRect[] rect)
        {
            if (rect.Length > 0 && DetectGaze)
            {
                Profiler.Start("GazeALL");
                Point info = gazeDetector.Detect(rect[0], mat);
                if (info != null)
                {
                    info = filter.Calculate(info);
                    lock (renderLock)
                    {
                        trail.Enqueue(info);
                    }

                    Logger.Log("FaceDectection.GazeDetected", info.ToString());
                }
                Profiler.End("GazeALL");
            }

            lock (renderLock)
            {
                this.rect = rect;
            }

            Profiler.End("DetectionALL");

            Detected?.Invoke(this, new FaceDetectedArgs(mat, rect));

            mat.Dispose();
        }

        public void Draw(VMat mat)
        {
            lock (renderLock)
            {
                Profiler.Count("DrawFPS");

                //update face
                if (rect != null)
                {
                    foreach (FaceRect f in rect)
                        f.Draw(mat, 3, true, true);

                    if (frameMax > 300)
                        frameMax = frameOk = 0;
                    if (rect.Length > 0 && rect[0].Children.Count > 0)
                        frameOk++;
                }

                //update gaze trail
                if (trail.Count > 20)
                    trail.Dequeue();
                double size = 1;
                foreach (Point pt in trail)
                {
                    if (size == trail.Count - 1)
                    {
                        Core.Cv.DrawCircle(mat, new Point(pt.X * mat.Width, pt.Y * mat.Height), 2, Scalar.BgrCyan, 4);
                    }
                    Core.Cv.DrawCircle(mat, new Point(pt.X * mat.Width, pt.Y * mat.Height), size, Scalar.BgrYellow, 2);
                    size++;
                }

                //update hello wrold
                frameMax++;
                yoffset += 0.02;
                yoffset %= 1;

                //draw texts
                mat.DrawText(50, 400 + 250 * Math.Pow(Math.Sin(2 * Math.PI * yoffset), 3), "HELLO WORLD");
                mat.DrawText(50, 50, $"DetectFPS: {Profiler.Get("FaceFPS")} ({Profiler.Get("DetectionALL").ToString("0.00")}ms)", Scalar.BgrGreen);
                mat.DrawText(50, mat.Height - 50, $"DrawFPS: {Profiler.Get("DrawFPS")}", Scalar.BgrGreen);
                mat.DrawText(50, 85, "Frame: " + frameOk + "/" + frameMax + " (" + ((double)frameOk / frameMax * 100).ToString("0.00") + "%)", Scalar.BgrGreen);
            }
        }

        public void Dispose()
        {
            if(capture != null)
            {
                capture.Dispose();
                capture = null;
            }

            if(detector != null)
            {
                detector.Dispose();
                detector = null;
            }
        }
    }
}
