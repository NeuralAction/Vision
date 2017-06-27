using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Vision;
using Vision.Windows;

namespace EyeGazeGen
{
    /// <summary>
    /// ModelViewerControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ModelViewerControl : UserControl
    {
        private EyeGazeModel model;
        public EyeGazeModel Model { get => model; set => model = value; }
        private double zoom = 1;
        public double Zoom { get => zoom; set => zoom = value; }
        private EyesDetector Detector = new EyesDetector(new EyesDetectorXmlLoader());

        public ModelViewerControl()
        {
            InitializeComponent();
        }

        DispatcherTimer resizeDelay;
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(resizeDelay == null)
            {
                resizeDelay = new DispatcherTimer();
                resizeDelay.Interval = TimeSpan.FromMilliseconds(50);
                resizeDelay.Tick += (obj, arg) => 
                {
                    resizeDelay.Stop();
                    Update();
                };
            }
            resizeDelay.Start();
        }

        public void Update()
        {
            canvas.Children.Clear();

            Vision.Point origin = null;
            zoom = 1;
            double canvasRatio = canvas.ActualWidth / canvas.ActualHeight;
            double modelRatio = model.ScreenSize.Width / model.ScreenSize.Height;
            if(modelRatio > canvasRatio)
            {
                //모델 가로를 줄임
                zoom = canvas.ActualWidth / model.ScreenSize.Width;
                origin = new Vision.Point(0, (canvas.ActualHeight - model.ScreenSize.Height * zoom) * 0.5);
            }
            else
            {
                //모델 세로를 줄임
                zoom = canvas.ActualHeight / model.ScreenSize.Height;
                origin = new Vision.Point((canvas.ActualWidth - model.ScreenSize.Width * zoom) * 0.5, 0);
            }

            foreach(EyeGazeModelElement ele in model.Elements)
            {
                AddPoint(new Vision.Point(origin.X + ele.Point.X * zoom, origin.Y + ele.Point.Y * zoom), ele);
            }

            Tb_Header.Text = $"Name:{model.SessionName}\nCount:{model.Elements.Count}\nPath:{model.Directory.AbosolutePath}\nZoom:{zoom.ToString("0.00")}x";
        }

        Brush fill;
        Brush stroke;
        Brush hover;
        private void AddPoint(Vision.Point pt, EyeGazeModelElement ele)
        {
            if(fill == null)
            {
                fill = new SolidColorBrush(Color.FromArgb(10, 255, 255, 255));
                fill.Freeze();
                stroke = new SolidColorBrush(Color.FromArgb(80, 0, 255, 255));
                stroke.Freeze();
                hover = new SolidColorBrush(Color.FromArgb(195, 255, 25, 60));
                hover.Freeze();
            }

            Ellipse el = new Ellipse();
            el.Fill = fill;
            el.Stroke = stroke;
            el.Width = 20;
            el.Height = 20;
            el.Tag = ele;
            el.MouseEnter += (obj, arg) =>
            {
                Ellipse ellipse = (Ellipse)obj;
                ellipse.Fill = hover;

                EyeGazeModelElement element = (EyeGazeModelElement)ellipse.Tag;
                string filepath = element.File.AbosolutePath;
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(filepath);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.CreateOptions = BitmapCreateOptions.DelayCreation;
                img.EndInit();
                Img_Background.Source = img;
                using (VMat mat = Core.Cv.ImgRead(element.File))
                {
                    FaceRect[] rect = Detector.Detect(mat);
                    if (rect.Length > 0 && rect[0].Children.Count > 0)
                    {
                        EyeRect eye = rect[0].LeftEye;
                        if (eye != null)
                        {
                            using(VMat roi = eye.RoiCropByPercent(mat))
                            {
                                roi.Resize(new Vision.Size(160, 160));
                                roi.NormalizeRGB();
                                BitmapSource eyeImg = roi.ToBitmapSource();
                                Img_Eyes.Source = eyeImg;
                            }
                        }
                        else
                        {
                            Logger.Error("no eyes found");
                            Img_Eyes.Source = null;
                        }
                    }
                    else
                    {
                        Logger.Error("no eyes found");
                        Img_Eyes.Source = null;
                    }
                }
                Tb_Info.Text = $"Index:{element.Index} Point:{element.Point}";
            };
            el.MouseLeave += (obj, arg) =>
            {
                Ellipse ellipse = (Ellipse)obj;
                ellipse.Fill = fill;
            };

            el.CacheMode = new BitmapCache();

            canvas.Children.Add(el);

            Canvas.SetTop(el, pt.Y - 10);
            Canvas.SetLeft(el, pt.X - 10);
        }
    }
}
