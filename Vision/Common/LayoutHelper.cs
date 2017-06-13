using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public enum Stretch
    {
        Stretch = 0,
        None = 1,
        Uniform = 2,
        UniformToFill = 3
    }

    public static class LayoutHelper
    {
        public static Point ResizePoint(Point pt, Size original, Size resized, Stretch stretch)
        {
            switch (stretch)
            {
                case Stretch.Stretch:
                    double zoomX = resized.Width / original.Width;
                    double zoomY = resized.Height / original.Height;
                    return new Point(pt.X * zoomX, pt.Y * zoomY);
                case Stretch.None:
                    return pt;
                case Stretch.Uniform:
                    double originRatio = original.Width / original.Height;
                    double resizedRatio = resized.Width / resized.Height;
                    double zoom;
                    Point origin;
                    if(originRatio < resizedRatio)
                    {
                        //세로를 맞춤
                        zoom = resized.Height / original.Height;
                        origin = new Point((resized.Width - original.Width * zoom) * 0.5, 0);
                    }
                    else
                    {
                        zoom = resized.Width / original.Width;
                        origin = new Point(0, (resized.Height - original.Height * zoom) * 0.5);
                    }
                    return new Point(pt.X * zoom + origin.X, pt.Y * zoom + origin.Y);
                case Stretch.UniformToFill:
                    return pt;
                default:
                    return pt;
            }
        }
    }
}
