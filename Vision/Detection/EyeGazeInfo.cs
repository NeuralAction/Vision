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

        public virtual Point3D Vector { get; set; }

        public virtual Point ScreenPoint { get; set; }

        public EyeGazeInfo()
        {
            Vector = new Point3D();

            ScreenPoint = new Point(-1,-1);
        }
    }
}
