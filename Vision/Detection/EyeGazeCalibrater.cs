using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vision.Detection
{
    public enum CalibratingState
    {
        Begin,
        End,
        Point,
        Wait,
        Sample,
    }

    public class CalibratingArgs : EventArgs
    {
        public CalibratingState State { get; set; }

        public CalibratingArgs(CalibratingState s)
        {
            State = s;
        }
    }

    public class EyeGazeCalibrater
    {
        public double Interval { get; set; } = 1000;
        public double WaitInterval { get; set; } = 300;
        public double SampleInterval { get; set; } = 150;

        public int GridHeight { get; set; } = 6;
        public int GridWidth { get; set; } = 10;
        public int SampleCount { get; set; } = 7;

        public event EventHandler<CalibratingArgs> Calibarting;

        Task calibTask;
        CancellationTokenSource tokenSource;

        public EyeGazeCalibrater()
        {

        }

        public Point Apply(Point pt)
        {
            return pt;
        }

        public void Start()
        {
            tokenSource = new CancellationTokenSource();
            calibTask = Task.Factory.StartNew(() =>
            {
                var token = tokenSource.Token;

                if (token.IsCancellationRequested)
                    return;

                Dictionary<Point, Point> labelResultDict = new Dictionary<Point, Point>();
                bool[] calibed = new bool[GridWidth * GridHeight];

                Calibarting.Invoke(this, new CalibratingArgs(CalibratingState.Begin));

                for (int i = 0; i < calibed.Length; i++)
                {
                    int targetIndex = 1;
                    while (true)
                    {
                        var ind = Random.R.NextInt(0, calibed.Length - 1);
                        if (!calibed[ind])
                        {
                            targetIndex = ind;
                            break;
                        }
                    }

                    calibed[targetIndex] = true;
                }

                Calibarting.Invoke(this, new CalibratingArgs(CalibratingState.End));
            });
        }

        public void Stop()
        {
            if(calibTask != null)
            {
                tokenSource.Cancel();
                
                calibTask.Wait();

                tokenSource.Dispose();

                tokenSource = null;
                calibTask = null;
            }
        }
    }
}
