using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Vision;
using Vision.Detection;

namespace EyeTestApp
{
    public class EyeGazeDetectModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)(EyeGazeDetectMode)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (EyeGazeDetectMode)(int)value;
        }
    }

    public class EyeGazeSmoothModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)(PointSmoother.SmoothMethod)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (PointSmoother.SmoothMethod)(int)value;
        }
    }

    public class IntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32((string)value);
        }
    }

    public class Settings : INotifyPropertyChanged
    {
        public static Settings Current { get; set; }
        static Settings()
        {
            Current = new Settings();
        }

        int camera = 0;
        public int Camera
        {
            get => camera;
            set { camera = value; OnPropertyChanged(); }
        }

        bool faceSmooth = true;
        public bool FaceSmooth
        {
            get => faceSmooth;
            set { faceSmooth = value; OnPropertyChanged(); }
        }

        bool gazeSmooth = true;
        public bool GazeSmooth
        {
            get => gazeSmooth;
            set { gazeSmooth = value; OnPropertyChanged(); }
        }

        int gazeSmoothCount = 6;
        public int GazeSmoothCount
        {
            get => gazeSmoothCount;
            set { gazeSmoothCount = value; OnPropertyChanged(); }
        }

        PointSmoother.SmoothMethod gazeSmoothMode = PointSmoother.SmoothMethod.MeanKalman;
        public PointSmoother.SmoothMethod GazeSmoothMode
        {
            get => gazeSmoothMode;
            set { gazeSmoothMode = value; OnPropertyChanged(); }
        }

        EyeGazeDetectMode gazeMode = EyeGazeDetectMode.FaceMobile;
        public EyeGazeDetectMode GazeMode
        {
            get => gazeMode;
            set { gazeMode = value; OnPropertyChanged(); }
        }

        bool openSmooth = true;
        public bool OpenSmooth
        {
            get => openSmooth;
            set { openSmooth = value; OnPropertyChanged(); }
        }

        bool cursorSmooth = true;
        public bool CursorSmooth
        {
            get => cursorSmooth;
            set { cursorSmooth = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }
}
