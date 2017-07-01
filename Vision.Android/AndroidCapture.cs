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
                List<Hardware.Camera.Size> supportSize = parameter.SupportedPreviewSizes.ToList();
                foreach (Hardware.Camera.Size size in supportSize)
                {
                    Logger.Log(this, string.Format("Camera Support Size: W{0},H{1}", size.Width, size.Height));

                    if (size.Width == 1280 && size.Height == 720)
                    {
                        parameter.SetPreviewSize(size.Width, size.Height);
                        Logger.Log(this, string.Format("SET Camera Size: W{0},H{1}", size.Width, size.Height));
                    }
                }
                width = parameter.PreviewSize.Width;
                height = parameter.PreviewSize.Height;
                fps = parameter.PreviewFrameRate;
                cameraType = parameter.PreviewFormat;

                string[] supportedFocusMode = parameter.SupportedFocusModes.ToArray();
                if (supportedFocusMode.Contains(Hardware.Camera.Parameters.FocusModeContinuousVideo))
                {
                    parameter.FocusMode = Hardware.Camera.Parameters.FocusModeContinuousVideo;
                }
                else if (supportedFocusMode.Contains(Hardware.Camera.Parameters.FocusModeContinuousPicture))
                {
                    parameter.FocusMode = Hardware.Camera.Parameters.FocusModeContinuousPicture;
                }
                //parameter.PreviewFormat = Graphics.ImageFormatType.

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
            //if (e.Buffer != null && LimitedTaskScheduler.QueuedTaskCount < LimitedTaskScheduler.MaxTaskCount)
            //    LimitedTaskScheduler.Factory.StartNew(() => CaptureCvtProc(e.Buffer, frameCount, LimitedTaskScheduler.QueuedTaskCount));
            CaptureCvtProc(e.Buffer, 0, 0);

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
                case Graphics.ImageFormatType.Yuv420888:
                default:
                    throw new NotImplementedException("Unknown Camera Format");
            }
            Profiler.End("CaptureCvt.CvtColor" + threadindex);

            Profiler.Start("CaptureCvt.Tp" + threadindex);
            mat = mat.T();
            Profiler.End("CaptureCvt.Tp" + threadindex);

            Profiler.Start("CaptureCvt.Flip" + threadindex);
            if (cameraIndex == 1)
                OpenCV.Core.Core.Flip(mat, mat, (int)FlipMode.XY);
            else
                OpenCV.Core.Core.Flip(mat, mat, (int)FlipMode.Y);
            Profiler.End("CaptureCvt.Flip" + threadindex);

            Profiler.End("CaptureCvt" + threadindex);
            capturedBuffer = mat;
            FrameReady?.Invoke(this, new FrameArgs(new AndroidMat(capturedBuffer)));
            return;
            if (lastFrame > frameIndex)
            {
                if (mat != null)
                    mat.Dispose();
                mat = null;

                return;
            }

            lock (capturedBufferLocker)
            {
                lastFrame = frameIndex;
                
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