using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Cv
{
    public abstract class UIObject : INotifyPropertyChanged, IDisposable
    {
        private Size size = new Size(320, 100);
        public virtual Size Size
        {
            get => size;
            set { size = value; OnPropertyChanged(); }
        }

        private Point point = new Point(0, 0);
        public virtual Point Point
        {
            get => point;
            set { point = value; OnPropertyChanged(); }
        }

        private Scalar foreground = new Scalar(255, 255, 255, 255);
        public virtual Scalar Foreground
        {
            get => foreground;
            set { foreground = value; OnPropertyChanged(); }
        }

        private Scalar background = new Scalar(0, 0, 0, 100);
        public virtual Scalar Background
        {
            get => background;
            set { background = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        public abstract void Draw(Mat target);

        public virtual void Dispose() { }
    }

    public class LinePlot : UIObject
    {
        public int Length { get; set; } = 100;
        public double Elapse { get; set; } = 100;
        public double Max { get; set; } = 1;
        public double Min { get; set; } = 0;
        public string Name { get; set; }

        Mat backgroundCache;
        Queue<double> valueQueue = new Queue<double>();
        long lastMs = 0;

        public LinePlot()
        {
            UpdateCache();
        }

        public void Step(double value)
        {
            if (Logger.Stopwatch.ElapsedMilliseconds - lastMs > Elapse)
            {
                lastMs = Logger.Stopwatch.ElapsedMilliseconds;
                valueQueue.Enqueue(value);
                if (valueQueue.Count > Length)
                {
                    valueQueue.Dequeue();
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();

        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            UpdateCache();
        }

        private void UpdateCache()
        {
            if (backgroundCache != null)
            {
                backgroundCache.Dispose();
                backgroundCache = null;
            }

            backgroundCache = new Mat(Size.ToCvSize(), MatType.CV_8UC3);
            backgroundCache.DrawRectangle(new Rect(new Point(0, 0), Size), Background, -1);
        }

        public override void Draw(Mat target)
        {
            if (target == null)
                throw new ArgumentNullException();

            Core.Cv.DrawMatAlpha(target, backgroundCache, Point, Background.Value4 / 255.0);
            target.DrawRectangle(new Rect(Point, Size), Foreground, 1);

            Core.Cv.DrawText(target, Name, Point + new Point(5, 20), HersheyFonts.HersheyPlain, 1, Foreground, 1, LineTypes.AntiAlias);
            var text = Max.ToString();
            Core.Cv.DrawText(target, text, Point + new Point(Size.Width - 7 - text.Length * 10, 15), HersheyFonts.HersheyPlain, 0.75, Foreground, 1, LineTypes.AntiAlias);
            Core.Cv.DrawText(target, Min.ToString(), Point + new Point(Size.Width - 7 - text.Length * 10, Size.Height - 5), HersheyFonts.HersheyPlain, 0.75, Foreground, 1, LineTypes.AntiAlias);

            Point pt = new Point(0, Size.Height / 2);
            int index = 0;
            foreach(double val in valueQueue)
            {
                var value = Math.Max(Min, Math.Min(val, Max));
                var pt2 = new Point(index * Size.Width / Length, Size.Height - Size.Height * ((value - Min) / (Max - Min)));
                target.DrawLine(Point + pt, Point + pt2, Foreground, 1, LineTypes.AntiAlias);
                pt = pt2;
                index++;
            }
        }
    }

    public class Point3DLinePlot : UIObject
    {
        public LinePlot PlotX { get; set; } = new LinePlot() { Name = "X" };
        public LinePlot PlotY { get; set; } = new LinePlot() { Name = "Y" };
        public LinePlot PlotZ { get; set; } = new LinePlot() { Name = "Z" };
        public double Elapse
        {
            get => PlotX.Elapse;
            set
            {
                PlotZ.Elapse = PlotY.Elapse = PlotX.Elapse = value;
            }
        }

        public override Point Point
        {
            get => base.Point;
            set { base.Point = value; UpdatePlot(); }
        }

        public override Size Size
        {
            get => base.Size;
            set { base.Size = value; UpdatePlot(); }
        }

        public Point3DLinePlot()
        {
            Point = new Point(0, 0);
            Size = new Size(250, 210);
            Elapse = 50;
        }

        public void Step(Point3D pt)
        {
            PlotX.Step(pt.X);
            PlotY.Step(pt.Y);
            PlotZ.Step(pt.Z);
        }

        public override void Draw(Mat frame)
        {
            PlotX.Draw(frame);
            PlotY.Draw(frame);
            PlotZ.Draw(frame);
        }

        private void UpdatePlot()
        {
            var size = new Size(Size.Width, Size.Height / 3);
            PlotX.Size = PlotY.Size = PlotZ.Size = size;
            PlotX.Point.X = PlotY.Point.X = PlotZ.Point.X = Point.X;
            PlotX.Point.Y = Point.Y;
            PlotY.Point.Y = Point.Y + size.Height + 1;
            PlotZ.Point.Y = Point.Y + size.Height * 2 + 2;
        }
    }
}
