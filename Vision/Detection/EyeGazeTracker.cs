using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;

namespace Vision.Detection
{
    public class EyeGazeTracker : IDisposable
    {
        public event EventHandler<Point> GazeTracked;
        public event EventHandler<FaceRect[]> FaceTracked;
        public event EventHandler<FrameArgs> FrameCaptured;

        public EyeGazeDetector GazeDetector { get; set; }
        public FaceDetector FaceDetector { get; set; }
        public ScreenProperties ScreenProperties { get; set; }
        public bool Show { get; set; } = false;

        Capture Capture;

        public EyeGazeTracker(FaceDetectorXmlLoader loader, ScreenProperties screen)
        {
            GazeDetector = new EyeGazeDetector()
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
                Interpolation = Cv.Interpolation.Cubic,
                LandmarkDetect = true,
                LandmarkSolve = true,
                MaxFaceSize = 320,
                MaxSize = 320,
                SmoothLandmarks = true,
                SmoothVectors = true
            };

            ScreenProperties = screen;
        }

        public EyeGazeTracker(Size pixelSize, double dpi) : this(new FaceDetectorXmlLoader(), ScreenProperties.CreatePixelScreen(pixelSize, dpi))
        {

        }

        public EyeGazeTracker() : this(new FaceDetectorXmlLoader(), ScreenProperties.CreatePixelScreen(new Size(1920,1080)))
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

            FrameCaptured?.Invoke(this, e);
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
