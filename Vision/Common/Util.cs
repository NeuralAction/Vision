using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public static class Util
    {
        public static byte[] ReadAll(this Stream s)
        {
            int len = (int)s.Length;
            byte[] buff = new byte[len];
            s.Read(buff, 0, len);
            return buff;
        }

        public static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        public static double Rad2Deg(double rad)
        {
            return rad / Math.PI * 180;
        }

        public static double Deg2Rad(double deg)
        {
            return deg / 180 * Math.PI;
        }

        public static double FixZero(double value, int level = 7)
        {
            if (Math.Abs(value) < Math.Pow(10, -level))
            {
                return 0;
            }
            return value;
        }

        public static Vector<double> CrossProduct(Vector<double> left, Vector<double> right)
        {
            if ((left.Count != 3 || right.Count != 3))
            {
                string message = "Vectors must have a length of 3.";
                throw new Exception(message);
            }
            var result = CreateVector.Dense<double>(3);
            result[0] = left[1] * right[2] - left[2] * right[1];
            result[1] = -left[0] * right[2] + left[2] * right[0];
            result[2] = left[0] * right[1] - left[1] * right[0];

            return result;
        }
    }
}
