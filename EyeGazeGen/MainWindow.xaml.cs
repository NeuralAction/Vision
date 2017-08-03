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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Vision;
using Vision.Cv;
using Vision.Detection;
using Vision.Windows;

namespace EyeGazeGen
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        EyeGazeModelRecorder recorder;
        FaceDetector detector;
        VMat frame;

        public MainWindow()
        {
            InitializeComponent();

            Vision.Core.Init(new Vision.Windows.WindowsCore());

            detector = new FaceDetector(new FaceDetectorXmlLoader());

            UpdateLib();

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            Profiler.Count("wpfFPS");
        }

        #region modeling

        private void Bt_Start_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void Start()
        {
            Stop();

            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            Topmost = true;
            Topmost = false;
            Panel_StartMenu.Visibility = Visibility.Hidden;
            Panel_StartMenu.IsEnabled = false;
            panel_rec.Visibility = Visibility.Visible;
            panel_rec.IsEnabled = true;
            Bt_Pause.Content = "Pause";

            cv_points.Children.Clear();

            UpdateLayout();

            recorder = new EyeGazeModelRecorder(Tb_Session.Text, new Vision.Size(canvas.ActualWidth, canvas.ActualHeight));
            recorder.SetPoint += Recorder_SetPoint;
            recorder.FrameReady += Recorder_FrameReady;
            recorder.Captured += Recorder_Captured;
            int index = 0;
            try
            {
                index = Convert.ToInt32(Tb_Camera.Text);
            }
            catch
            {
                MessageBox.Show("Camera index is not valid. Use default");
            }
            recorder.Start(index);
        }

        SolidColorBrush white;
        SolidColorBrush stroke;
        private void Recorder_Captured(object sender, Vision.Point e)
        {
            Dispatcher.Invoke(() =>
            {
                Ellipse el = new Ellipse();
                if (white == null)
                {
                    white = new SolidColorBrush(Colors.White);
                    white.Opacity = 0.3;
                    white.Freeze();

                    stroke = new SolidColorBrush(Colors.Cyan);
                    stroke.Freeze();
                }

                el.Fill = white;
                el.Width = 20;
                el.Height = 20;
                el.Stroke = stroke;
                el.StrokeThickness = 1;

                cv_points.Children.Add(el);

                Canvas.SetLeft(el, e.X - 10);
                Canvas.SetTop(el, e.Y - 10);
            });
        }

        int offset = 0;
        private void Recorder_FrameReady(object sender, VMat e)
        {
            if (frame != null)
            {
                frame.Dispose();
                frame = null;
            }

            Profiler.Count("fps");

            if (e != null && !e.IsEmpty)
            {
                frame = e.Clone();
                FaceRect[] rect = detector.Detect(frame);
                foreach (FaceRect r in rect)
                    r.Draw(frame, 1, true);
                frame.DrawText(0, 50, $"fps:{Profiler.Get("fps").ToString().PadRight(2)} wpfFPS:{Profiler.Get("wpfFPS").ToString().PadRight(2)} recorded:{((recorder == null) ? 0 : recorder.CaptureCount)}");
                if (recorder.IsPaused)
                {
                    frame.DrawText(-offset, 50, "PAUSED-PAUSED-PAUSED-PAUSED-PAUSED-PAUSED-PAUSED-PAUSED-PAUSED", Scalar.BgrBlue);
                    offset += 2;
                    offset = offset % (26 * 7);
                }

                Dispatcher.Invoke(() =>
                {
                    var source = frame.ToBitmapSource();
                    Background = new ImageBrush(source)
                    {
                        Stretch = System.Windows.Media.Stretch.Uniform
                    };
                    Background.Freeze();
                });
            }
        }

        private Vision.Point lastpoint = new Vision.Point(0,0);
        private Storyboard storyboard;
        private void Recorder_SetPoint(object sender, EyeGazePointArg e)
        {
            canvas.Dispatcher.Invoke(() => 
            {
                if(storyboard != null)
                {
                    storyboard.Stop();
                    storyboard = null;
                }

                storyboard = new Storyboard();

                Duration d = new Duration(TimeSpan.FromMilliseconds(e.WaitTime * 0.66));
                DoubleAnimation aniX = new DoubleAnimation(lastpoint.X, e.Point.X, d);
                DoubleAnimation aniY = new DoubleAnimation(lastpoint.Y, e.Point.Y, d);

                Storyboard.SetTargetProperty(aniX, new PropertyPath("(Canvas.Left)"));
                Storyboard.SetTargetProperty(aniY, new PropertyPath("(Canvas.Top)"));
                Storyboard.SetTarget(aniX, pointer);
                Storyboard.SetTarget(aniY, pointer);

                storyboard.Children.Add(aniX);
                storyboard.Children.Add(aniY);

                storyboard.Begin();

                lastpoint = e.Point;

                SolidColorBrush brush = new SolidColorBrush(Color.FromArgb((byte)e.Color.Value4, (byte)e.Color.Value1, (byte)e.Color.Value2, (byte)e.Color.Value3));
                brush.Freeze();
                ellipse.Fill = brush;
            });
        }

        private void Stop()
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.ThreeDBorderWindow;
            Topmost = false;
            Panel_StartMenu.Visibility = Visibility.Visible;
            Panel_StartMenu.IsEnabled = true;
            panel_rec.Visibility = Visibility.Hidden;
            panel_rec.IsEnabled = false;

            UpdateLib();

            if (recorder != null)
            {
                recorder.Stop();
                recorder = null;
            }
        }

        private void Bt_Stop_Click(object sender, RoutedEventArgs e)
        {
            if (recorder == null)
                return;

            Stop();
        }

        private void Bt_Pause_Click(object sender, RoutedEventArgs e)
        {
            if (recorder == null)
                return;

            recorder.IsPaused = !recorder.IsPaused;
            Topmost = true;
            Topmost = false;
            if (recorder.IsPaused)
            {
                Bt_Pause.Content = "Resume";
                return;
            }
            Bt_Pause.Content = "Pause";
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    Bt_Pause_Click(null, null);
                    break;
                case Key.Escape:
                    Bt_Stop_Click(null, null);
                    break;
                default:
                    break;
            }
        }

        #endregion modeling

        #region Lib

        List<string> LibItemSource;

        private void Lst_Library_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int selInd = Lst_Library.SelectedIndex;
            if (selInd > -1)
            {
                string path = LibItemSource[selInd];

                EyeGazeModel model = new EyeGazeModel(new DirectoryNode(path));

                ModelViewer viewer = new ModelViewer(this, model);
                viewer.Show();
            }
        }

        private void UpdateLib()
        {
            List<string> libs = new List<string>();
            DirectoryNode[] files = Storage.Root.GetDirectories();
            if (files != null)
            {
                foreach (DirectoryNode node in files)
                {
                    if (EyeGazeModel.IsModel(node))
                    {
                        libs.Add(node.Path);
                    }
                }
            }
            LibItemSource = libs;
            Lst_Library.ItemsSource = LibItemSource;
            Lst_Library.Items.Refresh();
        }

        #endregion Lib
    }
}
