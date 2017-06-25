using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Vision
{
    public class EyeRect : Rect
    {
        public FaceRect Parent { get; set; }
        public Point AbsoluteCenter
        {
            get
            {
                Point pt = Center;
                return new Point(Center.X + Parent.X, Center.Y + Parent.Y);
            }
        }

        public EyeRect(FaceRect parent, Rect rect) : base(rect)
        {
            Parent = parent;
        }

        public EyeRect(FaceRect parent, Rectangle rect) : base(rect)
        {
            Parent = parent;
        }
        
        public void Draw(VMat frame, double thickness = 1)
        {
            Point center = new Point(Parent.X + X + Width * 0.5, Parent.Y + Y + Height * 0.5);
            double radius = (Width + Height) * 0.25;

            Core.Cv.DrawCircle(frame, center, radius, Scalar.Red, thickness, LineType.Link4, 0);
        }

        public VMat ROI(VMat frame)
        {
            return VMat.New(frame, new Rect(Parent.X + X, Parent.Y + Y, Width, Height));
        }

        public VMat RoiCropByPercent(VMat frame, double percentOfFace = 0.45)
        {
            double size = Math.Max(Parent.Width, Parent.Height) * percentOfFace;
            return VMat.New(frame, new Vision.Rect(Parent.X + Center.X - size * 0.5, Parent.Y + Center.Y - size * 0.5, size, size));
        }
    }
}
