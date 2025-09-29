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
                // Always invoke the first time
                this.TimeIsUp?.Invoke(this, EventArgs.Empty);
            }
            _timer.Elapsed += OnTimeElapsed;
            _timer.Start();
        }

        // Manual request
        public void ForceTick()
        {
            this.TimeIsUp?.Invoke(this, EventArgs.Empty);
            Reset();
        }

        private void OnTimeElapsed(Object? source, ElapsedEventArgs e)
        {
            UserCLIService.CLIPrint($"Running sync, next one scheduled at {DateTime.Now + TimeSpan.FromMilliseconds(interval)}");
            this.TimeIsUp?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Elapsed -= OnTimeElapsed;
            _timer.Dispose();
        }
    }
}
