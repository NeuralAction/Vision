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
using Vision;

namespace Vision.Android
{
    public class AndroidMat : VMat
    {
        public override object Object
        {
            get { return InnerMat; }
            set { throw new NotImplementedException(); }
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

        public override float[] GetArray()
        {
            Profiler.Start("MatGetArray");

            int width = (int)Width;
            int height = (int)Height;
            int len = (int)width * (int)height * Channel;
            double[] buffer = new double[len];
            float[] f = new float[len];
            int i = 0;
            int b = 0;

            using (Mat m = new Mat())
            {
                InnerMat.ConvertTo(m, CvType.Cv64fc3);
                m.Get(0, 0, buffer);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        //BGR to RGB
                        f[i] = (float)buffer[b + 2];
                        i++;
                        f[i] = (float)buffer[b + 1];
                        i++;
                        f[i] = (float)buffer[b + 0];
                        i++;
                        b += 3;
                    }
                }
            }

            Profiler.End("MatGetArray");
            return f;
        }

        protected override int GetChannel()
        {
            return InnerMat.Channels();
        }

        protected override long GetTotal()
        {
            return InnerMat.Total();
        }

        public override VMat[] Split()
        {
            int ch = Channel;
            Mat[] spl = new Mat[ch];
            for(int i=0; i<ch; i++)
            {
                spl[i] = new Mat();
            }
            OpenCV.Core.Core.Split(InnerMat, spl);

            Mat[] mspl = spl;

            List<VMat> ret = new List<VMat>();
            foreach(Mat m in mspl)
            {
                ret.Add(new AndroidMat(m));
            }
            return ret.ToArray();
        }

        public override void Merge(VMat[] channels)
        {
            List<Mat> c = new List<Mat>();
            foreach(VMat m in channels)
            {
                c.Add((Mat)((AndroidMat)m).Object);
            }
            OpenCV.Core.Core.Merge(c, InnerMat);
        }
    }
}