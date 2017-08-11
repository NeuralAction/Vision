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
                    {
                        Bt_Start.Content = "Stop";
                    }
                    else
                    {
                        Bt_Start.Content = "Start";
                    }
                });
                _started = value;
            }
        }
        object StateLocker = new object();

        public MainWindow()
        {
            InitializeComponent();

            WindowState = WindowState.Maximized;
            Topmost = true;
        }

        public void LazyStart(int camera, bool faceSmooth, bool gazeSmooth)
        {
            Task t = new Task(() =>
            {
                lock (StateLocker)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Bt_Start.IsEnabled = false;
                        Bt_Start.Content = "Waiting Camera";
                    });

                    if (Started)
                    {
                        if(service != null)
                        {
                            service.Dispose();
                        }
                    }

                    service = new EyeGazeService();
                    service.GazeDetector.ClipToBound = true;
                    service.GazeDetector.UseSmoothing = gazeSmooth;
                    service.FaceDetector.SmoothLandmarks = faceSmooth;
                    service.FaceDetector.SmoothVectors = faceSmooth;
                    service.GazeTracked += Service_GazeTracked;
                    service.FaceTracked += Service_FaceTracked;
                    service.Start(camera);

                    Dispatcher.Invoke(() =>
                    {
                        Bt_Start.IsEnabled = true;
                    });
                    Started = true;
                }
            });

            t.Start();
        }

        public void LazyStop()
        {
            Task t = new Task(() => 
            {
                lock (StateLocker)
                {
                    if (Started)
                    {
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

                        Started = false;
                    }
                }
            });

            t.Start();
        }

        private void Service_FaceTracked(object sender, FaceRect[] e)
        {
            Dispatcher.Invoke(() =>
            {
                if(e == null || e.Length < 1 || e[0].LeftEye == null)
                {
                    Cursor.Opacity = 0.33;
                }
            });
        }
        
        private void Service_GazeTracked(object sender, Vision.Point e)
        {
            Dispatcher.Invoke(() =>
            {
                Cursor.Opacity = 1;

                var width = ActualWidth;
                var height = ActualHeight;

                var left = e.X / service.ScreenProperties.PixelSize.Width * width;
                var top = e.Y / service.ScreenProperties.PixelSize.Height * height;

                Canvas.SetLeft(Cursor, left);
                Canvas.SetTop(Cursor, top);
            });
        }

        private void Cb_Gaze_Smooth_Checked(object sender, RoutedEventArgs e)
        {
            if(service != null)
            {
                service.GazeDetector.UseSmoothing = true;
            }
        }

        private void Cb_Gaze_Smooth_Unchecked(object sender, RoutedEventArgs e)
        {
            if (service != null)
            {
                service.GazeDetector.UseSmoothing = false;
            }
        }

        private void Cb_Head_Smooth_Checked(object sender, RoutedEventArgs e)
        {
            if (service != null)
            {
                service.FaceDetector.SmoothLandmarks = true;
                service.FaceDetector.SmoothVectors = true;
            }
        }

        private void Cb_Head_Smooth_Unchecked(object sender, RoutedEventArgs e)
        {
            if (service != null)
            {
                service.FaceDetector.SmoothLandmarks = false;
                service.FaceDetector.SmoothVectors = false;
            }
        }

        private void Bt_Start_Click(object sender, RoutedEventArgs e)
        {
            int index = -1;

            try
            {
                index = Convert.ToInt32(Tb_Camera.Text);
            }
            catch
            {
                MessageBox.Show("Invalid Camera Index");
                return;
            }

            if (Started)
            {
                LazyStop();
            }
            else
            {
                LazyStart(index, (bool)Cb_Head_Smooth.IsChecked, (bool)Cb_Gaze_Smooth.IsChecked);
            }
        }
    }
}
