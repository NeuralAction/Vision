using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public static class Logger
    {
        public delegate void WriteMethodDelegate(string text);

        public static WriteMethodDelegate WriteMethod;
        public static Stopwatch Stopwatch;
        public static string TimeStamp
        {
            get { return string.Format("[{0}]", Stopwatch.ElapsedMilliseconds); }
        }
        static Logger()
        {
            Stopwatch = new Stopwatch();
            Stopwatch.Start();
        }

        public static void Log(string message)
        {
            WriteLine(string.Format("{0}[LOG] {1}", TimeStamp, message));
        }

        public static void Log(object sender, string message)
        {
            Log(sender.ToString() + " - " + message);
        }

        public static void Error(string message)
        {
            WriteLine(string.Format("{0}[ERR] {1}", TimeStamp, message));
        }

        public static void Error(object sender, string message)
        {
            Error(sender.ToString() + " - " + message);
        }

        public static void Error(object sender, Exception ex)
        {
            Error(sender, ex.ToString());
        }

        private static void WriteLine(string str)
        {
            WriteMethod(str + "\n");
        }
    }
}
