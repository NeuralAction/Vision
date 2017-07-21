using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vision.Cv;

namespace Vision.Windows
{
    public class WindowsCapture : Capture
    {
        public VideoCapture InnerCapture;

        public override event EventHandler<FrameArgs> FrameReady;

        public override object Object
        {
            get { return InnerCapture; }
            set { throw new NotImplementedException(); }
        }
        public override double FPS
        {
            get
            {
                double fps = InnerCapture.Get(CaptureProperty.Fps);
                if(fps == 0)
                {
                    return 24;
                }
                return fps;
            }
            set { InnerCapture.Set(CaptureProperty.Fps, value); }
        }

        private Thread captureThread;
        private Stopwatch sw;
        private bool flip = false;
        private Cv.FlipMode flipMode;

        private WindowsCapture()
        {
            sw = new Stopwatch();
            sw.Start();
        }

        public WindowsCapture(int index) : this()
        {
            InnerCapture = new VideoCapture(index);

            InnerCapture.Set(CaptureProperty.FourCC, (double)FourCC.MJPG);
            InnerCapture.Set(CaptureProperty.Fps, 30);

            InnerCapture.Set(CaptureProperty.FrameWidth, 2048);
            InnerCapture.Set(CaptureProperty.FrameHeight, 2048);

            double w = InnerCapture.Get(CaptureProperty.FrameWidth);
            double h = InnerCapture.Get(CaptureProperty.FrameHeight);

            Logger.Log($"Capture Size: (w:{w},h:{h})  CaptureFormat:{InnerCapture.Get(CaptureProperty.FourCC)}");

            flip = true;
            flipMode = Cv.FlipMode.Y;
        }

        public WindowsCapture(string filepath) : this()
        {
            InnerCapture = new VideoCapture(filepath);
        }

        public override bool CanQuery()
        {
            if (InnerCapture == null)
                return false;

            return InnerCapture.IsOpened();
        }

        public override void Dispose()
        {
            if (InnerCapture != null)
            {
                InnerCapture.Dispose();
                InnerCapture = null;
            }

            if(sw != null)
            {
                sw.Stop();
                sw = null;
            }
        }

        public override VMat QueryFrame()
        {
            if(InnerCapture == null)
            {
                return null;
            }

            Mat frame = new Mat();
            if (CaptureRead(frame))
            {
                return new WindowsMat(frame);
            }
            else
            {
                if (frame != null)
                    frame.Dispose();
                return null;
            }
        }

        private bool CaptureRead(Mat mat)
        {
            bool result = InnerCapture.Read(mat);

            if (mat != null && !mat.Empty() && result && flip)
                Cv2.Flip(mat, mat, (OpenCvSharp.FlipMode)flipMode);

            return result;
        }

        protected override bool Opened()
        {
            if (InnerCapture == null)
                return false;

            return InnerCapture.IsOpened();
        }

        protected override void OnStart()
        {
            Stop();

            captureThread = new Thread(new ThreadStart(CaptureProc));
            captureThread.IsBackground = true;
            captureThread.Start();
        }

        protected override void OnStop()
        {
            if (captureThread != null)
            {
                captureThread.Abort();
                captureThread = null;
            }
        }

        public override void Join()
        {
            if (captureThread != null)
            {
                captureThread.Join();
            }
        }

        private void CaptureProc()
        {
            double fps = Math.Max(1, FPS);
            long lastMs = sw.ElapsedMilliseconds;
            char lastkey = (char)0;
            while (true)
            {
                using (Mat frame = new Mat())
                {
                    if (CaptureRead(frame))
                    {
                        FrameArgs arg = new FrameArgs(new WindowsMat(frame), lastkey);
                        FrameReady?.Invoke(this, arg);
                        if (arg.Break)
                        {
                            Dispose();
                            Stop();
                            return;
                        }

                        int sleep = (int)Math.Round(Math.Max(1, Math.Min(1000, (1000 / fps) - sw.ElapsedMilliseconds + lastMs)));
                        lastMs = sw.ElapsedMilliseconds;
                        lastkey = Core.Cv.WaitKey(sleep);
                    }
                    else
                    {
                        lastkey = Core.Cv.WaitKey(1);
                    }
                }
            }
        }
    }
}
