using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Cv
{
    public enum HaarDetectionType
    {
        Zero = 0,

        DoCannyPruning = 1,

        ScaleImage = 2,

        FindBiggestObject = 4,

        DoRoughSearch = 8
    }
}
