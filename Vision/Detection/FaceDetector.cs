using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;
using SharpFace;

namespace Vision.Detection
{
    public abstract class FaceDetectionProvider : IDisposable
    {
        public abstract double UnitPerMM { get; }
        public abstract bool UseSmooth { get; set; }

        /// <summary>
        /// Detect faces in full image. Return empty array when nothing found.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="debug"></param>
        /// <returns></returns>
        public abstract FaceRect[] Detect(Mat frame, bool debug = false);
        public abstract void Dispose();

        protected List<FaceSmoother> Smoother = new List<FaceSmoother>();
        protected FaceSmoother GetSmoother(int index)
        {
            for (int i = 0; i < index - Smoother.Count + 1; i++)
            {
                Smoother.Add(new FaceSmoother());
            }
            return Smoother[index];
        }
    }

    public class OpenFaceModelLoader
    {
        static OpenFaceModelLoader defaultModel;
        public static OpenFaceModelLoader Default
        {
            get
            {
                if(defaultModel == null)
                {
                    defaultModel = new OpenFaceModelLoader();
                }
                return defaultModel;
            }
        }
        public static ManifestResource ModelResource = new ManifestResource("Vision.Detection", "openface_model.zip");

        public bool Loaded { get; set; } = false;

        public void Load()
        {
            var file = Storage.LoadResource(ModelResource, true);
            Storage.UnZip(file, Storage.Root, true);
            Loaded = true;
        }
    }

    public class OpenFaceDetector : FaceDetectionProvider
    {
        public LandmarkDetectorWrap Detector { get; set; }

        public InterpolationFlags Interpolation { get; set; } = InterpolationFlags.Nearest;

        public override double UnitPerMM => 1;
        public override bool UseSmooth
        {
            get => Detector.InVideo;
            set { Detector.InVideo = value; }
        }
        public double MaxSize { set; get; } = 320;

        public OpenFaceDetector(OpenFaceModelLoader loader)
        {
            Logger.Log(this, "Start Load CLNF Model");
            if (loader != null && !loader.Loaded)
            {
                loader.Load();
            }
            Detector = new LandmarkDetectorWrap(Storage.Root.AbosolutePath);
            var model = new FileNode(Detector.Parameters.model_location, true);
            if (Storage.IsExist(model))
            {
                Detector.Load();
            }
            else
            {
                throw new Exception("Model is not found");
            }
            Logger.Log(this, "Finish Load CLNF Model");
        }
        
        /// <summary>
        /// default load behavior use default
        /// </summary>
        public OpenFaceDetector() : this(OpenFaceModelLoader.Default)
        {

        }

        public override FaceRect[] Detect(Mat frame, bool debug = false)
        {
            List<FaceRect> faces = new List<FaceRect>();

            Profiler.Start("Detector.Resize");
            var scale = frame.CalcScaleFactor(MaxSize);
            using (var resize = new Mat())
            {
                Cv2.Resize(frame, resize, new OpenCvSharp.Size(frame.Width * scale, frame.Height * scale), 0, 0, Interpolation);
                Profiler.End("Detector.Resize");

                Profiler.Start("Detector.DetectImage");
                if (Detector.DetectImage(resize))
                {
                    Profiler.Start("Detector.GetRect");

                    var face = GetFaceRect(frame, 1 / scale);

                    faces.Add(face);

                    Profiler.End("Detector.GetRect");
                }
                Profiler.End("Detector.DetectImage");
            }
            return faces.ToArray();
        }

