using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Hardware = Android.Hardware;
using Graphics = Android.Graphics;
using OpenCV.VideoIO;
using OpenCV.Core;
using Vision.Cv;

#pragma warning disable CS0618 // 형식 또는 멤버는 사용되지 않습니다.

namespace Vision.Android
{
    public class AndroidCapture : Capture
    {
        public override object Object
        {
            get
            {
                return Camera;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        private double fps;
        public override double FPS => fps;
        public bool MultiThread { get; set; } = false;

        private Hardware.Camera Camera;
        private int cameraIndex;
        private bool cameraOn = false;
        private Graphics.ImageFormatType cameraType;
        private int width;
        private int height;
        private Graphics.SurfaceTexture Texture;
        private object capturedBufferLocker = new object();
        private Mat capturedBuffer;
        private long frameCount = 0;
        private long lastFrame = -1;

        public override event EventHandler<FrameArgs> FrameReady;

        public AndroidCapture(int index)
        {
            cameraIndex = index;
        }

        public AndroidCapture(string filepath)
        {
            throw new NotImplementedException();
        }

        #region CatpureInterface

        public override bool CanQuery()
        {
            if (Camera != null)
            {
                return true;
            }
            return false;
        }

        public override VMat QueryFrame()
        {
            if (!cameraOn)
                Start();

            lock (capturedBufferLocker)
            {
                if (capturedBuffer != null)
                {
                    VMat ret = new AndroidMat(capturedBuffer);

                    capturedBuffer = null;

                    return ret;
                }
                return null;
            }
        }

        protected override bool Opened()
        {
            if (Camera != null)
            {
                return true;
            }
            return false;
        }

        #endregion CaptureInterface

        #region CaptureProc

        protected override void OnStart()
        {
            try
            {
                if(Camera == null)
                    Camera = Hardware.Camera.Open(cameraIndex);

                if(Texture == null)
                    Texture = new Graphics.SurfaceTexture(0);

                CameraPreviewCallback callback = new CameraPreviewCallback();
                callback.PreviewUpdated += Callback_PreviewUpdated;

                Hardware.Camera.Parameters parameter = Camera.GetParameters();
                List<Hardware.Camera.Size> supportSize = parameter.SupportedPreviewSizes.OrderByDescending(x=>x.Width).ToList();
                foreach (Hardware.Camera.Size size in supportSize)
                {
                    Logger.Log(this, $"Camera Support Size: W{size.Width},H{size.Height}");

                    if (size.Width == 1280 && size.Height == 720)
                    //if(size.Width == size.Height)
                    {
                        parameter.SetPreviewSize(size.Width, size.Height);
                        Logger.Log(this, $"SET Camera Size: W{size.Width},H{size.Height}");
                        break;
                    }
                }

                string[] supportedFocusMode = parameter.SupportedFocusModes.ToArray();
                if (supportedFocusMode.Contains(Hardware.Camera.Parameters.FocusModeContinuousVideo))
                {
                    parameter.FocusMode = Hardware.Camera.Parameters.FocusModeContinuousVideo;
                }
                else if (supportedFocusMode.Contains(Hardware.Camera.Parameters.FocusModeContinuousPicture))
                {
                    parameter.FocusMode = Hardware.Camera.Parameters.FocusModeContinuousPicture;
                }
                parameter.ColorEffect = Hardware.Camera.Parameters.EffectNone;

                width = parameter.PreviewSize.Width;
                height = parameter.PreviewSize.Height;
                fps = parameter.PreviewFrameRate;
                cameraType = parameter.PreviewFormat;

                Logger.Log(this, string.Format("Camera is creating W{0} H{1} FPS{2}", width, height, fps));
                Camera.SetParameters(parameter);

                Camera.SetPreviewCallback(callback);
                Camera.SetPreviewTexture(Texture);
                Camera.StartPreview();

                cameraOn = true;
            }
            catch (Exception ex)
            {
                Logger.Error(this, "Camera Init Failed.\n" + ex.ToString());

                Dispose();

                throw new ArgumentException("Camera Exception", ex);
            }
        }

        protected override void OnStop()
        {
            if (Camera != null)
            {
                Camera.StopPreview();
                Camera.SetPreviewCallback(null);
                Camera.SetPreviewTexture(null);
            }

            cameraOn = false;
        }

        private void Callback_PreviewUpdated(object sender, PreviewUpdatedEventArgs e)
        {
            Profiler.End("Captured");
            Profiler.Start("Captured");

            if (FrameReady == null)
                return;

            frameCount++;
            if (MultiThread)
            {
                if (e.Buffer != null && LimitedTaskScheduler.QueuedTaskCount < LimitedTaskScheduler.MaxTaskCount)
                    LimitedTaskScheduler.Factory.StartNew(() => CaptureCvtProc(e.Buffer, frameCount, LimitedTaskScheduler.QueuedTaskCount));
            }
            else
            {
                CaptureCvtProc(e.Buffer, 0, 0);
            }

            Profiler.Capture("TaskCount", LimitedTaskScheduler.QueuedTaskCount);
        }

        private void CaptureCvtProc(byte[] Buffer, long frameIndex, int threadindex)
        {
            Profiler.Start("CaptureCvt" + threadindex);
            Mat mat = null;

            Profiler.Start("CaptureCvt.CvtColor" + threadindex);
            switch (cameraType)
            {
                case Graphics.ImageFormatType.Nv16:
                    mat = new Mat((int)Math.Round(height * 1.5), width, CvType.Cv8uc1);
                    mat.Put(0, 0, Buffer);
                    OpenCV.ImgProc.Imgproc.CvtColor(mat, mat, (int)ColorConversion.YuvToBGR_NV12);
                    break;
                case Graphics.ImageFormatType.Nv21:
                    mat = new Mat((int)Math.Round(height * 1.5), width, CvType.Cv8uc1);
                    mat.Put(0, 0, Buffer);
                    OpenCV.ImgProc.Imgproc.CvtColor(mat, mat, (int)ColorConversion.YuvToBGR_NV21);
                    break;
                case Graphics.ImageFormatType.Rgb565:
                    mat = new Mat(width, height, CvType.Cv16uc1);
                    mat.Put(0, 0, Buffer);
                    OpenCV.ImgProc.Imgproc.CvtColor(mat, mat, (int)ColorConversion.Bgr565ToBgr);
                    break;
                case Graphics.ImageFormatType.Yuv420888:
                default:
                    throw new NotImplementedException("Unknown Camera Format");
            }
            Profiler.End("CaptureCvt.CvtColor" + threadindex);

            Profiler.Start("CaptureCvt.Tp" + threadindex);
            OpenCV.Core.Core.Transpose(mat, mat);
            Profiler.End("CaptureCvt.Tp" + threadindex);

            Profiler.Start("CaptureCvt.Flip" + threadindex);
            if (cameraIndex == 1)
                OpenCV.Core.Core.Flip(mat, mat, (int)FlipMode.XY);
            else
                OpenCV.Core.Core.Flip(mat, mat, (int)FlipMode.Y);
            Profiler.End("CaptureCvt.Flip" + threadindex);

            Profiler.End("CaptureCvt" + threadindex);
            capturedBuffer = mat;

            var args = new FrameArgs(new AndroidMat(mat));

            if (MultiThread)
            {
                lock (capturedBufferLocker)
                {
                    if (lastFrame > frameIndex)
                    {
                        if (mat != null)
                            mat.Dispose();
                        mat = null;
                        Profiler.Count("CaptureSkipped");
                        return;
                    }

                    lastFrame = frameIndex;
                }
                FrameReady?.Invoke(this, args);
            }
            else
            {
                FrameReady?.Invoke(this, args);
            }

            if (args.VMatDispose)
            {
                mat.Release();
                mat.Dispose();
                mat = null;
            }

            if (args.Break)
            {
                Dispose();
                Stop();
                return;
            }
        }

        #endregion CaptureProc

        public override void Dispose()
        {
            if (Camera != null)
            {
                Stop();
                Camera.Release();
                Camera.Dispose();
                Camera = null;
            }

            if (Texture != null)
            {
                Texture.Release();
                Texture.Dispose();
                Texture = null;
            }
        }
    }
}

#pragma warning restore CS0618 // 형식 또는 멤버는 사용되지 않습니다.