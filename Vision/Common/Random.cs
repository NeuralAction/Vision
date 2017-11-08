using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public class Random
    {
        public static Random R { get; private set; } = new Random();

        System.Random rnd = new System.Random((int)(DateTime.UtcNow.TimeOfDay.TotalMilliseconds));

        public Random()
        {

        }

        public int NextInt(int min, int max)
        {
            return rnd.Next(min, max);
        }

        public double NextDouble(double min, double max)
        {
            return (double)rnd.Next((int)(min * 1500), (int)(max * 1500)) / 1500;
        }
    }
}
