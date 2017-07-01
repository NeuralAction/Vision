using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Vision;
using Vision.Tests;
using Vision.Android;
using System.Threading;
using System.Diagnostics;
using Android.Util;

namespace AndroidTests
{
    [Activity(Label = "AndroidTests", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        int count = 1;
        int index = 1;

        FaceDetection detection;
        InceptionTests inception;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            Button button = FindViewById<Button>(Resource.Id.MyButton);

            button.Click += delegate 
            {
                button.Text = string.Format("{0} clicks!", count++);

                if(detection != null)
                {
                    detection.Dispose();
                    detection = null;
                }

                if(inception != null)
                {
                    inception.Dispose();
                    inception = null;
                }

                index++;
                index %= 2;
                //detection = new FaceDetection(index, new EyesDetectorXmlLoader());
                //detection.Start();
                inception = new InceptionTests(index);
                inception.Start();
            };

            ImageView img = FindViewById<ImageView>(Resource.Id.imageView1);

            Core.Init(new AndroidCore(this, this, img));

            //detection = new FaceDetection(index, new EyesDetectorXmlLoader());
            //detection.Start();
            inception = new InceptionTests(index);
            inception.Start();
        }
    }
}

