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

        public Point3D()
        {

        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
