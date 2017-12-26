using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Vision;
using Vision.Detection;

namespace EyeTestApp
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        EyeGazeService service;
        bool _started = false;
        bool Started
        {
            get => _started;
            set
            {
                Dispatcher.Invoke(() =>
                {
                    if (value)
                        Bt_Start.Content = "Stop";
                    else
                        Bt_Start.Content = "Start";
                });
                _started = value;
            }
        }
        object StateLocker = new object();

        public MainWindow()
        {
            InitializeComponent();

            var names = Enum.GetNames(typeof(EyeGazeDetectMode));
            for (int i = 0; i < names.Length; i++)
            {
                Cbb_GazeMode.Items.Add(new ComboBoxItem() { Content = Enum.GetName(typeof(EyeGazeDetectMode), (EyeGazeDetectMode)i) });
            }
            InitCombo<EyeGazeDetectMode>(Cbb_GazeMode);
            InitCombo<PointSmoother.SmoothMethod>(Cbb_GazeSmoothMode);
            InitCombo<ClickEyeTarget>(Cbb_OpenEyeTarget);

            names = Enum.GetNames(typeof(PointSmoother.SmoothMethod));
            for (int i = 0; i < names.Length; i++)
            {
                Cbb_GazeSmoothMode.Items.Add(new ComboBoxItem() { Content = Enum.GetName(typeof(PointSmoother.SmoothMethod), (PointSmoother.SmoothMethod)i) });
            }

            Loaded += MainWindow_Loaded;
            
            Settings.Current.PropertyChanged += Current_PropertyChanged;
            DataContext = Settings.Current;
        }

        void InitCombo<T>(System.Windows.Controls.ComboBox cb) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            var names = Enum.GetNames(typeof(T));
            for (int i = 0; i < names.Length; i++)
            {
                cb.Items.Add(new ComboBoxItem() { Content = Enum.GetName(typeof(T), i) });
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PresentationSource source = PresentationSource.FromVisual(this);
            var scrScale = source == null ? 1.0 : source.CompositionTarget.TransformToDevice.M11;
            Settings.Current.PropertyChanged -= Current_PropertyChanged;
            Settings.Current.DPI = 96 * scrScale;
            Settings.Current.PropertyChanged += Current_PropertyChanged;

            UpdateScreen();
        }

        Vision.Detection.ScreenProperties scrProperties;
        Vision.Size scrLogicalSize;
        Vision.Size scrPhysicalSize;
        void UpdateScreen()
        {
            this.Dispatcher.Invoke(() =>
            {
                PresentationSource source = PresentationSource.FromVisual(this);
                var scrScale = source == null ? 1.0 : source.CompositionTarget.TransformToDevice.M11;
                var scr = Screen.GetBounds(new System.Drawing.Point((int)(Left + ActualWidth / 2), (int)(Top + ActualHeight / 2)));
                var width = scr.Width / scrScale;
                var height = scr.Height / scrScale;
                scrLogicalSize = new Vision.Size(width, height);
                scrPhysicalSize = new Vision.Size(scr.Width, scr.Height);
                scrProperties = ScreenProperties.CreatePixelScreen(scrPhysicalSize, Settings.Current.DPI);
                scrProperties.PixelSize = scrLogicalSize;

                Tb_Scr_Pixel_W.Text = scrPhysicalSize.Width.ToString();
                Tb_Scr_Pixel_H.Text = scrPhysicalSize.Height.ToString();
                Tb_Scr_Mm_W.Text = scrProperties.Size.Width.ToString("0.0");
                Tb_Scr_Mm_H.Text = scrProperties.Size.Height.ToString("0.0");

                var mW = (ActualWidth - width) / 2;
                var mH = (ActualHeight - height) / 2;
                Grid_Background.Margin = new Thickness(mW, mH, mW, mH);
            });
        }

        public void LazyStart()
        {
            Task.Factory.StartNew(() =>
            {
                lock (StateLocker)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Bt_Start.IsEnabled = false;
                        Bt_Start.Content = "Waiting Camera";
                    });

                    if (service != null)
                    {
                        service.Dispose();
                        service = null;
                    }

                    var set = Settings.Current;

                    service = new EyeGazeService();

                    UpdateScreen();
                    service.ScreenProperties = scrProperties;

                    service.GazeTracked += Service_GazeTracked;
                    service.Clicked += Service_Clicked;
                    service.Released += Service_Released;

                    service.GazeDetector.ClipToBound = true;
                    service.GazeDetector.UseSmoothing = set.GazeSmooth;
                    service.GazeDetector.DetectMode = set.GazeMode;
                    service.GazeDetector.Smoother.QueueCount = set.GazeSmoothCount;
                    service.GazeDetector.Smoother.Method = set.GazeSmoothMode;
                    service.GazeDetector.Calibrator.Calibarting += Calibrator_Calibarting;
                    service.GazeDetector.Calibrator.Calibrated += Calibrator_Calibrated;

                    service.SmoothOpen = set.OpenSmooth;
                    service.ClickTraget = set.OpenEyeTarget;

                    service.FaceDetector.UseSmooth = set.FaceSmooth;

                    service.Start(set.Camera);

                    Dispatcher.Invoke(() =>
                    {
                        Bt_Start.IsEnabled = true;
                    });

                    Started = true;
                }
            });
        }

        private void Calibrator_Calibrated(object sender, CalibratedArgs e)
        {
            Dispatcher.Invoke(() => { Calib.Visibility = Visibility.Hidden; });
            Task.Factory.StartNew(() =>
            {
                var log = new EyeGazeCalibrationLog(e.Data);
                using (var plt = log.Plot(service.ScreenProperties, service.GazeDetector.Calibrator))
                {
                    Core.Cv.ImgShow("plot", plt);
                    Core.Cv.WaitKey(0);
                    Core.Cv.CloseAllWindows();
                }
            });
        }

        private void Calibrator_Calibarting(object sender, CalibratingArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Calib.Visibility = Visibility.Visible;

                var calib = (EyeGazeCalibrater)sender;
                double duration = 0;
                Color color = Colors.White;
                switch (e.State)
                {
                    case CalibratingState.Point:
                        color = Colors.Lime;
                        duration = calib.Interval;
                        break;
                    case CalibratingState.Wait:
                        color = Colors.Yellow;
                        duration = calib.WaitInterval;
                        break;
                    case CalibratingState.SampleWait:
                        color = Colors.Orange;
                        duration = calib.SampleWaitInterval;
                        break;
                    case CalibratingState.Sample:
                        color = Colors.Red;
                        duration = calib.SampleInterval;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                Calib_Circle.Fill = new SolidColorBrush(color);
                Calib_Circle.Fill.Freeze();

                var storyboard = new Storyboard();
                var aniDur = new Duration(TimeSpan.FromMilliseconds(duration * 0.8));
                var aniX = new DoubleAnimation(Canvas.GetLeft(Calib), e.Data.X, aniDur);
                var aniY = new DoubleAnimation(Canvas.GetTop(Calib), e.Data.Y, aniDur);
                Storyboard.SetTarget(aniX, Calib);
                Storyboard.SetTarget(aniY, Calib);
                Storyboard.SetTargetProperty(aniX, new PropertyPath(Canvas.LeftProperty));
                Storyboard.SetTargetProperty(aniY, new PropertyPath(Canvas.TopProperty));
                storyboard.Children.Add(aniX);
                storyboard.Children.Add(aniY);
                Timeline.SetDesiredFrameRate(storyboard, 30);
                storyboard.Begin();

                Calib_Text.Text = (e.Percent * 100).ToString("0.00") + "%";
            });
        }

        private void Service_Released(object sender, Vision.Point e)
        {
            Dispatcher.Invoke(() =>
            {
                Grid_Cursor.Width = 55;
                Grid_Cursor.Height = 55;
                Cursor_Back.Visibility = Visibility.Hidden;
            });
        }

        private void Service_Clicked(object sender, Vision.Point e)
        {
            Dispatcher.Invoke(() =>
            {
                Grid_Cursor.Width = 100;
                Grid_Cursor.Height = 100;
                Cursor_Back.Visibility = Visibility.Visible;
            });
        }

        public void LazyStop()
        {
            Task.Factory.StartNew(() =>
            {
                lock (StateLocker)
                {
                    if (Started)
                    {
                        Started = false;

                        Dispatcher.Invoke(() =>
                        {
                            Bt_Start.IsEnabled = false;
                            Bt_Start.Content = "Closing Camera";
                        });

                        if (service != null)
                        {
                            service.Dispose();
                            service = null;
                        }

                        Dispatcher.Invoke(() =>
                        {
                            Bt_Start.IsEnabled = true;
                        });
                    }
                }
            });
        }

        private void Service_GazeTracked(object sender, Vision.Point e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e != null)
                {
                    Cursor.Opacity = 1;
                    SetCursorPos(e.X, e.Y);
                }
                else
                {
                    Cursor.Opacity = 0.33;
                }
            });
        }

        #region UI

        double cursorX = 0, cursorY = 0;
        DispatcherTimer cursorUpdater;
        private void SetCursorPos(double x, double y)
        {
            cursorX = x;
            cursorY = y;
            if (Settings.Current.CursorSmooth)
            {
                if (cursorUpdater == null)
                {
                    cursorUpdater = new DispatcherTimer();
                    cursorUpdater.Interval = TimeSpan.FromMilliseconds(30);
                    cursorUpdater.Tick += delegate
                    {
                        var left = Canvas.GetLeft(Cursor);
                        var top = Canvas.GetTop(Cursor);
                        Canvas.SetLeft(Cursor, left + (cursorX - left) / 4);
                        Canvas.SetTop(Cursor, top + (cursorY - top) / 4);
                    };
                }
                cursorUpdater.Start();
            }
            else
            {
                cursorUpdater?.Stop();
                Canvas.SetLeft(Cursor, x);
                Canvas.SetTop(Cursor, y);
            }
        }

        private void Current_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.Camera):
                    break;
                case nameof(Settings.FaceSmooth):
                    if (service != null)
                        service.FaceDetector.UseSmooth = Settings.Current.FaceSmooth;
                    break;
                case nameof(Settings.GazeMode):
                    if (service != null)
                        service.GazeDetector.DetectMode = Settings.Current.GazeMode;
                    break;
                case nameof(Settings.GazeSmooth):
                    if (service != null)
                        service.GazeDetector.UseSmoothing = Settings.Current.GazeSmooth;
                    break;
                case nameof(Settings.GazeSmoothCount):
                    if (service != null)
                        service.GazeDetector.Smoother.QueueCount = Settings.Current.GazeSmoothCount;
                    break;
                case nameof(Settings.GazeSmoothMode):
                    if (service != null)
                        service.GazeDetector.Smoother.Method = Settings.Current.GazeSmoothMode;
                    break;
                case nameof(Settings.OpenSmooth):
                    if (service != null)
                        service.SmoothOpen = Settings.Current.OpenSmooth;
                    break;
                case nameof(Settings.OpenEyeTarget):
                    if (service != null)
                        service.ClickTraget = Settings.Current.OpenEyeTarget;
                    break;
                case nameof(Settings.DPI):
                    UpdateScreen();
                    if (service != null)
                        service.ScreenProperties = scrProperties;
                    break;
                case nameof(Settings.CursorSmooth):
                    break;
                default:
                    Logger.Throw("Uknown Property Name: " + e.PropertyName);
                    break;
            }
        }

        private void Bt_Start_Click(object sender, RoutedEventArgs e)
        {
            if (Started)
                LazyStop();
            else
                LazyStart();
        }

        private void Bt_Calib_Calib_Click(object sender, RoutedEventArgs e)
        {
            if (service != null && !service.GazeDetector.Calibrator.IsStarted)
                service.GazeDetector.Calibrator.Start(service.ScreenProperties);
        }

        private void Bt_Calib_Test_Click(object sender, RoutedEventArgs e)
        {
            if (service != null && !service.GazeDetector.Calibrator.IsStarted)
                service.GazeDetector.Calibrator.Start(service.ScreenProperties, false);
        }

        private void Bt_Calib_Stop_Click(object sender, RoutedEventArgs e)
        {
            if (service != null)
            {
                service.GazeDetector.Calibrator.Stop();
                Calib.Visibility = Visibility.Hidden;
            }
        }

        #endregion UI
    }
}
