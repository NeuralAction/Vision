using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public Point()
        {

        }

        public double EucludianLength()
        {
            return Math.Sqrt(X*X+Y*Y);
        }

        public static Point operator +(Point a, Point b)
        {
            return new Point(a.X + b.X, a.Y + b.Y);
        }

        public static Point operator - (Point a, Point b)
        {
            return new Point(a.X - b.X, a.Y - b.Y);
        }

        public static double EucludianDistance(Point a, Point b)
        {
            return (a - b).EucludianLength();
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }

    public class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3D(double X, double Y, double Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public Point3D(double[] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Length != 3)
                throw new ArgumentOutOfRangeException(nameof(array));

            X = array[0];
            Y = array[1];
            Z = array[2];
        }

        public Point3D()
        {

        }

        public static Point3D operator +(Point3D a, Point3D b)
        {
            return new Point3D(a.X+b.X, a.Y+b.Y, a.Z+b.Z);
        }

        public static Point3D operator -(Point3D a, Point3D b)
        {
            return new Point3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public double[] ToArray()
        {
            return new double[] { X, Y, Z };
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
