using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Windows
{
    public class WindowsMat : VMat
    {
        public Mat InnerMat;
        public override object Object
        {
            get
            {
                return InnerMat;
            }
        }

        public WindowsMat()
        {
            InnerMat = new Mat();
        }

        public WindowsMat(Mat mat)
        {
            InnerMat = mat;
        }

        public WindowsMat(Size size)
        {
            InnerMat = new Mat(size.ToCvSize(), OpenCvSharp.MatType.CV_8UC3);
        }

        public WindowsMat(Size size, MatType type)
        {
            InnerMat = new Mat(size.ToCvSize(), new OpenCvSharp.MatType(type.Value));
        }

        public WindowsMat(VMat mat, Rect Rect)
        {
            InnerMat = new Mat((Mat)mat.Object, new OpenCvSharp.Rect((int)Rect.X, (int)Rect.Y, (int)Rect.Width, (int)Rect.Height));
        }

        protected override bool Empty()
        {
            if (InnerMat == null)
                return true;

            return InnerMat.Empty();
        }

        protected override Size GetSize()
        {
            if (InnerMat == null)
                return null;

            OpenCvSharp.Size size = InnerMat.Size();

            return new Size(size.Width, size.Height);
        }

        public override void Dispose()
        {
            if (InnerMat != null)
            {
                InnerMat.Dispose();
                InnerMat = null;
            }
        }

        public override void CopyTo(VMat dist)
        {
            InnerMat.CopyTo((Mat)dist.Object);
        }

        public override void CopyTo(VMat dist, VMat mask)
        {
            InnerMat.CopyTo((Mat)dist.Object, (Mat)mask.Object);
        }

        public override VMat Clone()
        {
            return new WindowsMat(InnerMat.Clone());
        }
    }
}
