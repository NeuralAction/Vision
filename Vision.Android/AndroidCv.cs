using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Widget;
using App = Android.App;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Vision.Cv;

namespace Vision.Android
{
    public class AndroidCv : Vision.Cv.Cv
    {
        public static AndroidCv Cv { get { return (AndroidCv)Core.Cv; } }

        public Context AppContext { get; set; }
        private ImageView imageView;
        private App.Activity activity;

        public AndroidCv(Context context, App.Activity activity, ImageView imageView = null)
        {
            Logger.WriteMethod = new Logger.WriteMethodDelegate((string s)=> { Log.WriteLine(LogPriority.Debug, "Vision.Android", s); });

            AppContext = context;
            this.imageView = imageView;
            this.activity = activity;

            OpenCvSharp.Android.NativeBinding.Init(context, activity, imageView);
            SharpFace.Android.Native.Init();
            SharpFace.NativeTest.Test();
        }

        public override void CloseWindow(string name)
        {

        }

        public override void CloseAllWindows()
        {

        }

        public void Dispose()
        {

        }

        protected override void InternalImgShow(string name, Mat img)
        {
            Cv2.ImShow(name, img);
        }

        protected override void InternalImgWrite(string name, Mat img, int quality)
        {
            Cv2.ImWrite(name, img, new ImageEncodingParam(ImwriteFlags.JpegQuality, quality));
        }

        protected override Mat InternalImgRead(string path)
        {
            return Cv2.ImRead(path);
        }
    }
}
