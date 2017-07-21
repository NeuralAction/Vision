using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    //ref. https://stackoverflow.com/questions/39315817/filtering-streaming-data-to-reduce-noise-kalman-filter-c-sharp
    public class KalmanFilter
    {
        double A = double.Parse("1");
        double H = double.Parse("1");
        double P = double.Parse("0.1");
        double Q = double.Parse("0.125");
        double R = double.Parse("1");
        double K;
        double z;
        double x;
        bool init = false;

        public KalmanFilter()
        {

        }

        public double Calculate(double d)
        {
            if (!init)
            {
                x = d;
                init = true;
            }

            z = d;

            x = A * x;
            P = A * P * A + Q;

            K = P * H / (H * P * H + R);
            x = x + K * (z - H * x);
            P = (1 - K * H) * P;

            return x;
        }
    }

    public class PointKalmanFilter
    {
        KalmanFilter filterX = new KalmanFilter();
        KalmanFilter filterY = new KalmanFilter();

        public Point Calculate(Point pt)
        {
            Point ret = new Point(0,0);
            ret.X = filterX.Calculate(pt.X);
            ret.Y = filterY.Calculate(pt.Y);
            return ret;
        }
    }

    public class ArrayKalmanFilter
    {
        KalmanFilter[] filters;

        public ArrayKalmanFilter(int length)
        {
            filters = new KalmanFilter[length];
            for (int i = 0; i < length; i++)
                filters[i] = new KalmanFilter();
        }

        public double[] Calculate(double[] array)
        {
            double[] ret = new double[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                ret[i] = filters[i].Calculate(array[i]);
            }
            return ret;
        }
    }
}
