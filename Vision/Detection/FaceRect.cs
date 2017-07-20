using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;
using Xamarin.Forms;

namespace Vision.Detection
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

        public Point[] Landmarks { get; set; }

        public FaceRect(Rect rect) : base(rect.Rectangle)
        {

        }

        public void Add(EyeRect rect)
        {
            Children.Add(rect);
        }

        public void Draw(VMat frame, double thickness = 1, bool drawEyes = false, bool drawLandmarks = false)
        {
            Point center = new Point(X + Width * 0.5, Y + Height * 0.5);

            Size size = new Size(Width * 0.5, Height * 0.5);

            Core.Cv.DrawEllipse(frame, center, size, 0, 0, 360, Scalar.BgrMagenta, thickness, LineType.Link4, 0);

            if (drawEyes && Children != null)
            {
                foreach (EyeRect eye in Children)
                    eye.Draw(frame, thickness);
            }

            if (drawLandmarks && Landmarks != null)
            {
                bool first = true;
                int count = 0;
                foreach (Point pt in Landmarks)
                {
                    Scalar color = Scalar.BgrRed;
                    if (first)
                    {
                        color = Scalar.BgrMagenta;
                        first = false;
                    }
                    count++;
                    Core.Cv.DrawCircle(frame, pt, 2, color, 4);
                    frame.DrawText(pt.X + 5, pt.Y, count.ToString());
                }
            }
        }

        public VMat ROI(VMat frame)
        {
            return VMat.New(frame, this);
        }
    }
}
