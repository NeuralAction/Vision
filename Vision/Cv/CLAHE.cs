using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Cv
{
    public abstract class CLAHE : VirtualObject, IDisposable
    {
        public abstract double ClipLimit { get; }
        public abstract Size TileGridSize { get; }

        public abstract void Apply(VMat input, VMat output);
        public void Apply(VMat mat)
        {
            Apply(mat, mat);
        }

        public static CLAHE New(double clip = 3, Size gridSize = null)
        {
            return Core.Cv.CreateCLAHE(clip, (gridSize == null) ? new Size(8, 8) : gridSize);
        }

        public abstract void Dispose();
    }
}
