using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Detection
{
    public class ScreenProperties
    {
        /// <summary>
        /// Origin point of screen in mm (left, top aligned)
        /// </summary>
        public Point3D Origin { get; set; }

        /// <summary>
        /// Size in mm
        /// </summary>
        public Size Size { get; set; }

        /// <summary>
        /// Size in pixel
        /// </summary>
        public Size PixelSize { get; set; }

        public ScreenProperties()
        {

        }

        public ScreenProperties(Point3D trans, Size size, Size pixelSize)
        {
            Origin = trans;
            Size = size;
            PixelSize = pixelSize;
        }

        public Point3D ToCameraCoordinate(double unitPerMM, Point pt)
        {
            return new Point3D((pt.X / PixelSize.Width * Size.Width + Origin.X) * unitPerMM, (pt.Y / PixelSize.Height * Size.Height + Origin.Y) * unitPerMM, Origin.Z * unitPerMM);
        }

        public Point ToScreenCoordinate(double unitPerMM, Point3D pt)
        {
            return new Point((pt.X / unitPerMM - Origin.X) / Size.Width * PixelSize.Width, (pt.Y / unitPerMM - Origin.Y) / Size.Height * PixelSize.Height);
        }
    }
}
