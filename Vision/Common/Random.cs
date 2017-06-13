using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public class Random
    {
        private System.Random rnd = new System.Random((int)(DateTime.UtcNow.TimeOfDay.TotalMilliseconds));

        public Random()
        {

        }

        public double NextDouble(double min, double max)
        {
            return (double)rnd.Next((int)(min * 1500), (int)(max * 1500)) / 1500;
        }
    }
}
