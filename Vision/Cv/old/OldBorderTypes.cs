using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Cv
{
    /// <summary>
    /// Type of the border to create around the copied source image rectangle
    /// </summary>
    public enum OldBorderTypes
    {
        /// <summary>
        /// Border is filled with the fixed value, passed as last parameter of the function.
        /// `iiiiii|abcdefgh|iiiiiii` with some specified `i`
        /// </summary>
        Constant = 0,
        
        /// <summary>
        /// The pixels from the top and bottom rows, the left-most and right-most columns 
        /// are replicated to fill the border. `aaaaaa|abcdefgh|hhhhhhh`
        /// </summary>
        Replicate = 1,
        
        /// <summary>
        /// `fedcba|abcdefgh|hgfedcb`
        /// </summary>
        Reflect = 2,

        /// <summary>
        /// `cdefgh|abcdefgh|abcdefg`
        /// </summary>
        Wrap = 3,

        /// <summary>
        /// `gfedcb|abcdefgh|gfedcba`
        /// </summary>
        Reflect101 = 4,

        /// <summary>
        /// same as BORDER_REFLECT_101
        /// </summary>
        Default = 4,

        /// <summary>
        /// `uvwxyz|absdefgh|ijklmno`
        /// </summary>
        Transparent = 5,

        /// <summary>
        /// do not look outside of ROI
        /// </summary>
        Isolated = 16
    }
}
