using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Cv
{
    public static class Converter
    {
        public static Rect ToRect(this OpenCvSharp.Rect r)
        {
            return new Rect(r.X, r.Y, r.Width, r.Height);
        }

        public static Scalar ToScalar(this OpenCvSharp.Scalar s)
        {
            return new Scalar(s.Val0, s.Val1, s.Val2, s.Val3);
        }

        public static Size ToSize(this OpenCvSharp.Size s)
        {
            return new Size(s.Width, s.Height);
        }
    }
}
