using System;
using System.Collections.Concurrent;
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
        public static bool ReportOn = true;
        public static Stopwatch Stopwatch;
        public static event EventHandler<ConcurrentDictionary<string, ProfilerData>> Reported;
        public static double ReportWait = 1000;
        
        static ConcurrentDictionary<string, ProfilerData> Data = new ConcurrentDictionary<string, ProfilerData>();
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
            
            GetOrAdd(name).Start(GetCurrent());
            Report();
        }

        public static void End(string name, bool showlog = false)
        {
            if (!IsDebug)
                return;

            if (showlog)
                Logger.Log("Logger", $"\"{name}\" Flag is ended");

            GetOrAdd(name).End(GetCurrent());
            Report();
        }

        public static void Capture(string name, double value)
        {
            if (!IsDebug)
                return;
           
            GetOrAdd(name).Capture(value);
            Report();
        }

        public static void Count(string name)
        {
            if (!IsDebug)
                return;
            
            GetOrAdd(name).Count();
            Report();
        }

        public static double Get(string name)
        {
            if (!IsDebug)
                return 0;
            
            return GetOrAdd(name).Average;
        }

        private static ProfilerData GetOrAdd(string name)
        {
            return Data.GetOrAdd(name, new ProfilerData(name));
        }

        private static double GetCurrent()
        {
            return Stopwatch.Elapsed.Ticks / TimeSpan.TicksPerMillisecond;
        }

        static StringBuilder sb = new StringBuilder();
        private static void Report()
        {
            if(ReportOn && GetCurrent() - lastMs > ReportWait)
            {
                lastMs = GetCurrent();

                lock (sb)
                {
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
