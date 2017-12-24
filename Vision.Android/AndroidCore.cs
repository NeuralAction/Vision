using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;
using System.Threading;
using Vision.Detection;

namespace Vision.Android
{
    public class AndroidCore : Core
    {
        public Context Context { get; set; }
        public Activity MainActivity { get; set; }

        private ImageView img;

        public AndroidCore(Context context, Activity activity, ImageView imshowView = null)
        {
            Context = context;
            MainActivity = activity;
            img = imshowView;
        }

        public static void Init(Context context, Activity activity, ImageView imgView = null)
        {
            Core.Init(new AndroidCore(context, activity, imgView));
        }

        public override void Initialize()
        {
            InitLogger(new Logger.WriteMethodDelegate((s) => Log.Info("Vision.Android", s)));
            InitCv(new AndroidCv(Context, MainActivity, img));
            InitStorage(new AndroidStorage());

            TensorFlowSharp.Android.NativeBinding.Init();
            TensorFlowSharp.Android.NativeBinding.PrintFunc = new TensorFlow.NativeBinding.Print((s) => Log.Info("Vision.Android", s));
            Logger.Log("TF Version: " + TensorFlow.TFCore.Version);
        }

        protected override void InternalSleep(int duration)
        {
            Thread.Sleep(duration);
        }

        protected override ScreenProperties InternalGetDefaultScreen()
        {
            return ScreenProperties.CreatePixelScreen(new Size(1080, 1920), 320);
        }
    }
}