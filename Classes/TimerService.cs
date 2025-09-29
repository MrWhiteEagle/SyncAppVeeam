using System.Timers;

namespace SyncAppVeeam.Classes
{
    /// <summary>
    /// Responsible for measuring time between sync sequences, can be forced by calling ForceTick
    /// </summary>
    public class TimerService : IDisposable
    {
        private double interval;
        private System.Timers.Timer _timer;
        public event EventHandler? TimeIsUp;
        private bool isRunning = false;

        public TimerService(TimeSpan interval)
        {
            this.interval = interval.TotalMilliseconds;
            Reset();
        }

        private void Reset()
        {
            // Safeguard memory leaks, resubscribe to event
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= OnTimeElapsed;
            }
            // First call on reset in the constructor creates the timer object.
            else
            {
                _timer = new System.Timers.Timer(interval);
                _timer.AutoReset = true;
            }
            _timer.Elapsed += OnTimeElapsed;
            _timer.Start();
        }

        // Manual request
        public void ForceTick()
        {
            if (!isRunning)
            {
                this.TimeIsUp?.Invoke(this, EventArgs.Empty);
            }
            Reset();
        }

        private void OnTimeElapsed(Object? source, ElapsedEventArgs e)
        {
            if (!isRunning)
            {
                UserCLIService.CLIPrint($"Running sync, next one scheduled at {DateTime.Now + TimeSpan.FromMilliseconds(interval)}");
                this.TimeIsUp?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                UserCLIService.CLIPrint($"Tried to run sync, but one is already in progress... Skipping...");
            }
        }

        public void Lock()
        {
            this.isRunning = true;
        }

        public void Release()
        {
            this.isRunning = false;
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Elapsed -= OnTimeElapsed;
            _timer.Dispose();
        }
    }
}
