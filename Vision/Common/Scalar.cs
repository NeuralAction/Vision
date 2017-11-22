using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public class Scalar
    {
        public static Scalar BgrWhite = new Scalar(255, 255, 255);
        public static Scalar BgrGray = new Scalar(128, 128, 128);
        public static Scalar BgrBlack = new Scalar(0, 0, 0);
        public static Scalar BgrRed = new Scalar(0, 0, 255);
        public static Scalar BgrGreen = new Scalar(0, 255, 0);
        public static Scalar BgrBlue = new Scalar(255, 0, 0);
        public static Scalar BgrMagenta = new Scalar(255, 0, 255);
        public static Scalar BgrYellow = new Scalar(0, 255, 255);
        public static Scalar BgrCyan = new Scalar(255, 255, 0);
        public static Scalar BgrOrange = new Scalar(0, 165, 255);

        private static Random rand = new Vision.Random();

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

        public OpenCvSharp.Scalar ToCvScalar()
        {
            return new OpenCvSharp.Scalar(Value1, Value2, Value3, Value4);
        }

        public static Scalar All(double val)
        {
            return new Scalar(val, val, val, val);
        }

        public static Scalar Random()
        {
            return new Scalar(rand.NextDouble(0, 255), rand.NextDouble(0, 255), rand.NextDouble(0, 255));
        }

        public static Scalar Blend(Scalar a, double alphaA, Scalar b, double alphaB)
        {
            var aMul = alphaA / (alphaA + alphaB);
            var bMul = alphaB / (alphaA + alphaB);
            return new Scalar(
                a.Value1 * aMul + b.Value1 * bMul,
                a.Value2 * aMul + b.Value2 * bMul,
                a.Value3 * aMul + b.Value3 * bMul,
                a.Value4 * aMul + b.Value4 * bMul);
        }

        public static explicit operator OpenCvSharp.Scalar(Scalar s)
        {
            return s.ToCvScalar();
        }

        public static implicit operator Scalar(OpenCvSharp.Scalar s)
        {
            return new Scalar(s.Val0, s.Val1, s.Val2, s.Val3);
        }
    }
}
