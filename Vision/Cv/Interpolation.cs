using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Cv
{
    [Flags]
    public enum Interpolation
    {
        NearestNeighbor = 0,

        Linear = 1,

        Cubic = 2,

        Area = 3,

        Lanczos4 = 4,

        Max = 7,

        FillOutliers = 8,

        InverseMap = 16
    }
}
