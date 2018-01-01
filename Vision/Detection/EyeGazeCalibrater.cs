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
        public double Percent { get; set; }

        public CalibratingArgs(CalibratingState s, Point data = null, double percent = 0)
        {
            State = s;
            Data = data;
            Percent = percent;
        }
    }

    public class CalibratedArgs : EventArgs
    {
        public Dictionary<Point3D, CalibratingPushData> Data { get; set; }
        public CancellationToken Token { get; set; }

        public CalibratedArgs(Dictionary<Point3D, CalibratingPushData> data, CancellationToken token)
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
        public double Interval { get; set; } = 1500;
        public double WaitInterval { get; set; } = 500;
        public double SampleWaitInterval { get; set; } = 150;
        public double SampleInterval { get; set; } = 100;

        public int GridWidth { get; set; } = 4;
        public int GridHeight { get; set; } = 3;
        public int SampleCount { get; set; } = 5;

        public bool IsStarted { get; set; } = false;
        public bool IsCalibrating { get; set; } = false;

        public EyeGazeCalibratorEngineBase Engine { get; set; }

        public event EventHandler<CalibratingArgs> Calibarting;
        public event EventHandler CalibrateBegin;
        public event EventHandler<CalibratedArgs> Calibrated;

        Task calibTask;
        CancellationTokenSource tokenSource;
        CalibratingPushData lastData;

        public EyeGazeCalibrater()
        {
            Engine = new LinearEyeGazeCalibratorEngine();
        }

        public void Apply(FaceRect face, ScreenProperties screen)
        {
            if (!IsCalibrating)
            {
                Engine.Apply(face, screen);
            }
        }

        public void Push(CalibratingPushData data)
        {
            if (IsStarted)
            {
                lastData = data;
            }
        }

        public void Start(ScreenProperties screen, bool train = true)
        {
            if(IsStarted || (calibTask != null && (!calibTask.IsCanceled && !calibTask.IsCompleted && !calibTask.IsFaulted)))
            {
                Logger.Throw("Already started");
            }

            IsStarted = true;
            IsCalibrating = true;
            tokenSource = new CancellationTokenSource();

            calibTask = Task.Factory.StartNew(() => CalibProc(screen, train));
        }

        private void CalibProc(ScreenProperties screen, bool train)
        {
            var token = tokenSource.Token;

            if (token.IsCancellationRequested)
                return;

            var labelResultDict = new Dictionary<Point3D, CalibratingPushData>();
            var calibed = new bool[GridWidth * GridHeight];

            CalibrateBegin?.Invoke(this, null);

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
                x = x * screen.PixelSize.Width;

                double y = Math.Floor((double)targetIndex / GridWidth) / (GridHeight - 1);
                y = y * screen.PixelSize.Height;

                var targetPoint = new Point(x, y);
                var calibPercent = (double)i / calibed.Length;

                Calibarting.Invoke(this, new CalibratingArgs(CalibratingState.Point, targetPoint, calibPercent));
                Task.Delay((int)Interval).Wait();
                if (token.IsCancellationRequested)
                    return;

                Calibarting.Invoke(this, new CalibratingArgs(CalibratingState.Wait, targetPoint, calibPercent));
                Task.Delay((int)WaitInterval).Wait();
                if (token.IsCancellationRequested)
                    return;

                for (int sampling = 0; sampling < SampleCount; sampling++)
                {
                    var samplePercent = calibPercent + ((double)(sampling + 1) / SampleCount) * (1.0 / calibed.Length);

                    Calibarting.Invoke(this, new CalibratingArgs(CalibratingState.SampleWait, targetPoint, samplePercent));
                    Task.Delay((int)SampleWaitInterval).Wait();
                    if (token.IsCancellationRequested)
                        return;

                    if (lastData == null || lastData.Face.GazeInfo == null)
                    {
                        Logger.Error("Data is not sented... Maybe machine is too slow.");
                        while (lastData == null || lastData.Face.GazeInfo == null)
                        {
                            if (token.IsCancellationRequested)
                                return;
                            Task.Delay(500).Wait();
                            Logger.Error("Gaze is not captured");
                        }
                    }

                    Calibarting.Invoke(this, new CalibratingArgs(CalibratingState.Sample, targetPoint, samplePercent));
                    Task.Delay((int)SampleInterval).Wait();
                    if (token.IsCancellationRequested)
                        return;

                    var targetVec = lastData.Face.SolveLookScreenVector(targetPoint, screen);
                    labelResultDict.Add(targetVec, lastData);
                    Logger.Log(this, $"Calibrating {targetPoint} ({i + 1}/{calibed.Length}) [{sampling + 1}/{SampleCount}] - {targetVec} : {lastData.Face.GazeInfo.Vector}");
                }

                calibed[targetIndex] = true;
            }

            Engine.SetData(labelResultDict);
            if (train)
                Engine.Train();
            IsCalibrating = false;

            Logger.Log(this, "Calibrated");
            Calibrated?.Invoke(this, new CalibratedArgs(labelResultDict, token));
            IsStarted = false;
        }

        public void Stop()
        {
            IsStarted = false;
            IsCalibrating = false;
            if(calibTask != null)
            {
                tokenSource.Cancel(false);

                calibTask.Wait();

                tokenSource.Dispose();

                tokenSource = null;
                calibTask = null;

                Calibrated?.Invoke(this, null);
            }
        }
    }
}
