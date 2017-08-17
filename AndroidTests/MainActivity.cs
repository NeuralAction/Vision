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

using Vision.Cv;
using Vision.Detection;

namespace AndroidTests
{
    [Activity(Label = "AndroidTests", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        int count = 1;
        int index = 1;

        FaceDetection detection;
        ScreenProperties screen = ScreenProperties.CreatePixelScreen(new Vision.Size(1080, 1920), 375);
        InceptionTests inception;

        FaceDetectorXmlLoader detectorXml;
        FlandmarkModelLoader flandModel;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            Button button = FindViewById<Button>(Resource.Id.MyButton);

            button.Click += delegate 
            {
                button.Text = string.Format("{0} clicks!", count++);

                DisposeDetector();

                index++;
                index %= 2;

                Start();
            };

            ImageView img = FindViewById<ImageView>(Resource.Id.imageView1);

            AndroidCore.Init(this, this, img);

            detectorXml = new FaceDetectorXmlLoader();
            flandModel = new FlandmarkModelLoader();

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

        private void Start()
        {
            detection = new FaceDetection(index, detectorXml, flandModel);
            detection.ScreenProperties = screen;
            detection.DetectGaze = true;
            detection.Detector.SmoothVectors = true;
            detection.Detector.SmoothLandmarks = true;
            detection.Detector.EyesDetectCascade = true;
            detection.Detector.LandmarkDetect = true;
            detection.Detector.LandmarkSolve = true;
            detection.Detector.EyesDetectLandmark = true;
            detection.Start();

            //inception = new InceptionTests(index);
            //inception.Start();
        }
    }
}

