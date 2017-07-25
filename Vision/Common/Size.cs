using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public class Size
    {
        public double Width { get; set; }
        public double Height { get; set; }

        public Size(double all)
        {
            Width = Height = all;
        }

        /// <summary>
        /// Size
        /// </summary>
        /// <param name="width">rows</param>
        /// <param name="height">cols</param>
        public Size(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }
}
