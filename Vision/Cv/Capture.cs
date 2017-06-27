using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision
{
    public abstract class Capture : VirtualObject, IDisposable
    {
        public virtual bool IsOpened
        {
            get
            {
                return Opened();
            }
        }

        public virtual double FPS { get; set; }
        public bool IsRunning { get; protected set; }

        public abstract event EventHandler<FrameArgs> FrameReady;

        public abstract void Dispose();
        public abstract VMat QueryFrame();
        public abstract bool CanQuery();
        public void Start()
        {
            IsRunning = true;
            OnStart();
        }
        protected abstract void OnStart();
        public void Stop()
        {
            IsRunning = false;
            OnStop();
        }
        protected abstract void OnStop();
        public virtual void Join()
        {
            while (IsRunning)
            {
                Core.Sleep(1);
            }
        }

        protected abstract bool Opened();

        public static Capture New(int index)
        {
            return Core.Cv.CreateCapture(index);
        }

        public static Capture New(string filePath)
        {
            return Core.Cv.CreateCapture(filePath);
        }
    }

    public class FrameArgs : EventArgs
    {
        public VMat VMat { get; set; }
        public char LastKey { get; set; }
        public bool Break { get; set; } = false;

        public FrameArgs(VMat mat, char k = (char)0)
        {
            VMat = mat;
            LastKey = k;
        }
    }
}
