using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Vision
{
    public class FaceRect : Rect
    {
        public List<EyeRect> Children { get; set; } = new List<EyeRect>();
        public EyeRect LeftEye
        {
            get
            {
                foreach(EyeRect r in Children)
                {
                    if (r.Center.X < _width * 0.5 && r.Center.Y < _height * 0.45)
                        return r;
                }
                return null;
            }
        }
        public EyeRect RightEye
        {
            get
            {
                foreach (EyeRect r in Children)
                {
                    if (r.Center.X > _width * 0.5 && r.Center.Y < _height * 0.45)
                        return r;
                }
                return null;
            }
        }
        public EyeGazeInfo Info { get; set; }

        public FaceRect(Rect rect) : base(rect.Rectangle)
        {

        }

        public void Add(EyeRect rect)
        {
            Children.Add(rect);
        }

        public void Draw(VMat frame, double thickness = 1, bool drawEyes = false)
        {
            Point center = new Point(X + Width * 0.5, Y + Height * 0.5);

            Size size = new Size(Width * 0.5, Height * 0.5);

            Core.Cv.DrawEllipse(frame, center, size, 0, 0, 360, Scalar.Magenta, thickness, LineType.Link4, 0);

            if (drawEyes && Children != null)
            {
                foreach (EyeRect eye in Children)
                    eye.Draw(frame, thickness);
            }
        }

        public VMat ROI(VMat frame)
        {
            return VMat.New(frame, this);
        }
    }
}
