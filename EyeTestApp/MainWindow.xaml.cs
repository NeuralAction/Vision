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

            Tb_SesitiveX.Text = EyeGazeDetector.DefaultSensitiveX.ToString();
            Tb_SesitiveY.Text = EyeGazeDetector.DefaultSensitiveY.ToString();
            Tb_OffsetX.Text = EyeGazeDetector.DefaultOffsetX.ToString();
            Tb_OffsetY.Text = EyeGazeDetector.DefaultOffsetY.ToString();

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
                    ((OpenFaceDetector)service.FaceDetector).UseSmooth = faceSmooth;
                    service.GazeTracked += Service_GazeTracked;
                    service.FaceTracked += Service_FaceTracked;
                    service.Clicked += Service_Clicked;
                    service.Released += Service_Released;
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

        private void Service_Released(object sender, Vision.Point e)
        {
            Dispatcher.Invoke(() => 
            {
                Grid_Cursor.Width = 55;
                Grid_Cursor.Height = 55;
            });
        }

        private void Service_Clicked(object sender, Vision.Point e)
        {
            Dispatcher.Invoke(() =>
            {
                Grid_Cursor.Width = 120;
                Grid_Cursor.Height = 120;
            });
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

        }
        
        private void Service_GazeTracked(object sender, Vision.Point e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e != null)
                {
                    Cursor.Opacity = 1;

                    var width = ActualWidth;
                    var height = ActualHeight;

                    var left = e.X / service.ScreenProperties.PixelSize.Width * width;
                    var top = e.Y / service.ScreenProperties.PixelSize.Height * height;

                    Canvas.SetLeft(Cursor, left);
                    Canvas.SetTop(Cursor, top);
                }
                else
                {
                    Cursor.Opacity = 0.33;
                }
            });
        }

        #region UI

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
                ((OpenFaceDetector)service.FaceDetector).UseSmooth = true;
            }
        }

        private void Cb_Head_Smooth_Unchecked(object sender, RoutedEventArgs e)
        {
            if (service != null)
            {
                ((OpenFaceDetector)service.FaceDetector).UseSmooth = false;
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

        private void Tb_SesitiveX_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(service != null)
            {
                try
                {
                    service.GazeDetector.SensitiveX = Convert.ToDouble(Tb_SesitiveX.Text);
                }
                catch
                {

                }
            }
        }

        private void Tb_SesitiveY_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (service != null)
            {
                try
                {
                    service.GazeDetector.SensitiveY = Convert.ToDouble(Tb_SesitiveY.Text);
                }
                catch
                {

                }
            }
        }

        private void Tb_OffsetX_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (service != null)
            {
                try
                {
                    service.GazeDetector.OffsetX = Convert.ToDouble(Tb_OffsetX.Text);
                }
                catch
                {

                }
            }
        }

        private void Tb_OffsetY_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (service != null)
            {
                try
                {
                    service.GazeDetector.OffsetY = Convert.ToDouble(Tb_OffsetY.Text);
                }
                catch
                {

                }
            }
        }

        private void Cb_Model_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(service != null)
                service.GazeDetector.DetectMode = (EyeGazeDetectMode)Cb_Model.SelectedIndex;
        }
        #endregion UI
    }
}
