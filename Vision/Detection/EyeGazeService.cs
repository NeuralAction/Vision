using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;

namespace Vision.Detection
{
    public class EyeGazeService : IDisposable
    {
        public event EventHandler<Point> GazeTracked;
        public event EventHandler<FaceRect[]> FaceTracked;
        public event EventHandler<FrameArgs> FrameCaptured;

        public EyeGazeDetector GazeDetector { get; set; }
        public FaceDetector FaceDetector { get; set; }
        public ScreenProperties ScreenProperties { get; set; }

        Capture Capture;
        Task FaceTask;
        Task GazeTask;

        public EyeGazeService(FaceDetectorXmlLoader loader, ScreenProperties screen)
        {
            ScreenProperties = screen;

            GazeDetector = new EyeGazeDetector(ScreenProperties)
            {
                UseModification = true
            };

            FaceDetector = new FaceDetector(loader)
            {
                EyesDetectCascade = false,
                EyesDetectLandmark = true,
                EyesMaxFactor = 0.8,
                EyesMinFactor = 0.2,
                EyesScaleFactor = 1.2,
                FaceMaxFactor = 1,
                FaceMinFactor = 0.15,
                FaceScaleFactor = 1.2,
                Interpolation = Interpolation.Cubic,
                LandmarkDetect = true,
                LandmarkSolve = true,
                MaxFaceSize = 320,
                MaxSize = 320,
                SmoothLandmarks = true,
                SmoothVectors = true
            };
        }

        public EyeGazeService(Size pixelSize, double dpi) : this(new FaceDetectorXmlLoader(), ScreenProperties.CreatePixelScreen(pixelSize, dpi))
        {

        }

        public EyeGazeService() : this(new FaceDetectorXmlLoader(), ScreenProperties.CreatePixelScreen(new Size(1920,1080)))
        {

        }

        public void Start(int index)
        {
            Start(Capture.New(index));
        }

        public void Start(string filename)
        {
            Start(Capture.New(filename));
        }

        public void Start(Capture capture)
        {
            Capture = capture;
            capture.FrameReady += Capture_FrameReady;
            capture.Start();
        }

        private void Capture_FrameReady(object sender, FrameArgs e)
        {
            var faceRect = FaceDetector.Detect(e.VMat);

            if(e.VMat != null && !e.VMat.IsEmpty)
            {
                var cloned = e.VMat.Clone();
                StartFace(cloned);
            }

            FrameCaptured?.Invoke(this, e);
        }
        
        private void StartFace(VMat mat)
        {
            if(FaceTask != null)
            {
                FaceTask.Wait();
            }

            FaceTask = new Task(() =>
            {
                var result = FaceDetector.Detect(mat);

                StartGaze(result, mat);
                FaceTracked?.Invoke(this, result);
            });

            FaceTask.Start();
        }

        private void StartGaze(FaceRect[] face, VMat frame)
        {
            if(GazeTask != null)
            {
                GazeTask.Wait();
            }

            GazeTask = new Task(() =>
            {
                if(face != null && face.Length > 0 && face[0].LeftEye != null)
                {
                    var targetEye = face[0].LeftEye;
                    var result = GazeDetector.Detect(targetEye, frame);

                    GazeTracked?.Invoke(this, result);
                }

                frame.Dispose();
            });

            GazeTask.Start();
        }

        public void Stop()
        {
            if(Capture != null)
            {
                Capture.Stop();
                Capture.Dispose();
                Capture = null;
            }
        }

        public void Dispose()
        {
            if (Capture != null)
            {
                Capture.Stop();
                Capture.Dispose();
                Capture = null;
            }

            if (FaceTask != null)
            {
                FaceTask.Wait();
                FaceTask = null;
            }

            if (GazeTask != null)
            {
                GazeTask.Wait();
                GazeTask = null;
            }

            if (GazeDetector != null)
            {
                GazeDetector.Dispose();
                GazeDetector = null;
            }

            if(FaceDetector != null)
            {
                FaceDetector.Dispose();
                FaceDetector = null;
            }
        }
    }
}