        private FaceRect GetFaceRect(Mat frame, double scale = 1)
        {
            var boundBox = Detector.BoundaryBox.ToRect() * scale;
            var boundBoxMax = Math.Max(boundBox.Width, boundBox.Height) * 1.1;
            var oriBox = new Rect(boundBox.Center - new Point(boundBoxMax / 2, boundBoxMax / 2), new Size(boundBoxMax));
            var oriLT = new Point(oriBox.X, oriBox.Y);
            var oriRB = new Point(oriBox.X + oriBox.Width, oriBox.Y + oriBox.Height);
            var minPt = new Point(0, 0);
            var maxPt = new Point(frame.Width - 1, frame.Height - 1);
            oriLT = MatTool.Clamp(oriLT, minPt, maxPt);
            oriRB = MatTool.Clamp(oriRB, minPt, maxPt);
            var realCenter = MatTool.Average(oriRB, oriLT);
            var realSize = Math.Min(Math.Abs(oriLT.X - oriRB.X), Math.Abs(oriLT.Y - oriRB.Y));
            var realBox = new Rect(realCenter.X - realSize / 2, realCenter.Y - realSize / 2, realSize, realSize);

            var face = new FaceRect(realBox, GetSmoother(0));

            var landmarks = Detector.Landmarks;
            var convertedLandmark = new Point[landmarks.Count];
            var i = 0;
            foreach (var item in landmarks)
            {
                convertedLandmark[i] = new Point(item.X * scale, item.Y * scale); i++;
            }
            face.Landmarks = convertedLandmark;

            face.LandmarkTransform = new Point3D(Detector.Transform);
            face.LandmarkRotation = new Point3D(Detector.RodriguesRotation);
            face.LandmarkDistCoeffs = new double[4] { 0, 0, 0, 0 };
            face.LandmarkCameraMatrix = MatTool.CameraMatrixArray(Detector.FocalX * scale, Detector.FocalY * scale, Detector.CenterX * scale, Detector.CenterY * scale);
            face.UnitPerMM = UnitPerMM;

            var left = GetEye(face, 36, 41);
            var right = GetEye(face, 42, 47);
            face.Add(left);
            face.Add(right);
            
            return face;
        }

        private EyeRect GetEye(FaceRect rect, int start, int end)
        {
            var data = rect.Landmarks;
            var center = new Point();
            var max = new Point();
            var min = new Point(1000000, 1000000);
            for (int i = start; i <= end; i++)
            {
                var x = data[i].X;
                var y = data[i].Y;
                center.X += x;
                center.Y += y;
                max.X = Math.Max(max.X, x);
                max.Y = Math.Max(max.Y, y);
                min.X = Math.Min(min.X, x);
                min.Y = Math.Min(min.Y, y);
            }
            center.X /= end - start + 1;
            center.Y /= end - start + 1;

            var landmarkSize = Math.Max(max.X - min.X, max.Y - min.Y) * 1.25;
            var faceSize = Math.Max(rect.Width, rect.Height) * 0.33;

            var avgSize = (faceSize + landmarkSize) / 2;
            center.X -= avgSize / 2;
            center.Y -= avgSize / 2;
            center.X -= rect.X;
            center.Y -= rect.Y;
            var ret = new EyeRect(rect, new Rect(center, new Size(avgSize)));

            return ret;
        }

        public override void Dispose()
        {
            if (Detector != null)
            {
                Detector.Dispose();
                Detector = null;
            }
        }
    }

    public class FaceDetector : FaceDetectionProvider
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

        public override double UnitPerMM => Flandmark.UnitPerMilimeter;
        public bool LandmarkDetect { get; set; } = true;
        public bool LandmarkSolve { get; set; } = true;

        public bool SmoothLandmarks { get; set; } = false;
        public bool SmoothVectors { get; set; } = false;
        public bool ClampVectors { get; set; } = true;
        public override bool UseSmooth
        {
            get => SmoothLandmarks && SmoothVectors;
            set => SmoothVectors = SmoothLandmarks = value;
        }

        public InterpolationFlags Interpolation { get; set; } = InterpolationFlags.Nearest;

        CascadeClassifier FaceCascade;
        CascadeClassifier EyesCascade;
        FaceLandmarkDetector Landmark;

        public FaceDetector(string FaceXml, string EyesXml, FileNode flandmark)
        {
            FaceCascade = new CascadeClassifier(FaceXml);
            EyesCascade = new CascadeClassifier(EyesXml);
            Landmark = new FaceLandmarkDetector(flandmark);
        }

        public FaceDetector(FaceDetectorXmlLoader Loader, FlandmarkModelLoader loader) : this(Loader.FaceXmlPath, Loader.EyeXmlPath, loader.Data)
        {

        }

        public FaceDetector() : this(new FaceDetectorXmlLoader(), new FlandmarkModelLoader())
        {

        }
        
        public override FaceRect[] Detect(Mat frame, bool debug = false)
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
                    FaceSmoother smoothCurrent = GetSmoother(index);

                    FaceRect faceRect = new FaceRect(face.ToRect(), smoothCurrent);
                    faceRect.Scale(1 / scaleFactor);
                    faceRect.UnitPerMM = UnitPerMM;

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

        public override void Dispose()
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
                    face.LandmarkTransform = new Point3D(TranslateFilter.Clone().Calculate(face.LandmarkTransformVector));
                    vectorInverted = false;
                }
                else
                {
                    face.LandmarkTransform = new Point3D(TranslateFilter.Calculate(face.LandmarkTransformVector));
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
