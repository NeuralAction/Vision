using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public class Scalar
    {
        public static Scalar White => new Scalar(255, 255, 255);
        public static Scalar Gray => new Scalar(128, 128, 128);
        public static Scalar Black => new Scalar(0, 0, 0);
        public static Scalar Red => new Scalar(255, 0, 0, 255);
        public static Scalar Green => new Scalar(0, 255, 0);
        public static Scalar Blue => new Scalar(0, 0, 255);
        public static Scalar Magenta => new Scalar(255, 0, 255);
        public static Scalar Yellow => new Scalar(255, 255, 0);
        public static Scalar Cyan => new Scalar(0, 255, 255);

        public double Value1;
        public double Value2;
        public double Value3;
        public double Value4;

        public Scalar(double val1 = 0, double val2 = 0, double val3 = 0, double val4 = 255)
        {
            Value1 = val1;
            Value2 = val2;
            Value3 = val3;
            Value4 = val4;
        }

        public static Scalar All(double val)
        {
            return new Scalar(val, val, val, val);
        }
    }
}
