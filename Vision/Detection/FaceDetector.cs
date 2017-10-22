using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;

namespace Vision.Detection
{
    public class FaceDetector : IDisposable
    {
        public double FaceScaleFactor { get; set; } = 1.2;
        public double FaceMinFactor { get; set; } = 0.1;
        public double FaceMaxFactor { get; set; } = 1;
        public int MaxSize { get; set; } = 180;
        
        public bool EyesDetectCascade { get; set; } = true;
        public bool EyesDetectLandmark { get; set; } = true;

        public double EyesScaleFactor { get; set; } = 1.2;
        public double EyesMinFactor { get; set; } = 0.2;
        public double EyesMaxFactor { get; set; } = 0.5;
        public int MaxFaceSize { get; set; } = 100;

        public bool LandmarkDetect { get; set; } = true;
        public bool LandmarkSolve { get; set; } = true;

        public bool SmoothLandmarks { get; set; } = false;
        public bool SmoothVectors { get; set; } = false;
        public bool ClampVectors { get; set; } = true;
        
        public InterpolationFlags Interpolation { get; set; } = InterpolationFlags.Nearest;

        CascadeClassifier FaceCascade;
        CascadeClassifier EyesCascade;
        FaceLandmarkDetector Landmark;
        
        List<FaceSmoother> Smoother = new List<FaceSmoother>();

        public FaceDetector(string FaceXml, string EyesXml)
        {
            FaceCascade = new CascadeClassifier(FaceXml);
            EyesCascade = new CascadeClassifier(EyesXml);
            Landmark = new FaceLandmarkDetector();
        }

        public FaceDetector(FaceDetectorXmlLoader Loader) : this(Loader.FaceXmlPath, Loader.EyeXmlPath)
        {

        }

        public FaceDetector() : this(new FaceDetectorXmlLoader())
        {

        }
        
        public FaceRect[] Detect(Mat frame, bool debug = false)
        {
            if (frame.IsEmpty)
                return null;

            using (Mat frame_gray = new Mat())
            {
                Profiler.Start("DetectionPre");

                Profiler.Start("DetectionPre.Cvt");
                frame.ConvertColor(frame_gray, ColorConversionCodes.BGR2GRAY);
                Profiler.End("DetectionPre.Cvt");

                double scaleFactor = frame_gray.CalcScaleFactor(MaxSize);
                Mat frame_face = null;
                if(scaleFactor != 1)
                {
                    frame_face = frame_gray.Clone();
                    frame_gray.ClampSize(MaxSize, Interpolation);
                }
                else
                {
                    frame_face = frame_gray;
                }
                frame_gray.EqualizeHistogram();
                Profiler.End("DetectionPre");

                Profiler.Start("DetectionFace");
                double frameMinSize = Math.Min(frame_gray.Width, frame_gray.Height);
                OpenCvSharp.Rect[] cascadeResult = FaceCascade.DetectMultiScale(frame_gray, FaceScaleFactor, 2, HaarDetectionType.ScaleImage, new OpenCvSharp.Size(frameMinSize * FaceMinFactor), new OpenCvSharp.Size(frameMinSize * FaceMaxFactor));
                var faces = from i in cascadeResult orderby i.X orderby i.Y select i;
                List<FaceRect> FaceList = new List<FaceRect>();
                Profiler.End("DetectionFace");
                
                Profiler.Start("DetectionEyes");
                int index = 0;
                foreach (var face in faces)
                {
                    if (Smoother.Count <= index)
                        Smoother.Add(new FaceSmoother());
                    FaceSmoother smoothCurrent = Smoother[index];

                    FaceRect faceRect = new FaceRect(face.ToRect(), smoothCurrent);
                    faceRect.Scale(1 / scaleFactor);

                    if (debug)
                        faceRect.Draw(frame, drawLandmarks: true);

                    if (LandmarkDetect || EyesDetectLandmark)
                    {
                        Profiler.Start("DetectionLandmark");
                        Landmark.Interpolation = Interpolation;
                        Landmark.Detect(frame_face, faceRect);
                        if (SmoothLandmarks)
                            smoothCurrent.SmoothLandmark(faceRect);

                        Profiler.Start("DetectionLandmarkSolve");
                        if (LandmarkSolve)
                        {
                            Landmark.Solve(frame_face, faceRect);
                            if (ClampVectors)
                                smoothCurrent.ClampVector(faceRect);
                            if (SmoothVectors)
                                smoothCurrent.SmoothVector(faceRect);
                        }
                        Profiler.End("DetectionLandmarkSolve");

                        Profiler.End("DetectionLandmark");
                    }
                    
                    if(EyesDetectCascade)
                    {
                        using (Mat faceROI = new Mat(frame_face, faceRect.ToCvRect()))
                        {
                            double eyeScale = faceROI.ClampSize(MaxFaceSize, Interpolation);
                            double faceMinSize = Math.Min(faceROI.Width, faceROI.Height);
                            Profiler.Start("DetectionEye");
                            var eyes = EyesCascade.DetectMultiScale(faceROI, EyesScaleFactor, 2, HaarDetectionType.ScaleImage, new OpenCvSharp.Size(faceMinSize * EyesMinFactor), new OpenCvSharp.Size(faceMinSize * EyesMaxFactor));

                            foreach (var teye in eyes)
                            {
                                var eye = teye.ToRect();
                                eye.Scale(1 / eyeScale);
                                EyeRect eyeRect = new EyeRect(faceRect, eye);
                                faceRect.Add(eyeRect);

                                if (debug)
                                    eyeRect.Draw(frame);
                            }
                            Profiler.End("DetectionEye");
                        }
                    }

                    if (EyesDetectLandmark)
                    {
                        FaceLandmarkDetector.CalcEyes(faceRect);
                    }

                    FaceList.Add(faceRect);
                    index++;
                }
                Profiler.End("DetectionEyes");
                
                if (frame_face != null)
                {
                    frame_face.Dispose();
                    frame_face = null;
                }
                var rect = FaceList.ToArray();

                return rect;
            }
        }

