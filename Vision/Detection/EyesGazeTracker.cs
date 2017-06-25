using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public abstract class EyesGazeTracker : IDisposable
    {
        public abstract event EventHandler<GazeTrackArgs> GazeTracked;

        public virtual bool IsStarted { get; protected set; } = false;
        public virtual ScreenInfo ScreenInfo { get; protected set; }

        public abstract void Start();
        public abstract void Stop();
        public abstract void Dispose();
    }

    public class ScreenInfo
    {

    }

    public class GazeTrackArgs : EventArgs
    {
        public List<EyeGazeInfo> Gazes { get; set; }

        public GazeTrackArgs(List<EyeGazeInfo> gazes)
        {
            Gazes = gazes;
        }
    }

    public class EyeGazeInfo
    {
        public virtual EyesGazeTracker Parent { get; protected set; }

        public virtual Size Size { get; protected set; }

        public virtual Rotation Rotation { get; protected set; }

        public virtual Point ScreenPoint { get; protected set; }

        public virtual EyeRect Eye { get; protected set; }

        public EyeGazeInfo()
        {
            Size = new Size(-1);

            Rotation = new Rotation();

            ScreenPoint = new Point(-1,-1);
        }

        public EyeGazeInfo(Size size, Rotation rot, Point scr, EyeRect rect)
        {
            Size = size;

            Rotation = rot;

            ScreenPoint = scr;

            Eye = rect;
        }

        public EyeGazeInfo (EyesGazeTracker tracker)
        {
            Parent = tracker;

            Size = new Size(-1);

            Rotation = new Rotation();

            ScreenPoint = new Point(-1, -1);
        }

        public EyeGazeInfo(EyesGazeTracker tracker, Size size, Rotation rot, Point scr, EyeRect rect)
        {
            Parent = tracker;

            Size = size;

            Rotation = rot;

            ScreenPoint = scr;

            Eye = rect;
        }
    }

    public class VirtualEyesGazeTracker : EyesGazeTracker
    {
        public override event EventHandler<GazeTrackArgs> GazeTracked;

        public VirtualEyesGazeTracker()
        {
            IsStarted = true;
        }

        public override void Dispose()
        {

        }

        public override void Start()
        {
            IsStarted = true;
        }

        public override void Stop()
        {
            IsStarted = false;
        }

        public void MakeTrack(List<EyeGazeInfo> info)
        {
            GazeTracked?.Invoke(this, new GazeTrackArgs(info));
        }
    }
}
