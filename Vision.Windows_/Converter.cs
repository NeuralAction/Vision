using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;

namespace Vision.Windows
{
    public static class Converter
    {
        public static System.Drawing.Bitmap ToBitmap(this Mat x)
        {
            return MatToBitmap(x);
        }

        public static System.Windows.Media.Imaging.BitmapSource ToBitmapSource(this Mat x)
        {
            return MatToBitmapSource(x);
        }

        public static System.Drawing.Bitmap MatToBitmap(Mat mat)
        {
            return OpenCvSharp.Windows.BitmapConverter.ToBitmap(mat);
        }

        public static System.Windows.Media.Imaging.BitmapSource MatToBitmapSource(Mat mat)
        {
            return OpenCvSharp.Windows.BitmapSourceConverter.ToBitmapSource(mat);
        }

        public static System.Windows.Point ToPoint(this Point pt)
        {
            return new System.Windows.Point(pt.X, pt.Y);
        }

        public static Point ToPoint(this System.Windows.Point pt)
        {
            return new Point(pt.X, pt.Y);
        }
    }
}
