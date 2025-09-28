using System.Timers;

namespace SyncAppVeeam.Classes
{
    public class TimerService
    {
        private TimeSpan _interval;
        private System.Timers.Timer _timer;
        private Task _syncTask = Task.CompletedTask; //<-- temp placeholder

        public TimerService(TimeSpan interval)
        {
            this._interval = interval;
            Reset();
        }

        public void Reset()
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
                _timer = new System.Timers.Timer(_interval);
                _timer.AutoReset = true;
            }
            _timer.Elapsed += OnTimeElapsed;
            _timer.Start();
        }

        public void OnTimeElapsed(Object? source, ElapsedEventArgs e)
        {
            Console.WriteLine("Time has elapsed at {0:HH:mm:ss.fff}", e.SignalTime);
        }
    }
}
