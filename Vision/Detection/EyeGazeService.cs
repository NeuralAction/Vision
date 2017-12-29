using OpenCvSharp;
using OpenCvSharp.Native;
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

    public enum ClickEyeTarget
    {
        LeftEye,
        RightEye,
        Both,
        All,
    }

    public class EyeBlinkArgs : EventArgs
    {
        public Point Point { get; set; }
        public ClickEyes ClickEyes { get; set; }

        public EyeBlinkArgs(Point pt, ClickEyes eye)
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

        public event EventHandler<EyeBlinkArgs> Blinked;
        public event EventHandler<EyeBlinkArgs> Blinking;
        public event EventHandler<EyeBlinkArgs> UnBlinked;

        public event EventHandler<Point> Clicked;
        public event EventHandler<Point> Clicking;
        public event EventHandler<Point> Released;

        public ClickEyeTarget ClickTraget { get; set; } = ClickEyeTarget.All;

        public EyeGazeDetector GazeDetector { get; set; }
        public EyeOpenDetector OpenDetector { get; set; }
        public FaceDetectionProvider FaceDetector { get; set; }
        public ScreenProperties ScreenProperties
        {
            get => GazeDetector.ScreenProperties;
            set => GazeDetector.ScreenProperties = value;
        }
        public bool SmoothOpen { get; set; } = true;
        public bool IsLeftClicking { get; protected set; } = false;
        public bool IsRightClicking { get; protected set; } = false;

        public int CaptureIndex => Capture.Index;

        Capture Capture;
        Task FaceTask;
        Task GazeTask;

        public EyeGazeService(OpenFaceModelLoader loader, ScreenProperties screen)
        {
            GazeDetector = new EyeGazeDetector(screen);

            FaceDetector = new OpenFaceDetector()
            {
                Interpolation = InterpolationFlags.Cubic,
                MaxSize = 320,
                UseSmooth = true,
            };

            OpenDetector = new EyeOpenDetector();
        }

        public EyeGazeService(ScreenProperties screen): this(OpenFaceModelLoader.Default, screen)
        {

        }

        public EyeGazeService(Size pixelSize, double dpi) : this(ScreenProperties.CreatePixelScreen(pixelSize, dpi))
        {

        }

        public EyeGazeService() : this(OpenFaceModelLoader.Default, ScreenProperties.CreatePixelScreen(new Size(1920,1080)))
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
            if(e.Mat != null && !e.Mat.IsEmpty)
            {
                StartFace(e.Mat.Clone());
            }

            FrameCaptured?.Invoke(this, e);
        }
        
        private void TaskWait(Task t)
        {
            if (t != null)
            {
                if(!t.IsCanceled || !t.IsCompleted || !t.IsFaulted)
                {
                    try
                    {
                        t.Wait();
                    }
                    catch(AggregateException ex)
                    {
                        Logger.Error(this, ex);
                    }
                }

                if(t.Exception != null)
                {
                    Logger.Throw(this, t.Exception);
                }
            }
        }

        private void StartFace(Mat mat)
        {
            TaskWait(FaceTask);

            FaceTask = Task.Factory.StartNew(() =>
            {
                var result = FaceDetector.Detect(mat);
                if (result != null && result.Length < 1)
                    result = null;

                StartGaze(result, mat);
            });
        }

        private void StartGaze(FaceRect[] face, Mat frame)
        {
            TaskWait(GazeTask);

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
                        OpenDetector.Detect(target.LeftEye, frame);
                        if(SmoothOpen)
                            target.Smoother.SmoothLeftEye(target.LeftEye);
                        var data = target.LeftEye.OpenData;
                        leftClicked = !data.IsOpen;
                    }

                    if(target.RightEye != null)
                    {
                        OpenDetector.Detect(target.RightEye, frame);
                        if(SmoothOpen)
                            target.Smoother.SmoothRightEye(target.RightEye);
                        var data = target.RightEye.OpenData;
                        rightClicked = !data.IsOpen;
                    }

                    if(target.LeftEye != null && target.RightEye != null)
                        result = GazeDetector.Detect(face[0], frame);
                }

                IsLeftClicking = leftClicked;
                IsRightClicking = rightClicked;

                FaceTracked?.Invoke(this, face);
                GazeTracked?.Invoke(this, result);

                if(preLeftClick != leftClicked && preRightClicking != rightClicked)
                {
                    if (leftClicked && rightClicked)
                        Blinked?.Invoke(this, new EyeBlinkArgs(result, ClickEyes.Both));
                    else if (!leftClicked && !rightClicked)
                        UnBlinked?.Invoke(this, new EyeBlinkArgs(result, ClickEyes.Both));
                }
                
                if(preLeftClick != leftClicked)
                {
                    if (leftClicked)
                        Blinked?.Invoke(this, new EyeBlinkArgs(result, ClickEyes.LeftEye));
                    else
                        UnBlinked?.Invoke(this, new EyeBlinkArgs(result, ClickEyes.LeftEye));
                }

                if (preRightClicking != rightClicked)
                {
                    if (rightClicked)
                        Blinked?.Invoke(this, new EyeBlinkArgs(result, ClickEyes.RightEye));
                    else
                        Blinked?.Invoke(this, new EyeBlinkArgs(result, ClickEyes.RightEye));
                }

                bool preClicking;
                bool newClicking;
                switch (ClickTraget)
                {
                    case ClickEyeTarget.LeftEye:
                        preClicking = preLeftClick && preRightClicking == false;
                        newClicking = leftClicked && rightClicked == false;
                        break;
                    case ClickEyeTarget.RightEye:
                        preClicking = preRightClicking && preLeftClick == false;
                        newClicking = rightClicked && leftClicked == false;
                        break;
                    case ClickEyeTarget.Both:
                        preClicking = preLeftClick && preRightClicking;
                        newClicking = leftClicked && rightClicked;
                        break;
                    case ClickEyeTarget.All:
                        preClicking = preLeftClick || preRightClicking;
                        newClicking = leftClicked || rightClicked;
                        break;
                    default:
                        throw new NotImplementedException();
                }

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

                    Blinking?.Invoke(this, new EyeBlinkArgs(result, clickEye));
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
                FaceTask.Wait(1000);
                FaceTask = null;
            }

            if (GazeTask != null)
            {
                GazeTask.Wait(1000);
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
