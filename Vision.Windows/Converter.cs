using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Windows
{
    public static class Converter
    {
        public static System.Drawing.Bitmap ToBitmap(this VMat x)
        {
            return VMatToBitmap(x);
        }

        public static System.Windows.Media.Imaging.BitmapSource ToBitmapSource(this VMat x)
        {
            return VMatToBitmapSource(x);
        }

        public static OpenCvSharp.Size ToCvSize(this Size s)
        {
            return new OpenCvSharp.Size(s.Width, s.Height);
        }

        public static OpenCvSharp.Rect ToCvRect(this Rect r)
        {
            return new OpenCvSharp.Rect((int)r.Width, (int)r.Height, (int)r.Width, (int)r.Height);
        }

        public static System.Drawing.Bitmap VMatToBitmap(VMat mat)
        {
            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap((OpenCvSharp.Mat)mat.Object);
        }

        public static System.Windows.Media.Imaging.BitmapSource VMatToBitmapSource(VMat mat)
        {
            return OpenCvSharp.Extensions.BitmapSourceConverter.ToBitmapSource((OpenCvSharp.Mat)mat.Object);
        }
    }
}
