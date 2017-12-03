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
using Vision.Cv;

namespace Vision.Android
{
    public static class Converter
    {
        public static OpenCV.Core.Size ToCvSize(this Size s)
        {
            return new OpenCV.Core.Size(s.Width, s.Height);
        }

        public static OpenCV.Core.Point ToCvPoint(this Point pt)
        {
            return new OpenCV.Core.Point(pt.X, pt.Y);
        }

        public static OpenCV.Core.Rect ToCvRect(this Rect rt)
        {
            return new OpenCV.Core.Rect(rt.Point.ToCvPoint(), rt.Size.ToCvSize());
        }

        public static OpenCV.Core.Scalar ToCvScalar(this Scalar s)
        {
            return new OpenCV.Core.Scalar(s.Value1, s.Value2, s.Value3, s.Value4);
        }

        public static OpenCV.Core.Mat ToCvMat(this VMat mat)
        {
            return (OpenCV.Core.Mat)mat.Object;
        }

        public static OpenCV.Core.Mat ToCvMat(int row, int col, double[] buffer)
        {
            OpenCV.Core.Mat m = new OpenCV.Core.Mat(row, col, MatType.CV_64FC1);
            m.Put(0, 0, buffer);
            return m;
        }

        public static OpenCV.Core.Mat ToCvMat(int row, int col, double[,] buffer)
        {
            OpenCV.Core.Mat m = new OpenCV.Core.Mat(row, col, MatType.CV_64FC1);
            for (int r = 0; r < row; r++)
                for (int c = 0; c < col; c++)
                    m.Put(r, c, buffer[r, c]);
            return m;
        }

        public static double[,] ToMatrixArray(int row, int col, double[] buffer)
        {
            double[,] ret = new double[row, col];
            for(int r = 0; r < row; r++)
            {
                for (int c = 0; c < col; c++)
                {
                    //TODO: index fix
                    ret[r, c] = buffer[r * col + c];
                }
            }
            return ret;
        }

        public static VMat ToVMat(this OpenCV.Core.Mat m)
        {
            return new AndroidMat(m);
        }
    }
}