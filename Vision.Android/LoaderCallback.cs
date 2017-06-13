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
using OpenCV.Android;

namespace Vision.Android
{
    public class LoaderCallback : Java.Lang.Object, ILoaderCallbackInterface
    {
        public event EventHandler<int> ManagerConnected;

        public void OnManagerConnected(int p0)
        {
            ManagerConnected?.Invoke(this, p0);
        }

        public void OnPackageInstall(int p0, IInstallCallbackInterface p1)
        {

        }
    }
}