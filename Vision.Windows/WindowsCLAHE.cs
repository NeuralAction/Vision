using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;

namespace Vision.Windows
{
    public class WindowsCLAHE : CLAHE
    {
        public override double ClipLimit => throw new NotImplementedException();
        public override Size TileGridSize => throw new NotImplementedException();

        public override object Object { get => InnerCLAHE; set => throw new NotSupportedException(); }

        OpenCvSharp.CLAHE InnerCLAHE;

        public WindowsCLAHE(double clip, Size gridSize)
        {
            InnerCLAHE = OpenCvSharp.Cv2.CreateCLAHE(clip, new OpenCvSharp.Size(gridSize.Width, gridSize.Height));
        }
        
        public override void Apply(VMat input, VMat output)
        {
            InnerCLAHE.Apply((OpenCvSharp.Mat)input.Object, (OpenCvSharp.Mat)output.Object);
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
