using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Cv
{
    public abstract class Cv
    {
        //Properties
        private static Cv _context;
        public static Cv Context
        {
            get
            {
                return _context;
            }
        }

        public virtual string BuildInformation => GetBuildInformation();
        public virtual int NumThreads { get => GetNumThreads(); set => SetNumThreads(value); }
        public virtual bool UseOptimized { get => GetUseOptimized(); set => SetUseOptimized(value); }

        //Get
        protected abstract string GetBuildInformation();
        protected abstract int GetNumThreads();
        protected abstract void SetNumThreads(int t);
        protected abstract bool GetUseOptimized();
        protected abstract void SetUseOptimized(bool b);

        //Initializer
        internal protected abstract VMat CreateMat();
        internal protected abstract VMat CreateMat(Size size);
        internal protected abstract VMat CreateMat(Size size, MatType type);
        internal protected abstract VMat CreateMat(Size size, MatType type, Array buffer);
        internal protected abstract VMat CreateMat(VMat mat, Rect rect);

        internal protected abstract CLAHE CreateCLAHE(double clip, Size gridSize);

        internal protected abstract CascadeClassifier CreateCascadeClassifier(string filePath);

        internal protected abstract Capture CreateCapture(int index);
        internal protected abstract Capture CreateCapture(string filePath);
        
        //Draw
        public abstract void DrawCircle(VMat img, Point center, double radius, Scalar color, double thickness = 1, LineType lineType = LineType.Link8, int shift = 0);
        public abstract void DrawEllipse(VMat img, Point center, Size axes, double angle, double startAngle, double endAngle, Scalar color, double thickness = 1, LineType lineType = LineType.Link8, int shift = 0);
        public abstract void DrawLine(VMat img, Point start, Point end, Scalar color, int thickness = 1, LineType lineType = LineType.Link8, int shift = 0);
        public abstract void DrawText(VMat img, string text, Point org, FontFace fontFace, double fontScale, Scalar color, int thickness = 1, LineType lineType = LineType.Link8, bool bottomLeftOrigin = false);
        public abstract void DrawRectangle(VMat img, Rect rect, Scalar color, int thickness = 1, LineType lineType = LineType.Link8, int shift = 0);

        //ImgProc
        public abstract void WarpPerspective(VMat src, VMat dst, VMat transform, Size dsize, Interpolation flags = Interpolation.Linear, BorderTypes borderMode = BorderTypes.Constant, Scalar borderValue = null);
        public abstract void ConvertColor(VMat src, VMat output, ColorConversion convertMode);
        public abstract void EqualizeHistogram(VMat input, VMat output);
        public abstract void Canny(VMat Input, VMat output, double thresold1, double thresold2, int apertureSize = 3, bool L2gradient = false);

        //Matrix
        public abstract VMat Mul(VMat left, VMat right);
        public abstract void Mul(VMat output, VMat left, VMat right);
        public abstract VMat Inv(VMat input);
        public abstract void Inv(VMat input, VMat output);
        public abstract VMat Transpose(VMat input);
        public abstract void Transpose(VMat input, VMat output);
        public abstract void Resize(VMat input, VMat dist, Size size, double fx = 0, double fy = 0, Interpolation inter = Interpolation.Linear);

        //GlobalFunc
        internal static void Init(Cv Context)
        {
            if (_context == null)
            {
                _context = Context;

                Logger.Log("Cv", $"Context is created. New Context: [{Context.ToString()}]");
                Logger.Log("Cv", $"NumThreads: {Context.NumThreads}");
                Logger.Log("Cv", $"UseOptimized: {Context.UseOptimized}");
            }
            else
            {
                Logger.Error("Cv", "Context is already created");
            }
        }
        protected abstract void InternalImgShow(string name, VMat img);
        public void ImgShow(string name, VMat img)
        {
            try
            {
                if (name == null)
                    throw new ArgumentNullException();
                if (img == null || img.IsEmpty)
                    throw new ArgumentNullException();
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Error(this, "img is disposed");
                return;
            }

            InternalImgShow(name, img);
        }
        protected abstract VMat InternalImgRead(string path);
        public VMat ImgRead(FileNode node)
        {
            return ImgRead(node.AbosolutePath);
        }
        public VMat ImgRead(string path)
        {
            Logger.Log(this, "Image Read : " + path);

            return InternalImgRead(path);
        }

        protected abstract void InternalImgWrite(string name, VMat img, int quality);
        public void ImgWrite(FileNode node, VMat img, int quality = 80)
        {
            ImgWrite(node.AbosolutePath, img, quality);
        }
        public void ImgWrite(string path, VMat img, int quality = 80)
        {
            Logger.Log(this, "Image Write : " + path);

            InternalImgWrite(path, img, quality);
        }

        public abstract void CloseWindow(string name);
        public abstract void CloseAllWindows();
        public abstract char WaitKey(int duration);

        //Math
        public abstract void SolvePnP(List<Point3D> model_points, List<Point> image_point, double[,] cameraMatrix, double[] distCoeffs, out double[] rvec, out double[] tvec);
        public abstract void ProjectPoints(List<Point3D> objectPoints, double[] rvec, double[] tvec, double[,] cameraMatrix, double[] distCoeffs, out Point[] imagePoints, out double[,] jacobian);
        public void Rodrigues(double[] vector, out double[,] matrix)
        {
            double[,] jacobian;
            Rodrigues(vector, out matrix, out jacobian);
        }
        public abstract void Rodrigues(double[] vector, out double[,] matrix, out double[,] jacobian);
        public void Rodrigues(double[,] matrix, out double[] vector)
        {
            double[,] jacobian;
            Rodrigues(matrix, out vector, out jacobian);
        }
        public abstract void Rodrigues(double[,] matrix, out double[] vector, out double[,] jacobian);
        public double RodriguesTheta(double[] vector)
        {
            if (vector == null)
                throw new ArgumentNullException();
            if (vector.Length != 3)
                throw new ArgumentOutOfRangeException();

            return Math.Sqrt(vector[0] * vector[0] + vector[1] * vector[1] + vector[2] * vector[2]);
        }
        public double[] RodriguesVector(double[] vector)
        {
            if (vector == null)
                throw new ArgumentNullException();
            if (vector.Length != 3)
                throw new ArgumentOutOfRangeException();

            double theta = RodriguesTheta(vector);

            return new double[] { vector[0] / theta, vector[1] / theta, vector[2] / theta };
        }
    }
}
