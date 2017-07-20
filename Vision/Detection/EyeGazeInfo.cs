using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Detection
{
    public class EyeGazeInfo
    {
        public virtual FaceRect Parent { get; protected set; }

        public virtual Size Size { get; protected set; }

        public virtual Rotation Rotation { get; protected set; }

        public virtual Point ScreenPoint { get; protected set; }

        public virtual EyeRect Eye { get; protected set; }

        public EyeGazeInfo()
        {
            Size = new Size(-1);

            Rotation = new Rotation();

            ScreenPoint = new Point(-1,-1);
        }

        public EyeGazeInfo(Size size, Rotation rot, Point scr, EyeRect rect)
        {
            Size = size;

            Rotation = rot;

            ScreenPoint = scr;

            Eye = rect;
        }

        public EyeGazeInfo (FaceRect face)
        {
            Parent = face;

            Size = new Size(-1);

            Rotation = new Rotation();

            ScreenPoint = new Point(-1, -1);
        }

        public EyeGazeInfo(FaceRect face, Size size, Rotation rot, Point scr, EyeRect rect)
        {
            Parent = face;

            Size = size;

            Rotation = rot;

            ScreenPoint = scr;

            Eye = rect;
        }
    }
}
