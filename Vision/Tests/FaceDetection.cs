using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vision;

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

        public event EventHandler<FaceDetectedArgs> Detected;

        string FilePath;
        int index = -1;
        EyesDetector detector;
        Capture capture;

        private FaceDetection(string faceXml, string eyeXml)
        {
            Logger.Log(this, "Press E to Exit");

            detector = new EyesDetector(faceXml, eyeXml)
            {
                Interpolation = Interpolation.NearestNeighbor,
                MaxSize = 250,
                MaxFaceSize = 85,
                FaceMaxFactor = 0.9,
            };
        }

        public FaceDetection(string filePath, string faceXml, string eyeXml) : this(faceXml, eyeXml)
        {
            FilePath = filePath;
            capture = Capture.New(FilePath);
            capture.FrameReady += Capture_FrameReady;
        }

        public FaceDetection(int index, string faceXml, string eyeXml) : this(faceXml, eyeXml)
        {
            this.index = index;
            capture = Capture.New(index);
            capture.FrameReady += Capture_FrameReady;
        }

        public FaceDetection(int index, EyesDetectorXmlLoader loader) : this(index, loader.FaceXmlPath, loader.EyeXmlPath)
        {

        }

        public FaceDetection(string filepath, EyesDetectorXmlLoader loader) : this(filepath, loader.FaceXmlPath, loader.EyeXmlPath)
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

        double yoffset = 0;
        int frameMax = 0;
        int frameOk = 0;
        private void Capture_FrameReady(object sender, FrameArgs e)
        {
            VMat mat = e.VMat;

            if (mat != null && !mat.IsEmpty)
            {
                Profiler.Count("FaceFPS");

                Profiler.Start("DetectionALL");
                FaceRect[] rect = detector.Detect(mat);
                Profiler.End("DetectionALL");

                Detected?.Invoke(this, new FaceDetectedArgs(mat, rect));

                if (DrawOn)
                {
                    Profiler.Start("Draw");
                    Draw(mat, rect);
                    Profiler.End("Draw");

                    Profiler.Start("imshow");
                    Core.Cv.ImgShow("camera", mat);
                    Profiler.End("imshow");
                }

                Profiler.End("CapMain");
            }

            switch (e.LastKey)
            {
                case 'e':
                    Core.Cv.CloseAllWindows();
                    capture.Stop();
                    break;
                default:
                    break;
            }
        }

        public void Draw(VMat mat, FaceRect[] rect)
        {
            foreach (FaceRect f in rect)
                f.Draw(mat, 3, true);

            if (frameMax > 300)
                frameMax = frameOk = 0;
            if (rect.Length > 0 && rect[0].Children.Count > 0)
                frameOk++;
            frameMax++;
            yoffset += 0.02;
            yoffset %= 1;
            mat.DrawText(50, 400 + 250 * Math.Pow(Math.Sin(2 * Math.PI * yoffset), 3), "HELLO WORLD");
            mat.DrawText(50, 50, "FPS: " + Profiler.Get("FaceFPS") + " Detect: " + Profiler.Get("DetectionALL").ToString("0.00") + "ms", Scalar.Green);
            mat.DrawText(50, 85, "Frame: " + frameOk + "/" + frameMax + " (" + ((double)frameOk / frameMax * 100).ToString("0.00") + "%)", Scalar.Green);
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
