using ManufacturingERP.Core;
using System;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace ManufacturingERP.Services
{
    public interface ISessionMonitorService
    {
        void StartMonitoring(int timeoutMinutes);
        void StopMonitoring();
        event EventHandler SessionExpired;
    }

    public class SessionMonitorService : ISessionMonitorService
    {
        private DispatcherTimer? _timer;
        private DateTime _lastActivityTime;
        private int _timeoutMinutes;

        public event EventHandler? SessionExpired;

        public void StartMonitoring(int timeoutMinutes)
        {
            _timeoutMinutes = timeoutMinutes;
            _lastActivityTime = DateTime.Now;

            // Hook into all input events
            InputManager.Current.PreProcessInput += OnActivity;

            // Check every 30 seconds
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        public void StopMonitoring()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTimerTick;
            }
            InputManager.Current.PreProcessInput -= OnActivity;
        }

        private void OnActivity(object sender, PreProcessInputEventArgs e)
        {
            // Reset activity time on any input (mouse, keyboard, touch)
            _lastActivityTime = DateTime.Now;
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if ((DateTime.Now - _lastActivityTime).TotalMinutes >= _timeoutMinutes)
            {
                StopMonitoring();
                SessionExpired?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
