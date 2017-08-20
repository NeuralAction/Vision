using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;

namespace Vision.Detection
{
    public enum ClickEyes
    {
        LeftEye = 0,
        RightEye = 1,
        Both = 2
    }

    public class EyeWinkArgs : EventArgs
    {
        public Point Point { get; set; }
        public ClickEyes ClickEyes { get; set; }

        public EyeWinkArgs(Point pt, ClickEyes eye)
        {
            Point = pt;
            ClickEyes = eye;
        }
    }

    public class EyeGazeService : IDisposable
    {
        public event EventHandler<Point> GazeTracked;
        public event EventHandler<FaceRect[]> FaceTracked;
        public event EventHandler<FrameArgs> FrameCaptured;

        public event EventHandler<EyeWinkArgs> Winked;
        public event EventHandler<EyeWinkArgs> Winking;
        public event EventHandler<EyeWinkArgs> UnWinked;

        public event EventHandler<Point> Clicked;
        public event EventHandler<Point> Clicking;
        public event EventHandler<Point> Released;

        public EyeGazeDetector GazeDetector { get; set; }
        public EyeOpenDetector OpenDetector { get; set; }
        public FaceDetector FaceDetector { get; set; }
        public ScreenProperties ScreenProperties { get; set; }
        public bool IsLeftClicking { get; set; } = false;
        public bool IsRightClicking { get; set; } = false;

        Capture Capture;
        Task FaceTask;
        Task GazeTask;

        public EyeGazeService(FaceDetectorXmlLoader loader, ScreenProperties screen)
        {
            ScreenProperties = screen;

            GazeDetector = new EyeGazeDetector(ScreenProperties);

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

            OpenDetector = new EyeOpenDetector();
        }

        public EyeGazeService(ScreenProperties screen): this(new FaceDetectorXmlLoader(), screen)
        {

        }

        public EyeGazeService(Size pixelSize, double dpi) : this(ScreenProperties.CreatePixelScreen(pixelSize, dpi))
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
                e.VMatDispose = false;
                StartFace(e.VMat);
            }

            FrameCaptured?.Invoke(this, e);
        }
        
        private void StartFace(VMat mat)
        {
            if(FaceTask != null)
            {
                FaceTask.Wait();
            }

            FaceTask = Task.Factory.StartNew(() =>
            {
                var result = FaceDetector.Detect(mat);
                if (result != null && result.Length < 1)
                    result = null;

                StartGaze(result, mat);
                FaceTracked?.Invoke(this, result);
            });
        }

        private void StartGaze(FaceRect[] face, VMat frame)
        {
            if(GazeTask != null)
            {
                GazeTask.Wait();
            }

            GazeTask = Task.Factory.StartNew(() =>
            {
                Point result = null;
                bool preLeftClick = IsLeftClicking, preRightClicking = IsRightClicking;
                bool leftClicked = false, rightClicked = false;
                if(face != null && face.Length > 0)
                {
                    var target = face[0];

                    if(target.LeftEye != null)
                    {
                        var data = OpenDetector.Detect(target.LeftEye, frame);
                        if(data.Percent > 0.6)
                            leftClicked = !data.IsOpen;
                    }

                    if(target.RightEye != null)
                    {
                        var data = OpenDetector.Detect(target.RightEye, frame);
                        if (data.Percent > 0.6)
                            rightClicked = !data.IsOpen;
                    }

                    if(target.LeftEye != null && target.RightEye != null)
                        result = GazeDetector.Detect(face[0], frame);
                }

                IsLeftClicking = leftClicked;
                IsRightClicking = rightClicked;

                GazeTracked?.Invoke(this, result);

                if(preLeftClick != leftClicked && preRightClicking != rightClicked)
                {
                    if (leftClicked && rightClicked)
                        Winked?.Invoke(this, new EyeWinkArgs(result, ClickEyes.Both));
                    else if (!leftClicked && !rightClicked)
                        UnWinked?.Invoke(this, new EyeWinkArgs(result, ClickEyes.Both));
                }
                
                if(preLeftClick != leftClicked)
                {
                    if (leftClicked)
                        Winked?.Invoke(this, new EyeWinkArgs(result, ClickEyes.LeftEye));
                    else
                        UnWinked?.Invoke(this, new EyeWinkArgs(result, ClickEyes.LeftEye));
                }

                if (preRightClicking != rightClicked)
                {
                    if (rightClicked)
                        Winked?.Invoke(this, new EyeWinkArgs(result, ClickEyes.RightEye));
                    else
                        Winked?.Invoke(this, new EyeWinkArgs(result, ClickEyes.RightEye));
                }

                bool preClicking = preLeftClick || preRightClicking;
                bool newClicking = leftClicked || rightClicked;

                if(preClicking != newClicking)
                {
                    if (newClicking)
                        Clicked?.Invoke(this, result);
                    else
                        Released?.Invoke(this, result);
                }

                if (newClicking)
                    Clicking?.Invoke(this, result);

                if (IsLeftClicking || IsRightClicking)
                {
                    ClickEyes clickEye = ClickEyes.Both;
                    if (IsLeftClicking)
                        clickEye = ClickEyes.LeftEye;
                    if (IsRightClicking)
                        clickEye = ClickEyes.RightEye;
                    if (IsLeftClicking && IsRightClicking)
                        clickEye = ClickEyes.Both;

                    Winking?.Invoke(this, new EyeWinkArgs(result, clickEye));
                }

                frame.Dispose();
                frame = null;
            });
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

            if(OpenDetector != null)
            {
                OpenDetector.Dispose();
                OpenDetector = null;
            }
        }
    }
}
