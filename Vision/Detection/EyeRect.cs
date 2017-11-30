using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vision.Cv;
using Xamarin.Forms;

namespace Vision.Detection
{
    public class EyeRect : Rect
    {
        public FaceRect Parent { get; set; }
        public Point AbsoluteCenter
        {
            get
            {
                Point pt = Center;
                return new Point(pt.X + Parent.X, pt.Y + Parent.Y);
            }
        }

        public Point AbsolutePoint
        {
            get
            {
                return new Point(X + Parent.X, Y + Parent.Y);
            }
        }

        public EyeRect Absolute
        {
            get
            {
                EyeRect ret = Clone();
                var point = AbsolutePoint;
                ret.X = point.X;
                ret.Y = point.Y;
                return ret;
            }
        }

        public EyeOpenData OpenData { get; set; }

        public EyeRect(FaceRect parent, Rect rect) : base(rect)
        {
            Parent = parent;
        }

        public EyeRect(FaceRect parent, Rectangle rect) : base(rect)
        {
            Parent = parent;
        }
        
        public void Draw(Mat frame, double thickness = 1)
        {
            Point center = new Point(Parent.X + X + Width * 0.5, Parent.Y + Y + Height * 0.5);
            double radius = (Width + Height) * 0.25;

            Core.Cv.DrawCircle(frame, center, radius, Scalar.BgrRed, thickness, LineTypes.Link4, 0);
            Core.Cv.DrawCircle(frame, center, 2, Scalar.BgrYellow, 4, LineTypes.Link4, 0);

            if(OpenData != null)
            {
                string text;
                Scalar color;
                if (OpenData.IsOpen)
                {
                    text = $"Open ({(OpenData.Percent*100).ToString("0.0")}%)";
                    color = Scalar.BgrRed;
                }
                else
                {
                    text = $"Close ({(OpenData.Percent * 100).ToString("0.0")}%)";
                    color = Scalar.BgrBlue;
                }
                frame.DrawText(Point.X + Parent.X, Point.Y + Parent.Y - 20, text, color);
            }
        }

        public Mat ROI(Mat frame)
        {
            return MatTool.New(frame, new Rect(Parent.X + X, Parent.Y + Y, Width, Height), true);
        }

        public Mat RoiCropByPercent(Mat frame, double percentOfFace = 0.33)
        {
            double size = Math.Max(Parent.Width, Parent.Height) * percentOfFace;
            return MatTool.New(frame, new Vision.Rect(Parent.X + Center.X - size * 0.5, Parent.Y + Center.Y - size * 0.5, size, size), true);
        }

        public EyeRect Clone()
        {
            return new EyeRect(Parent, this)
            {
                OpenData = OpenData?.Clone()
            };
        }
    }
}
