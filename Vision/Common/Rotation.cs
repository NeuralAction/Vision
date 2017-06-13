using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public class Rotation
    {
        private double x;
        public double X { get => x; set => x = value; }

        private double y;
        public double Y { get => y; set => y = value; }

        private double z;
        public double Z { get => z; set => z = value; }
        
        public Rotation()
        {

        }

        public Rotation(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = y;
        }
    }
}
