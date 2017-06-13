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
using OpenCV.Core;

namespace Vision.Android
{
    public class AndroidMat : VMat
    {
        public override object Object
        {
            get { return InnerMat; }
        }
        public Mat InnerMat;

        public AndroidMat()
        {
            InnerMat = new Mat();
        }

        public AndroidMat(Mat mat)
        {
            InnerMat = mat;
        }
        
        public AndroidMat(Size size)
        {
            InnerMat = new Mat(size.ToCvSize(), MatType.CV_8UC3.Value);
        }

        public AndroidMat(Size size, MatType type)
        {
            InnerMat = new Mat(size.ToCvSize(), type.Value);
        }

        public AndroidMat(VMat mat)
        {
            InnerMat = (Mat)mat.Object;
        }

        public AndroidMat(VMat mat, Rect rect)
        {
            InnerMat = new Mat((Mat)mat.Object, new OpenCV.Core.Rect((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
        }

        public override void Dispose()
        {
            if(InnerMat != null)
            {
                InnerMat.Dispose();
                InnerMat = null;
            }

            GC.SuppressFinalize(this);
        }

        protected override bool Empty()
        {
            return InnerMat.Empty();
        }

        protected override Size GetSize()
        {
            OpenCV.Core.Size size = InnerMat.Size();

            return new Size(size.Width, size.Height);
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
            return new AndroidMat(InnerMat.Clone());
        }
    }
}