using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Threading;
using System.Diagnostics;
using Android.Util;

using Vision;
using Vision.Cv;
using Vision.Tests;
using Vision.Android;
using Vision.Detection;

namespace AndroidTests
{
    [Activity(Label = "AndroidTests", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        int count = 0;
        int index = 1;

        FaceDetectionTests detection;
        ScreenProperties screen = new ScreenProperties()
        {
            Origin = new Point3D(-40, 0, 0),
            PixelSize = new Vision.Size(1440,2960),
            Size = new Vision.Size(67, 141)
        };
        InceptionTests inception;

        FaceDetectorXmlLoader detectorXml;
        FlandmarkModelLoader flandModel;

        protected override void OnCreate(Bundle bundle)
        {
            RequestWindowFeature(WindowFeatures.NoTitle);
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            ImageView img = FindViewById<ImageView>(Resource.Id.imageView1);
            AndroidCore.Init(this, this, img);

            Button button = FindViewById<Button>(Resource.Id.MyButton);
            button.Click += delegate
            {
                button.Text = $"{count++} clicks!";

                if (count % 2 == 1)
                {
                    Stop();
                    button.Text += "\nStopped";
                }
                else
                {
                    Start();
                    button.Text += "\nStarted";
                }
            };
            Button b1 = FindViewById<Button>(Resource.Id.button1);
            b1.Click += delegate
            {
                OpenCvSharp.Android.NativeBinding.K.SendKey('c');
            };
            Button b2 = FindViewById<Button>(Resource.Id.button2);
            b2.Click += delegate
            {
                OpenCvSharp.Android.NativeBinding.K.SendKey('v');
            };
            Button b3 = FindViewById<Button>(Resource.Id.button3);
            b3.Click += delegate
            {
                OpenCvSharp.Android.NativeBinding.K.SendKey(' ');
            };
            Button b4 = FindViewById<Button>(Resource.Id.button4);
            b4.Click += delegate
            {
                OpenCvSharp.Android.NativeBinding.K.SendKey('g');
            };
            Button b5 = FindViewById<Button>(Resource.Id.button5);
            b5.Click += delegate
            {
                OpenCvSharp.Android.NativeBinding.K.SendKey('j');
            };
            Button b6 = FindViewById<Button>(Resource.Id.button6);
            b6.Click += delegate
            {
                OpenCvSharp.Android.NativeBinding.K.SendKey('b');
            };
            Button b7 = FindViewById<Button>(Resource.Id.button7);
            b7.Click += delegate
            {
                OpenCvSharp.Android.NativeBinding.K.SendKey('`');
            };

            detectorXml = new FaceDetectorXmlLoader();
            flandModel = new FlandmarkModelLoader();
        }

        protected override void OnPause()
        {
            base.OnPause();

            Stop();
        }

        protected override void OnResume()
        {
            base.OnResume();

            Start();
        }

        protected override void OnStop()
        {
            base.OnStop();

            Stop();
        }

        protected override void OnStart()
        {
            base.OnStop();

            Start();
        }

        private void DisposeDetector()
        {
            if (detection != null)
            {
                detection.Dispose();
                detection = null;
            }

            if (inception != null)
            {
                inception.Dispose();
                inception = null;
            }
        }

        private void Stop()
        {
            if(detection != null)
            {
                detection.Stop();
            }
        }

        private void Start()
        {
            if (detection == null)
            {
                detection = new FaceDetectionTests(index, detectorXml, flandModel);
                detection.ScreenProperties = screen;
                detection.GazeDetector.Calibrator.GridWidth = 3;
                detection.GazeDetector.Calibrator.GridHeight = 4;
                detection.GazeDetector.Calibrator.SampleCount = 5;
                detection.GazeDetector.Calibrator.Interval = 1200;
                detection.DetectGaze = true;
            }

            detection.Start();

            //inception = new InceptionTests(index);
            //inception.Start();
        }
    }
}