        public void Dispose()
        {
            if(EyesCascade != null)
            {
                EyesCascade.Dispose();
                EyesCascade = null;
            }

            if(FaceCascade != null)
            {
                FaceCascade.Dispose();
                FaceCascade = null;
            }
        }
    }

    public class FaceSmoother
    {
        public const double TranslateLimit = 5000;

        PointKalmanFilter[] LandmarkFilter;
        ArrayKalmanFilter TranslateFilter;
        bool vectorInverted = false;

        public FaceSmoother()
        {
            LandmarkFilter = new PointKalmanFilter[8];
            for (int i = 0; i < 8; i++)
            {
                LandmarkFilter[i] = new PointKalmanFilter();
            }

            TranslateFilter = new ArrayKalmanFilter(3);
        }

        public void SmoothLandmark(FaceRect face)
        {
            if (face.Landmarks != null)
            {
                for (int i = 0; i < LandmarkFilter.Length; i++)
                {
                    face.Landmarks[i] = LandmarkFilter[i].Calculate(face.Landmarks[i]);
                }
            }
        }

        public void ClampVector(FaceRect face)
        {
            if (face.LandmarkTransformVector != null && face.LandmarkRotationVector != null)
            {
                if (face.LandmarkTransformVector[2] < 0)
                {
                    ArrayMul(face.LandmarkTransformVector, -1);
                }

                double min = face.LandmarkTransformVector.Min();
                double max = face.LandmarkTransformVector.Max();
                double absMax = Math.Max(Math.Abs(min), Math.Abs(max));
                if (absMax > TranslateLimit)
                {
                    ArrayMul(face.LandmarkTransformVector, TranslateLimit / absMax);
                    vectorInverted = true;
                }
            }
        }

        public void SmoothVector(FaceRect face)
        {
            if(face.LandmarkTransformVector != null && face.LandmarkRotationVector != null)
            {
                if (vectorInverted)
                {
                    face.LandmarkTransformVector = TranslateFilter.Clone().Calculate(face.LandmarkTransformVector);
                    vectorInverted = false;
                }
                else
                {
                    face.LandmarkTransformVector = TranslateFilter.Calculate(face.LandmarkTransformVector);
                }
            }
        }

        ArrayKalmanFilter leftKalman = new ArrayKalmanFilter(2);
        ArrayKalmanFilter rightKalman = new ArrayKalmanFilter(2);

        public void SmoothLeftEye(EyeRect rect)
        {
            SmoothEye(rect, leftKalman);
        }

        public void SmoothRightEye(EyeRect rect)
        {
            SmoothEye(rect, rightKalman);
        }

        private void SmoothEye(EyeRect rect, ArrayKalmanFilter kalman)
        {
            var result = kalman.Calculate(new double[] { rect.OpenData.Open, rect.OpenData.Close });
            rect.OpenData.Open = Math.Max(0, Math.Min(1, result[0]));
            rect.OpenData.Close = Math.Max(0, Math.Min(1, result[1]));
        }

        private void ArrayMul(double[] array, double right)
        {
            for(int i=0; i < array.Length; i++)
            {
                array[i] *= right;
            }
        }
    }
}
