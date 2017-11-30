using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Detection
{
    public class EyeGazeInfo
    {
        public static Point3D ToGazeVector(Point3D pt)
        {
            var scale = -1 / pt.Z;
            return new Point3D(pt.X * scale, pt.Y * scale, pt.Z * scale);
        }

        public Point3D Vector { get; set; }
        public Point ScreenPoint { get; set; }
        public bool ClipToBound { get; set; }

        public EyeGazeInfo()
        {
            Vector = new Point3D();
            ScreenPoint = new Point(-1,-1);
        }

        public void UpdateScreenPoint(FaceRect face, ScreenProperties screen)
        {
            ScreenPoint = face.SolveRayScreenVector(Vector, screen);
            if (ClipToBound)
            {
                ScreenPoint.X = Util.Clamp(ScreenPoint.X, 0, screen.PixelSize.Width);
                ScreenPoint.Y = Util.Clamp(ScreenPoint.Y, 0, screen.PixelSize.Height);
            }
        }
    }
}
