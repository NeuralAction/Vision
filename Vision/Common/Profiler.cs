using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public static class Profiler
    {
        public static bool IsDebug = true;
        public static Stopwatch Stopwatch;
        public static event EventHandler<Dictionary<string, ProfilerData>> Reported;
        public static double ReportWait = 1000;

        static object DataLocker = new object();
        static Dictionary<string, ProfilerData> Data = new Dictionary<string, ProfilerData>();
        static double lastMs = 0;

        static Profiler()
        {
            Stopwatch = new Stopwatch();
            Stopwatch.Start();
        }

        public static void Start(string name, bool showlog = false)
        {
            if (!IsDebug)
                return;

            if (showlog)
                Logger.Log("Logger", $"\"{name}\" Flag is started");

            lock (DataLocker)
            {
                SignKey(name);
                Data[name].Start(GetCurrent());
                Report();
            }
        }

        public static void End(string name, bool showlog = false)
        {
            if (!IsDebug)
                return;

            if (showlog)
                Logger.Log("Logger", $"\"{name}\" Flag is ended");

            lock (DataLocker)
            {
                SignKey(name);
                Data[name].End(GetCurrent());
                Report();
            }
        }

        public static void Capture(string name, double value)
        {
            if (!IsDebug)
                return;
            
            lock (DataLocker)
            {
                SignKey(name);
                Data[name].Capture(value);
                Report();
            }
        }

        public static void Count(string name)
        {
            if (!IsDebug)
                return;

            lock(DataLocker)
            {
                SignKey(name);
                Data[name].Count();
                Report();
            }
        }

        public static double Get(string name)
        {
            lock (DataLocker)
            {
                SignKey(name);
                return Data[name].Average;
            }
        }

        private static double GetCurrent()
        {
            return Stopwatch.Elapsed.Ticks / TimeSpan.TicksPerMillisecond;
        }

        private static void SignKey(string key)
        {
            if (!Data.ContainsKey(key))
            {
                Data.Add(key, new ProfilerData(key));
            }
        }

        static StringBuilder sb = new StringBuilder();
        private static void Report()
        {
            if(GetCurrent() - lastMs > ReportWait)
            {
                lastMs = GetCurrent();

                sb.AppendLine("Profiler Report ==");
                foreach (ProfilerData d in Data.Values)
                {
                    d.Push();
                    d.Clear();
                    sb.AppendLine(d.ToString());
                }

                Logger.Log(sb.ToString());

                sb.Clear();
                
                Reported?.Invoke(null, Data);
            }
        }
    }

    public class ProfilerData
    {
        public string Name;
        public double Average { get; private set; }
        public int CaptureCount = 0;
        public double CaptureSum = 0;

        double startMs = 0;
        bool isStarted = false;

        public ProfilerData(string name)
        {
            Name = name;
        }

        public void Start(double nowMs)
        {
            if (!isStarted)
            {
                startMs = nowMs;
            }
            else
            {

            }
            isStarted = true;
        }

        public void End(double nowMs)
        {
            if (isStarted)
            {
                CaptureSum += nowMs - startMs;
                CaptureCount++;
                isStarted = false;
            }
        }

        public void Capture(double value)
        {
            CaptureSum += value;
            CaptureCount++;
        }
        
        public void Count()
        {
            CaptureSum++;
            CaptureCount = 1;
        }

        public void Push()
        {
            if (CaptureCount != 0)
                Average = CaptureSum / CaptureCount;
            else
                Average = 0;
        }

        public void Clear()
        {
            CaptureCount = 0;
            CaptureSum = 0;
        }

        public override string ToString()
        {
            return string.Format("ProfilerData[{0}] Average: {1}", Name, Average.ToString("0.000"));
        }
    }
}
