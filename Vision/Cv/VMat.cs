using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Vision.Cv
{
    public abstract class VMat : VirtualObject, IDisposable
    {
        public virtual bool IsEmpty => Object == null || Empty();
        public virtual Size Size => (Object == null) ? null : GetSize();
        public virtual double Width => Size.Width;
        public virtual double Height => Size.Height;
        public virtual int Channel => GetChannel();
        public virtual int Cols => GetCols();
        public virtual int Rows => GetRows();
        public virtual long Total => GetTotal();

        #region Init

        public static VMat New()
        {
            return Core.Cv.CreateMat();
        }

        public static VMat New(Size size)
        {
            return Core.Cv.CreateMat(size);
        }

        public static VMat New(Size size, MatType type)
        {
            return Core.Cv.CreateMat(size, type);
        }

        public static VMat New(Size size, MatType type, Array buffer)
        {
            return Core.Cv.CreateMat(size, type, buffer);
        }

        public static VMat New(VMat Mat, Rect Rect, bool clamp = false)
        {
            double clmpX = Math.Max(0, Rect.X);
            double clmpY = Math.Max(0, Rect.Y);
            double clmpW = Math.Min(Mat.Width - 1, Rect.X + Rect.Width) - clmpX;
            double clmpH = Math.Min(Mat.Height - 1, Rect.Y + Rect.Height) - clmpY;

            return Core.Cv.CreateMat(Mat, new Rect(clmpX, clmpY, clmpW, clmpH));
        }

        #endregion Init

        #region ImgProc

        public void ConvertColor(VMat output, ColorConversion convert)
        {
            Core.Cv.ConvertColor(this, output, convert);
        }

        public void ConvertColor(ColorConversion convert)
        {
            ConvertColor(this, convert);
        }

        public void EqualizeHistogram(VMat output)
        {
            Core.Cv.EqualizeHistogram(this, output);
        }

        public void EqualizeHistogram()
        {
            EqualizeHistogram(this);
        }

        public void Canny()
        {
            Canny(this, 50, 100);
        }

        public void Canny(VMat output, double thresold1, double thresold2)
        {
            Core.Cv.Canny(this, output, thresold1, thresold2);
        }

        public void NormalizeRGB(VMat output, double clip)
        {
            if (Channel != 3)
                throw new NotSupportedException("Channel sould be RGB");

            ConvertColor(ColorConversion.BgrToLab);

            VMat[] spl = Split();

            CLAHE c = CLAHE.New(clip, new Size(8, 8));
            c.Apply(spl[0]);

            Merge(spl);

            ConvertColor(ColorConversion.LabToBgr);
        }

        public void NormalizeRGB(VMat output)
        {
            NormalizeRGB(output, 1);
        }

        public void NormalizeRGB()
        {
            NormalizeRGB(this);
        }

        public abstract VMat[] Split();
        public abstract void Merge(VMat[] channels);

        public double CalcScaleFactor(double maxsize)
        {
            double fwidth = Width;
            double fheight = Height;

            double scaleFactor = 1;

            if (Math.Max(fwidth, fheight) > maxsize)
            {
                if (fwidth > fheight)
                {
                    scaleFactor = maxsize / fwidth;
                }
                else
                {
                    scaleFactor = maxsize / fheight;
                }
            }

            return scaleFactor;
        }

        public double ClampSize(double maxsize, Interpolation inter = Interpolation.NearestNeighbor)
        {
            double scaleFactor = CalcScaleFactor(maxsize);

            if (scaleFactor != 1)
            {
                Resize(scaleFactor, inter);
            }

            return scaleFactor;
        }

        public void Resize(double scaleFactor, Interpolation inter = Interpolation.Linear)
        {
            Resize(new Size(Width * scaleFactor, Height * scaleFactor), 0, 0, inter);
        }

        public void Resize(Size size, double fx = 0, double fy = 0, Interpolation inter = Interpolation.Linear)
        {
            Resize(this, size, fx, fy, inter);
        }

        public void Resize(VMat output, Size size, double fx = 0, double fy = 0, Interpolation inter = Interpolation.Linear)
        {
            Core.Cv.Resize(this, output, size, fx, fy, inter);
        }

        #endregion ImgProc

        #region Draw

        public void DrawText(double x, double y, string text)
        {
            DrawText(x, y, text, Scalar.BgrWhite);
        }

        public void DrawText(double x, double y, string text, Scalar color)
        {
            DrawText(new Point(x, y), text, color);
        }

        public void DrawText(Point org, string txt)
        {
            DrawText(org, txt, Scalar.BgrWhite);
        }

        public void DrawText(Point org, string txt, Scalar color)
        {
            Core.Cv.DrawText(this, txt, org, FontFace.HersheyPlain, 2.5, color, 2, LineType.Link4);
        }

        public void DrawRectangle(Rect rect, Scalar color, int thickness = 1, LineType lineType = LineType.Link8, int shift = 0)
        {
            Core.Cv.DrawRectangle(this, rect, color, thickness, lineType, shift);
        }

        public void DrawCircle(Point center, double radius, Scalar color, double thickness = 1, LineType lineType = LineType.Link8, int shift = 0)
        {
            Core.Cv.DrawCircle(this, center, radius, color, thickness, lineType, shift);
        }

        public void DrawEllipse(Point center, Size axes, double angle, double startAngle, double endAngle, Scalar color, double thickness = 1, LineType lineType = LineType.Link8, int shift = 0)
        {
            Core.Cv.DrawEllipse(this, center, axes, angle, startAngle, endAngle, color, thickness, lineType, shift);
        }

        public void DrawLine(double x, double y, double x1, double y1)
        {
            DrawLine(new Point(x, y), new Point(x1, y1));
        }

        public void DrawLine(Point start, Point end)
        {
            DrawLine(start, end, Scalar.BgrWhite);
        }

        public void DrawLine(Point start, Point end, Scalar scalar, double thickness = 1, LineType lineType = LineType.Link8, int shift = 0)
        {
            Core.Cv.DrawLine(this, start, end, scalar, (int)thickness, lineType, shift);
        }

        #endregion Draw

        #region Math

        public VMat Transpose()
        {
            return Core.Cv.Transpose(this);
        }

        public VMat T()
        {
            return Transpose();
        }

        public VMat Inv()
        {
            return Core.Cv.Inv(this);
        }

        public VMat Mul(VMat right)
        {
            return Core.Cv.Mul(this, right);
        }
        
        public static VMat operator * (VMat left, VMat right)
        {
            return Core.Cv.Mul(left, right);
        }

        #endregion Math

        #region Misc

        protected abstract Size GetSize();
        protected abstract bool Empty();
        protected abstract int GetChannel();
        protected abstract int GetCols();
        protected abstract int GetRows();
        protected abstract long GetTotal();

        public abstract T At<T>(int d1, int d2) where T : struct;
        public abstract void CopyTo(VMat dist);
        public abstract void CopyTo(VMat dist, VMat mask);
        /// <summary>
        /// return RGB float Array. Bgr2Rgb internally
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public abstract float[] GetArray(float[] buffer = null);
        public abstract VMat Clone();
        public abstract void Dispose();

        public string Print()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{ ");
            for (int r = 0; r < Rows; r++)
            {
                builder.Append("{ ");
                for (int c = 0; c < Cols; c++)
                {
                    builder.Append($"{At<double>(r, c)}, ");
                }
                builder.AppendLine("}");
            }
            builder.AppendLine("}");
            return builder.ToString();
        }

        #endregion Misc
    }
}
