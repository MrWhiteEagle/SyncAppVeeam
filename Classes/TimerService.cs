using System.Timers;

namespace SyncAppVeeam.Classes
{
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
            //First call on reset in the constructor reset sets the timer object.
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

        // Usually supposed to be used as a manual sync request. - Invoke even then reset timer
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
            _timer.Dispose();
        }
    }
}
