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
    }
}