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
        public abstract void Apply(FaceRect face);
    }

    public class LinearEyeGazeCalibratorEngine : EyeGazeCalibratorEngineBase
    {
        LinearCalculator X = new LinearCalculator();
        LinearCalculator Y = new LinearCalculator();

        public double LearningRate { get; set; } = 0.01;
        public double Epoch { get; set; } = 50;

        public override void Apply(FaceRect face)
        {
            throw new Exception();
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

            var train = TrainData.Values.ToArray();
            var label = TrainData.Keys.ToArray();
            var lr = LearningRate;
            for (int e = 0; e < Epoch; e++)
            {
                for (int i = 0; i < train.Length; i++)
                {
                    int ind1 = Random.R.NextInt(0, train.Length);
                    int ind2 = -1;
                    while (true)
                    {
                        ind2 = Random.R.NextInt(0, train.Length);
                        if (ind1 != ind2)
                            break;
                    }
                    var train1 = train[ind1].Face.GazeInfo.Vector;
                    train1 = EyeGazeInfo.ToGazeVector(train1);
                    var train2 = train[ind2].Face.GazeInfo.Vector;
                    train2 = EyeGazeInfo.ToGazeVector(train2);
                    var label1 = EyeGazeInfo.ToGazeVector(label[ind1]);
                    var label2 = EyeGazeInfo.ToGazeVector(label[ind2]);
                    if (train1.X != train2.X && train1.Y != train2.Y)
                    {
                        var newX = X.Fit(lr, train1.X, label1.X, train2.X, label2.X);
                        var newY = Y.Fit(lr, train1.Y, label1.Y, train2.Y, label2.Y);
                        X.Assign(newX);
                        Y.Assign(newY);
                    }
                }
                lr *= 0.9;
                Logger.Log(this, X.ToString());
                Logger.Log(this, Y.ToString());
                Logger.Log(this, $"Lr:{lr}");
            }

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
                var error = Math.Sqrt(Math.Pow(x - item.Key.X, 2) + Math.Pow(y - item.Key.Y, 2));
                errors.Add(error);
            }
            var errAvg = errors.Average();
            var errMax = errors.Max();

            Logger.Log(this, $"Error avg: {errAvg}");
            Logger.Log(this, $"Error max: {errMax}");
            return errAvg;
        }
    }

    public class LinearCalculator
    {
        public double A = 1;
        public double B = 0; 

        public LinearCalculator()
        {

        }

        public LinearCalculator(double a, double b)
        {
            A = a;
            B = b;
        }

        public double Predict(double x)
        {
            return A * x + B;
        }

        public LinearCalculator Fit(double lr, double x1, double y1, double x2, double y2)
        {
            double newA, newB, retA = 0, retB = 0;
            newA = (y1 - y2) / (x1 - x2);
            newB = y1 - newA * x1;
            retA = A + (newA - A) * lr;
            retB = B + (newB - B) * lr;
            return new LinearCalculator(retA, retB);
        }

        public void Assign(LinearCalculator c)
        {
            A = c.A;
            B = c.B;
        }

        public override string ToString()
        {
            return $"f(x)={A}*x+{B}";
        }
    }
}
