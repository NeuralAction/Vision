using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vision.Cv;
using Vision.Detection;

namespace Vision
{
    public abstract class Core
    {
        public static string ProjectInfromation = "Vision Project - Computer Vision via AI - (c) 2017-2018";
        public static string VersionInfromation = "0.0.3 dev";
        public static Vision.Cv.Cv Cv { get { return Vision.Cv.Cv.Context; } }
        public static Core Current { get; private set; }

        public static void Init(Core core)
        {
            Current = core;

            core.Initialize();

            Logger.Log("Core", $"Environment: Core: {Environment.ProcessorCount}");
            Logger.Log("Core", $"TensorFlow Version: {TensorFlow.TFCore.Version}");
        }

        protected abstract void InternalSleep(int duration);
        public static void Sleep(int duration)
        {
            Current.InternalSleep(duration);
        }

        protected abstract ScreenProperties InternalGetDefaultScreen();
        public static ScreenProperties GetDefaultScreen()
        {
            return Current.InternalGetDefaultScreen();
        }

        public abstract void Initialize();

        protected void InitLogger(Logger.WriteMethodDelegate WriteMethod)
        {
            Logger.WriteMethod = WriteMethod;
        }

        protected void InitCv(Vision.Cv.Cv cv)
        {
            Vision.Cv.Cv.Init(cv);
        }

        protected void InitStorage(Storage storage)
        {
            Storage.Init(storage);
        }
    }
}
