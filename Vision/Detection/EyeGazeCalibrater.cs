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
        Point,
        Wait,
        SampleWait,
        Sample
    }

    public class CalibratingArgs : EventArgs
    {
        public CalibratingState State { get; set; }
        public Point Data { get; set; }

        public CalibratingArgs(CalibratingState s, Point data = null)
        {
            State = s;
            Data = data;
        }
    }

    public class CalibratedArgs : EventArgs
    {
        public Dictionary<Point3D, CalibratingPushData> Data { get; set; }

        public CalibratedArgs(Dictionary<Point3D, CalibratingPushData> data)
        {
            Data = data;
        }
    }

    public class CalibratingPushData
    {
        public FaceRect Face { get; set; }

        public CalibratingPushData(FaceRect face)
        {
            Face = face;
        }
    }

    public class EyeGazeCalibrater
    {
        public double Interval { get; set; } = 1000;
        public double WaitInterval { get; set; } = 300;
        public double SampleWaitInterval { get; set; } = 150;
        public double SampleInterval { get; set; } = 100;

        public int GridHeight { get; set; } = 6;
        public int GridWidth { get; set; } = 10;
        public int SampleCount { get; set; } = 7;

        public bool IsStarted { get; set; } = false;

        public ScreenProperties Screen { get; set; }

        public event EventHandler<CalibratingArgs> Calibarting;
        public event EventHandler CalibrateBegin;
        public event EventHandler<CalibratedArgs> Calibrated;

        Task calibTask;
        CancellationTokenSource tokenSource;
        CalibratingPushData lastData;

        public EyeGazeCalibrater(ScreenProperties screen)
        {
            Screen = screen;
        }

        public Point Apply(Point pt)
        {
            return pt;
        }

        public void Push(CalibratingPushData data)
        {
            if (IsStarted)
            {
                lastData = data;
            }
        }

        public void Start()
        {
            if(Calibarting == null)
            {
                Logger.Throw("Calibrating callback must be used");
            }

            if(IsStarted || (calibTask != null && (!calibTask.IsCanceled && !calibTask.IsCompleted && !calibTask.IsFaulted)))
            {
                Logger.Throw("already started");
            }

            IsStarted = true;
            tokenSource = new CancellationTokenSource();

            calibTask = Task.Factory.StartNew(() =>
            {
                var token = tokenSource.Token;

                if (token.IsCancellationRequested)
                    return;

                var labelResultDict = new Dictionary<Point3D, CalibratingPushData>();
                var calibed = new bool[GridWidth * GridHeight];

                CalibrateBegin.Invoke(this, EventArgs.Empty);

                for (int i = 0; i < calibed.Length; i++)
                {
                    if (token.IsCancellationRequested)
                        return;

                    int targetIndex = 1;
                    while (true)
                    {
                        var ind = Random.R.NextInt(0, calibed.Length);
                        ind = Math.Min(ind, calibed.Length - 1);    
                        if (!calibed[ind])
                        {
                            targetIndex = ind;
                            break;
                        }
                    }

                    double x = (double)(targetIndex % GridWidth) / (GridWidth - 1);
                    x = x * Screen.PixelSize.Width;

                    double y = Math.Floor((double)targetIndex / GridWidth) / (GridHeight - 1);
                    y = y * Screen.PixelSize.Height;

                    var targetPoint = new Point(x, y);

                    Calibarting.Invoke(this, new CalibratingArgs(CalibratingState.Point, targetPoint));
                    Task.Delay((int)Interval).Wait();
                    if (token.IsCancellationRequested)
                        return;

                    Calibarting.Invoke(this, new CalibratingArgs(CalibratingState.Wait, targetPoint));
                    Task.Delay((int)WaitInterval).Wait();
                    if (token.IsCancellationRequested)
                        return;

                    for (int sampling = 0; sampling < SampleCount; sampling++)
                    {
                        Calibarting.Invoke(this, new CalibratingArgs(CalibratingState.SampleWait, targetPoint));
                        Task.Delay((int)SampleWaitInterval).Wait();
                        if (token.IsCancellationRequested)
                            return;

                        if (lastData == null || lastData.Face.GazeInfo == null)
                        {
                            Logger.Error("data is not sented... maybe machine is too slow");
                            while (lastData == null || lastData.Face.GazeInfo == null)
                            {
                                if (token.IsCancellationRequested)
                                    return;
                                Task.Delay(500).Wait();
                                Logger.Error("gaze not captured");
                            }
                        }
                        
                        Calibarting.Invoke(this, new CalibratingArgs(CalibratingState.Sample, targetPoint));
                        Task.Delay((int)SampleInterval).Wait();
                        if (token.IsCancellationRequested)
                            return;

                        var targetVec = lastData.Face.SolveLookScreenVector(targetPoint, Screen);
                        labelResultDict.Add(targetVec, lastData);
                        Logger.Log(this, $"Calibrating {targetPoint} ({i+1}/{calibed.Length}) [{sampling+1}/{SampleCount}] - {targetVec} : {lastData.Face.GazeInfo.Vector}");
                    }

                    calibed[targetIndex] = true;
                }

                Logger.Log(this, "Calibrated");
                Calibrated.Invoke(this, new CalibratedArgs(labelResultDict));
                IsStarted = false;
            });
        }

        public void Stop()
        {
            IsStarted = false;
            if(calibTask != null)
            {
                tokenSource.Cancel(false);

                calibTask.Wait();

                tokenSource.Dispose();

                tokenSource = null;
                calibTask = null;
            }
        }
    }
}
