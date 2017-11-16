using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public class Size
    {
        public double Width;
        public double Height;

        public Point Center => new Point(Width / 2, Height / 2);

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

        public Size(Point leftop, Point rightbot)
        {
            Width = rightbot.X - leftop.X;
            Height = rightbot.Y - leftop.Y;
        }

        public static Size operator +(Size s, double d)
        {
            return new Size(s.Width + d, s.Height + d);
        }

        public OpenCvSharp.Size ToCvSize()
        {
            return new OpenCvSharp.Size((int)Width, (int)Height);
        }
    }
}
