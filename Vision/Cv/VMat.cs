using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Vision
{
    public abstract class VMat : VirtualObject, IDisposable
    {
        public virtual bool IsEmpty { get => Object == null || Empty(); }

        public virtual Size Size { get => (Object == null) ? null : GetSize(); }

        public virtual double Width { get => Size.Width;  }

        public virtual double Height { get => Size.Height; }

        public virtual int Channel { get => GetChannel(); }

        public virtual long Total { get => GetTotal(); }

        public static VMat New()
        {
            return Core.Cv.CreateMat();
        }

        public static VMat New(VMat Mat, Rect Rect)
        {
            return Core.Cv.CreateMat(Mat, Rect);
        }

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

            if(scaleFactor != 1)
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

        public void DrawText(double x, double y, string text)
        {
            DrawText(x, y, text, Scalar.White);
        }

        public void DrawText(double x, double y, string text, Scalar color)
        {
            DrawText(new Point(x, y), text, color);
        }

        public void DrawText(Point org, string txt)
        {
            DrawText(org, txt, Scalar.White);
        }

        public void DrawText(Point org, string txt, Scalar color)
        {
            Core.Cv.DrawText(this, txt, org, FontFace.HersheyPlain, 2.5, color, 2, LineType.Link4);
        }

        public void Transpose()
        {
            Core.Cv.Transpose(this, this);
        }

        protected abstract Size GetSize();
        protected abstract bool Empty();
        protected abstract int GetChannel();
        protected abstract long GetTotal();
        
        public abstract void CopyTo(VMat dist);
        public abstract void CopyTo(VMat dist, VMat mask);
        public abstract float[] GetArray();
        public abstract VMat Clone();
        public abstract void Dispose();
    }
}
