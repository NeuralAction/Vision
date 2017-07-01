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

namespace Vision.Android
{
    class AndroidCLAHE : CLAHE
    {
        public override double ClipLimit => InnerCLAHE.ClipLimit;

        public override Size TileGridSize
        {
            get
            {
                OpenCV.Core.Size s = InnerCLAHE.TilesGridSize;
                return new Size(s.Width, s.Height);
            }
        }

        public override object Object { get => InnerCLAHE; set => throw new NotImplementedException(); }
        private OpenCV.ImgProc.CLAHE InnerCLAHE;

        public AndroidCLAHE(double clip, Size gridSize)
        {
            InnerCLAHE = OpenCV.ImgProc.Imgproc.CreateCLAHE(clip, new OpenCV.Core.Size(gridSize.Width, gridSize.Height));
        }

        public override void Apply(VMat input, VMat output)
        {
            InnerCLAHE.Apply((OpenCV.Core.Mat)input.Object, (OpenCV.Core.Mat)output.Object);
        }

        public override void Dispose()
        {
            if(InnerCLAHE != null)
            {
                InnerCLAHE.Dispose();
                InnerCLAHE = null;
            }
        }
    }
}