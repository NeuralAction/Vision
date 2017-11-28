using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Vision
{
    public class Rect
    {
        internal double _width;
        public double Width
        {
            get
            {
                return _width;
            }
            set
            {
                _width = value;
            }
        }

        internal double _height;
        public double Height
        {
            get
            {
                return _height;
            }
            set
            {
                _height = value;
            }
        }

        internal double _x;
        public double X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
            }
        }

        internal double _y;
        public double Y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = Y;
            }
        }

        public Point Point
        {
            get
            {
                return new Point(_x, _y);
            }
            set
            {
                _x = value.X;
                _y = value.Y;
            }
        }

        public Size Size
        {
            get
            {
                return new Size(_width, _height);
            }
            set
            {
                _width = value.Width;
                _height = value.Height;
            }
        }

        public Point Center
        {
            get
            {
                return new Point(_x + _width * 0.5, _y + _height * 0.5);
            }
        }

        public Rectangle Rectangle
        {
            get { return GetRectangle(); }
            set { SetRectangle(value); }
        }

        public Rect()
        {

        }

        public Rect(double x, double y, double width, double height)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        public Rect(Point origin, Size size)
        {
            _x = origin.X;
            _y = origin.Y;
            _width = size.Width;
            _height = size.Height;
        }

        public Rect(Point leftTop, Point rightBot)
        {
            _x = leftTop.X;
            _y = leftTop.Y;
            _width = rightBot.X - leftTop.X;
            _height = rightBot.Y - leftTop.Y;
        }

        public Rect(Rect rect)
        {
            _x = rect.X;
            _y = rect.Y;
            _width = rect.Width;
            _height = rect.Height;
        }

        public Rect(Rectangle rect)
        {
            SetRectangle(rect);
        }

        public void SetRectangle(Rectangle Rect)
        {
            _x = Rect.X;
            _y = Rect.Y;
            _width = Rect.Width;
            _height = Rect.Height;
        }

        public Rectangle GetRectangle()
        {
            return new Rectangle(_x, _y, _width, _height);
        }

        public OpenCvSharp.Rect ToCvRect()
        {
            return new OpenCvSharp.Rect((int)_x, (int)_y, (int)_width, (int)_height);
        }

        public void Scale(double scaleFactor)
        {
            _x *= scaleFactor;
            _y *= scaleFactor;
            _width *= scaleFactor;
            _height *= scaleFactor;
        }

        public static Rect operator*(double d, Rect r)
        {
            return r * d;
        }

        public static Rect operator*(Rect r, double d)
        {
            return new Rect(r.X * d, r.Y * d, r.Width * d, r.Height * d);
        }

        public static explicit operator OpenCvSharp.Rect(Rect r)
        {
            return r.ToCvRect();
        }
    }
}
