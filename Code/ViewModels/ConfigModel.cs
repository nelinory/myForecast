using Microsoft.MediaCenter.UI;
using System;
using System.Collections.Generic;

namespace myForecast
{
    public class ConfigModel : ModelItem
    {
        private Choice _spinnerWeatherUnits;
        private Choice _spinnerRefreshRateInMinutes;
        private Choice _spinnerClockTimeFormats;

        public string ApiKey
        {
            get { return Configuration.Instance.ApiKey; }
            set { Configuration.Instance.ApiKey = value; FirePropertyChanged("ApiKey"); }
        }

        public string LocationCode
        {
            get { return Configuration.Instance.LocationCode; }
            set { Configuration.Instance.LocationCode = value; FirePropertyChanged("LocationCode"); }
        }

        public Choice WeatherUnit
        {
            get
            {
                int selectedIndex;
                switch (Configuration.Instance.WeatherUnit)
                {
                    case myForecast.WeatherUnit.Metric:
                        selectedIndex = 0;
                        break;
                    default:
                        selectedIndex = 1;
                        break;
                }
                _spinnerWeatherUnits.ChosenIndex = selectedIndex;

                return _spinnerWeatherUnits;
            }
            set
            {
                WeatherUnit selectedValue;
                switch (value.ChosenIndex)
                {
                    case 0:
                        selectedValue = myForecast.WeatherUnit.Metric;
                        break;
                    default:
                        selectedValue = myForecast.WeatherUnit.Imperial;
                        break;
                }
                Configuration.Instance.WeatherUnit = selectedValue;

                FirePropertyChanged("Unit");
            }
        }

        public Choice RefreshRateInMinutes
        {
            get
            {
                int selectedIndex;
                switch (Configuration.Instance.RefreshRateInMinutes.ToString())
                {
                    case "5":
                        selectedIndex = 0;
                        break;
                    case "10":
                        selectedIndex = 1;
                        break;
                    case "20":
                        selectedIndex = 2;
                        break;
                    case "30":
                        selectedIndex = 3;
                        break;
                    case "40":
                        selectedIndex = 4;
                        break;
                    case "50":
                        selectedIndex = 5;
                        break;
                    default:
                        selectedIndex = 6;
                        break;
                }
                _spinnerRefreshRateInMinutes.ChosenIndex = selectedIndex;

                return _spinnerRefreshRateInMinutes;
            }
            set
            {
                int selectedValue;
                switch (value.ChosenIndex)
                {
                    case 0:
                        selectedValue = 5;
                        break;
                    case 1:
                        selectedValue = 10;
                        break;
                    case 2:
                        selectedValue = 20;
                        break;
                    case 3:
                        selectedValue = 30;
                        break;
                    case 4:
                        selectedValue = 40;
                        break;
                    case 5:
                        selectedValue = 50;
                        break;
                    default:
                        selectedValue = 60;
                        break;
                }
                Configuration.Instance.RefreshRateInMinutes = selectedValue;

                FirePropertyChanged("RefreshRateInMinutes");
            }
        }

        public Choice ClockTimeFormat
        {
            get
            {
                int selectedIndex;
                switch (Configuration.Instance.ClockTimeFormat)
                {
                    case myForecast.ClockTimeFormat.Hours12:
                        selectedIndex = 0;
                        break;
                    default:
                        selectedIndex = 1;
                        break;
                }
                _spinnerClockTimeFormats.ChosenIndex = selectedIndex;

                return _spinnerClockTimeFormats;
            }
            set
            {
                ClockTimeFormat selectedValue;
                switch (value.ChosenIndex)
                {
                    case 0:
                        selectedValue = myForecast.ClockTimeFormat.Hours12;
                        break;
                    default:
                        selectedValue = myForecast.ClockTimeFormat.Hours24;
                        break;
                }
                Configuration.Instance.ClockTimeFormat = selectedValue;

                FirePropertyChanged("ClockTimeFormat");
            }
        }

        public ConfigModel()
        {
            // load the spinner items
            if (_spinnerWeatherUnits == null)
            {
                _spinnerWeatherUnits = new Choice();
                List<String> spinnerWeatherUnitsItems = new List<String>();
                spinnerWeatherUnitsItems.Add(myForecast.WeatherUnit.Metric.ToString());
                spinnerWeatherUnitsItems.Add(myForecast.WeatherUnit.Imperial.ToString());

                _spinnerWeatherUnits.Options = spinnerWeatherUnitsItems;
            }

            if (_spinnerRefreshRateInMinutes == null)
            {
                _spinnerRefreshRateInMinutes = new Choice();
                List<String> spinnerRefreshRateInMinutes = new List<String>();
                spinnerRefreshRateInMinutes.Add("5 minutes");
                spinnerRefreshRateInMinutes.Add("10 minutes");
                spinnerRefreshRateInMinutes.Add("20 minutes");
                spinnerRefreshRateInMinutes.Add("30 minutes");
                spinnerRefreshRateInMinutes.Add("40 minutes");
                spinnerRefreshRateInMinutes.Add("50 minutes");
                spinnerRefreshRateInMinutes.Add("60 minutes");

                _spinnerRefreshRateInMinutes.Options = spinnerRefreshRateInMinutes;
            }

            if (_spinnerClockTimeFormats == null)
            {
                _spinnerClockTimeFormats = new Choice();
                List<String> spinnerClockTimeFormats = new List<String>();
                spinnerClockTimeFormats.Add("12 hours");
                spinnerClockTimeFormats.Add("24 hours");

                _spinnerClockTimeFormats.Options = spinnerClockTimeFormats;
            }
        }

        public void Save()
        {
            Configuration.Instance.Save();
        }
    }
}
