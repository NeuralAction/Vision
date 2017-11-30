using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Detection
{
    public abstract class EyeGazeCalibratorEngineBase
    {
        public Dictionary<Point3D, CalibratingPushData> RawData { get; private set; } = new Dictionary<Point3D, CalibratingPushData>();
        public Dictionary<Point3D, CalibratingPushData> TrainData { get; private set; } = new Dictionary<Point3D, CalibratingPushData>();
        public Dictionary<Point3D, CalibratingPushData> TestData { get; private set; } = new Dictionary<Point3D, CalibratingPushData>();
        public double TestRatio { get; set; } = 0.1;

        public void SetData(Dictionary<Point3D, CalibratingPushData> data)
        {
            RawData = data;
            SuffleData();
        }

        public void SuffleData()
        {
            TestData.Clear();
            TrainData.Clear();

            foreach (var item in RawData)
            {
                if (Random.R.NextDouble(0, 1) < TestRatio)
                {
                    TestData.Add(item.Key, item.Value);
                }
                else
                {
                    TrainData.Add(item.Key, item.Value);
                }
            }
        }

        public abstract void Train();
        public abstract void Apply(FaceRect face, ScreenProperties screen);
    }

    public class LinearEyeGazeCalibratorEngine : EyeGazeCalibratorEngineBase
    {
        public LinearRegression X { get; set; } = new LinearRegression();
        public LinearRegression Y { get; set; } = new LinearRegression();

        public override void Apply(FaceRect face, ScreenProperties screen)
        {
            face.GazeInfo.Vector = Apply(face.GazeInfo.Vector);
            face.GazeInfo.UpdateScreenPoint(face, screen);
        }

        public Point3D Apply(Point3D vec)
        {
            var ret = EyeGazeInfo.ToGazeVector(vec);
            ret.X = X.Predict(ret.X);
            ret.Y = Y.Predict(ret.Y);
            return ret;
        }

        public override void Train()
        {
            CheckError();

            var trainX = new Point[RawData.Count];
            var trainY = new Point[RawData.Count];
            int i = 0;
            foreach (var item in RawData)
            {
                var label = EyeGazeInfo.ToGazeVector(item.Key);
                var train = EyeGazeInfo.ToGazeVector(item.Value.Face.GazeInfo.Vector);
                trainX[i] = new Point(train.X, label.X);
                trainY[i] = new Point(train.Y, label.Y);
                i++;
            }
            X.Train(trainX);
            Y.Train(trainY);

            CheckError();
        }

        private double CheckError()
        {
            var errors = new List<double>();
            foreach (var item in TestData)
            {
                var input = EyeGazeInfo.ToGazeVector(item.Value.Face.GazeInfo.Vector);
                var x = X.Predict(input.X);
                var y = Y.Predict(input.Y);
                var key = EyeGazeInfo.ToGazeVector(item.Key);
                var error = Math.Sqrt(Math.Pow(x - key.X, 2) + Math.Pow(y - key.Y, 2));
                errors.Add(error);
            }
            var errAvg = errors.Average();
            var errMax = errors.Max();

            Logger.Log(this, $"Error avg: {errAvg}");
            Logger.Log(this, $"Error max: {errMax}");
            return errAvg;
        }
    }

    public class LinearRegression
    {
        //ref: https://machinelearningmastery.com/implement-simple-linear-regression-scratch-python/

        public double A { get; set; } = 1;
        public double B { get; set; } = 0;

        public void Train(Point[] train)
        {
            var xs = new double[train.Length];
            var ys = new double[train.Length];
            for (int i = 0; i < train.Length; i++)
            {
                xs[i] = train[i].X;
                ys[i] = train[i].Y;
            }
            A = Covariance(xs, ys) / Variance(xs);
            B = ys.Average() - A * xs.Average();
        }

        public double Predict(double x)
        {
            return A * x + B;
        }

        private double Covariance(double[] x, double[] y)
        {
            double covar = 0.0, meanX = x.Average(), meanY = y.Average();
            for (int i = 0; i < x.Length; i++)
            {
                covar += (x[i] - meanX) * (y[i] - meanY);
            }
            return covar;
        }

        private double Variance(double[] x)
        {
            double mean = x.Average(), sum = 0;
            foreach (var item in x)
            {
                sum += Math.Pow(item - mean, 2);
            }
            return sum;
        }

        public override string ToString()
        {
            return $"y = {A} * x + {B}";
        }
    }
}
