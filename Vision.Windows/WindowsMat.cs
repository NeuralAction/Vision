using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;

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
            set
            {
                throw new NotSupportedException();
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

        public WindowsMat(Size size, Cv.MatType type)
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

        public override float[] GetArray(float[] buffer=null)
        {
            int width = (int)Width;
            int height = (int)Height;
            float[] f; // = new float[(int)width * (int)height * Channel];
            if (buffer == null)
            {
                f = new float[width * height * Channel];
            }
            else
            {
                if (buffer.Length < width * height * Channel)
                    throw new ArgumentOutOfRangeException(nameof(buffer));
                f = buffer;
            }
            using (MatOfByte3 matByte = new MatOfByte3())
            {
                InnerMat.CopyTo(matByte);

                var indexer = matByte.GetIndexer();
                int i = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vec3b color = indexer[y, x];
                        f[i] = (float)color.Item2;
                        i++;
                        f[i] = (float)color.Item1;
                        i++;
                        f[i] = (float)color.Item0;
                        i++;
                    }
                }
            }

            return f;
        }

        public override VMat[] Split()
        {
            Mat[] spl = InnerMat.Split();
            List<VMat> ret = new List<VMat>();
            foreach(Mat m in spl)
            {
                ret.Add(new WindowsMat(m));
            }
            return ret.ToArray();
        }

        protected override int GetChannel()
        {
            return InnerMat.Channels();
        }

        protected override long GetTotal()
        {
            return InnerMat.Total();
        }

        public override void Merge(VMat[] channels)
        {
            List<Mat> m = new List<Mat>();
            foreach(VMat v in channels)
            {
                m.Add((Mat)v.Object);
            }
            Cv2.Merge(m.ToArray(), (Mat)Object);
        }

        public override T At<T>(int d1, int d2)
        {
            return InnerMat.At<T>(d1, d2);
        }

        protected override int GetCols()
        {
            return InnerMat.Cols;
        }

        protected override int GetRows()
        {
            return InnerMat.Rows;
        }
    }
}
