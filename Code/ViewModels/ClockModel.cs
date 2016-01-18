using Microsoft.MediaCenter.UI;
using System;

namespace myForecast
{
    public class ClockModel : ModelItem
    {
        #region Private Properties

        private string _currentTime;

        private Timer _timer;
        private ClockTimeFormat _weatherClockTimeFormat;

        #endregion

        #region Public Properties

        public string CurrentTime
        {
            get { return _currentTime; }
            set { _currentTime = value; FirePropertyChanged("CurrentTime"); }
        }

        #endregion

        public ClockModel()
        {
            _weatherClockTimeFormat = Configuration.Instance.ClockTimeFormat.GetValueOrDefault(ClockTimeFormat.Hours12);

            //
            // Set up our clock refresh timer. The timer itself
            // is also a ModelItem. This clock is the "owner" of
            // the Timer. So, when this clock is disposed, all
            // its owned objects will be disposed automatically.
            //

            _timer = new Timer(this);
            _timer.Interval = 1000; // 1 second refresh interval
            _timer.Tick += delegate { UpdateTime(); };
            _timer.Enabled = true;

            UpdateTime();
        }

        private void UpdateTime()
        {
            string formattedCurrentTime;

            switch (_weatherClockTimeFormat)
            {
                case ClockTimeFormat.Hours12:
                    formattedCurrentTime = DateTime.Now.ToString("h:mm tt");
                    break;
                default:
                    formattedCurrentTime = DateTime.Now.ToString("HH:mm");
                    break;
            }

            CurrentTime = formattedCurrentTime;
        }
    }
}
