using Microsoft.MediaCenter;
using Microsoft.MediaCenter.UI;
using System;
using System.IO;
using System.Net;
using System.Xml;

namespace myForecast
{
    public class WeatherModel : ModelItem
    {
        #region Private Properties

        private readonly string _weatherFileName;
        private readonly string _weatherFileLocation;
        private readonly string _weatherApiAddress;

        private Timer _weatherRefreshTimer;
        private XmlDocument _xmlWeatherData;
        private WeatherUnit _weatherUnit;
        private int _weatherRefreshRateInMinutes;
        private ClockTimeFormat _weatherClockTimeFormat;
        private string _weatherApiKey;
        private string _weatherLocationCode;

        private bool _isLoaded;
        private string _lastUpdateTimestamp;
        private string _locationName;
        private string _currentConditionIcon;
        private string _currentConditionTemperature;
        private string _currentConditionDescription;
        private string _currentConditionFeelsLike;
        private string _currentConditionHumidity;
        private string _currentConditionDewPoint;
        private string _currentConditionWind;
        private string _currentConditionUvIndex;
        private string _currentConditionPressure;
        private ArrayListDataSet _forecast;

        #endregion

        #region Public Properties

        public bool IsLoaded
        {
            get { return _isLoaded; }
            set { _isLoaded = value; FirePropertyChanged("IsLoaded"); }
        }

        public string LastUpdateTimestamp
        {
            get { return _lastUpdateTimestamp; }
            set { _lastUpdateTimestamp = value; FirePropertyChanged("LastUpdateTimestamp"); }
        }

        public string LocationName
        {
            get { return _locationName; }
            set { _locationName = value; FirePropertyChanged("LocationName"); }
        }

        public string CurrentConditionIcon
        {
            get { return _currentConditionIcon; }
            set { _currentConditionIcon = value; FirePropertyChanged("CurrentConditionIcon"); }
        }

        public string CurrentConditionTemperature
        {
            get { return _currentConditionTemperature; }
            set { _currentConditionTemperature = value; FirePropertyChanged("CurrentConditionTemperature"); }
        }

        public string CurrentConditionDescription
        {
            get { return _currentConditionDescription; }
            set { _currentConditionDescription = value; FirePropertyChanged("CurrentConditionDescription"); }
        }

        public string CurrentConditionFeelsLike
        {
            get { return _currentConditionFeelsLike; }
            set { _currentConditionFeelsLike = value; FirePropertyChanged("CurrentConditionFeelsLike"); }
        }

        public string CurrentConditionHumidity
        {
            get { return _currentConditionHumidity; }
            set { _currentConditionHumidity = value; FirePropertyChanged("CurrentConditionHumidity"); }
        }

        public string CurrentConditionDewPoint
        {
            get { return _currentConditionDewPoint; }
            set { _currentConditionDewPoint = value; FirePropertyChanged("CurrentConditionDewPoint"); }
        }

        public string CurrentConditionWind
        {
            get { return _currentConditionWind; }
            set { _currentConditionWind = value; FirePropertyChanged("CurrentConditionWind"); }
        }

        public string CurrentConditionUvIndex
        {
            get { return _currentConditionUvIndex; }
            set { _currentConditionUvIndex = value; FirePropertyChanged("CurrentConditionUvIndex"); }
        }

        public string CurrentConditionPressure
        {
            get { return _currentConditionPressure; }
            set { _currentConditionPressure = value; FirePropertyChanged("CurrentConditionPressure"); }
        }

        public ArrayListDataSet Forecast
        {
            get { return _forecast; }
            set { _forecast = value; FirePropertyChanged("Forecast"); }
        }

        #endregion

        public WeatherModel()
        {
            if (Configuration.Instance.IsValid() == true)
            {
                _weatherApiKey = Configuration.Instance.ApiKey;
                _weatherLocationCode = Configuration.Instance.LocationCode;
                _weatherUnit = Configuration.Instance.WeatherUnit.GetValueOrDefault(WeatherUnit.Imperial);
                _weatherClockTimeFormat = Configuration.Instance.ClockTimeFormat.GetValueOrDefault(ClockTimeFormat.Hours12);
                _weatherRefreshRateInMinutes = Configuration.Instance.RefreshRateInMinutes.GetValueOrDefault(5);
            }

            _weatherFileName = String.Format(Configuration.Instance.WeatherFileNamePattern, _weatherLocationCode);
            _weatherFileName = _weatherFileName.Replace(":", "."); // small cleanup is needed for zmw location codes
            _weatherFileLocation = Path.Combine(Configuration.Instance.ConfigFileFolder, _weatherFileName);
            _weatherApiAddress = String.Format(Configuration.Instance.ApiUrlPattern, _weatherApiKey, _weatherLocationCode);

            _xmlWeatherData = new XmlDocument();
            _forecast = new ArrayListDataSet();
            _isLoaded = false;

            // refresh timer
            if (_weatherRefreshTimer == null)
            {
                _weatherRefreshTimer = new Timer(this);
                _weatherRefreshTimer.Interval = 7000; // 7 seconds interval
                _weatherRefreshTimer.Tick += delegate { LoadWeatherData(); };
            }
        }

        public void LoadWeatherData()
        {
            // if the config is invalid load default weather collection
            if (Configuration.Instance.IsValid() == false)
            {
                LoadDefaultWeatherModel();
                return;
            }

            using (WebClient webClient = new WebClient())
            {
                try
                {
                    if (IsWeatherRefreshRequired() == true)
                    {
                        // download the new weather data in a temporary string
                        // in case there is an error the old weather file will be preserved
                        string weatherDataXml = webClient.DownloadString(_weatherApiAddress);
                        if (String.IsNullOrEmpty(weatherDataXml) == false)
                        {
                            _xmlWeatherData.LoadXml(weatherDataXml);

                            // check for WeatherUnderground specific error
                            XmlNode errorMessageNode = _xmlWeatherData.SelectSingleNode("response/error/description");
                            if (errorMessageNode != null && String.IsNullOrEmpty(errorMessageNode.InnerText) == false)
                            {
                                ShowErrorDialog("Error received from WeatherUnderground: " + errorMessageNode.InnerText, null, true);
                                return;
                            }
                            else
                                File.WriteAllText(_weatherFileLocation, weatherDataXml);
                        }
                        else
                            ShowErrorDialog("No response received from WeatherUnderground.\nPlease, try again in few minutes.");
                    }
                    else
                    {
                        _xmlWeatherData.Load(_weatherFileLocation);
                    }

                    LoadWeatherModel();

                    _weatherRefreshTimer.Enabled = true;
                }
                catch (WebException webException)
                {
                    ShowErrorDialog("Error while connecting to WeatherUnderground.\nPlease, try again in few minutes.", webException);
                }
                catch (Exception exception)
                {
                    ShowErrorDialog("Ooops - catastrophic error.\nPlease, check the log file for more details.", exception);
                }
            }
        }

        private void LoadWeatherModel()
        {
            LastUpdateTimestamp = GetFormattedTimestampFromEpoch(_xmlWeatherData.SelectSingleNode("response/current_observation/observation_epoch").InnerText);
            LocationName = _xmlWeatherData.SelectSingleNode("response/current_observation/display_location/full").InnerText;

            // Current condition support
            LoadCurrentConditionProperties(_xmlWeatherData.SelectSingleNode("response/current_observation"));

            // Forecast support
            LoadForecastProperties(_xmlWeatherData.SelectNodes("response/forecast/simpleforecast/forecastdays/forecastday"));

            IsLoaded = true;
        }

        private void LoadCurrentConditionProperties(XmlNode currentConditionNode)
        {
            // icons.wxug.com/i/c/k/nt_clear.gif
            string icon_url = currentConditionNode.SelectSingleNode("icon_url").InnerText;
            // [0]nt_clear [1].gif
            string[] iconNameParts = icon_url.Substring(icon_url.LastIndexOf("/") + 1).Split('.');
            CurrentConditionIcon = "resx://myForecast/myForecast.Resources/" + iconNameParts[0];
            CurrentConditionDescription = currentConditionNode.SelectSingleNode("weather").InnerText;
            CurrentConditionHumidity = currentConditionNode.SelectSingleNode("relative_humidity").InnerText;

            switch (_weatherUnit)
            {
                case WeatherUnit.Imperial:
                    CurrentConditionTemperature = currentConditionNode.SelectSingleNode("temp_f").InnerText + "F";
                    CurrentConditionFeelsLike = currentConditionNode.SelectSingleNode("feelslike_f").InnerText + "F";
                    CurrentConditionDewPoint = currentConditionNode.SelectSingleNode("dewpoint_f").InnerText + "F";
                    CurrentConditionWind = (currentConditionNode.SelectSingleNode("wind_mph").InnerText == "0.0")
                                            ? currentConditionNode.SelectSingleNode("wind_string").InnerText
                                            : String.Format("{0} mph {1}", currentConditionNode.SelectSingleNode("wind_mph").InnerText, currentConditionNode.SelectSingleNode("wind_dir").InnerText);
                    CurrentConditionPressure = currentConditionNode.SelectSingleNode("pressure_in").InnerText + " in";
                    break;
                default:
                    CurrentConditionTemperature = currentConditionNode.SelectSingleNode("temp_c").InnerText + "°C";
                    CurrentConditionFeelsLike = currentConditionNode.SelectSingleNode("feelslike_c").InnerText + "°C";
                    CurrentConditionDewPoint = currentConditionNode.SelectSingleNode("dewpoint_c").InnerText + "°C";
                    CurrentConditionWind = (currentConditionNode.SelectSingleNode("wind_mph").InnerText == "0.0")
                                            ? currentConditionNode.SelectSingleNode("wind_string").InnerText
                                            : String.Format("{0} kph {1}", currentConditionNode.SelectSingleNode("wind_kph").InnerText, currentConditionNode.SelectSingleNode("wind_dir").InnerText);
                    CurrentConditionPressure = currentConditionNode.SelectSingleNode("pressure_mb").InnerText + " mb";
                    break;
            }

            // UV index format based on http://www.wunderground.com/resources/health/uvindex.asp
            int uvIndex = Int32.Parse(currentConditionNode.SelectSingleNode("UV").InnerText.Replace(".0", ""));
            if (uvIndex <= 2)
                CurrentConditionUvIndex = String.Format("{0} (Very Low)", uvIndex);
            else if (uvIndex >= 3 && uvIndex < 5)
                CurrentConditionUvIndex = String.Format("{0} (Low)", uvIndex);
            else if (uvIndex >= 5 && uvIndex < 7)
                CurrentConditionUvIndex = String.Format("{0} (Moderate)", uvIndex);
            else if (uvIndex >= 7 && uvIndex < 10)
                CurrentConditionUvIndex = String.Format("{0} (High)", uvIndex);
            else if (uvIndex >= 10)
                CurrentConditionUvIndex = String.Format("{0} (Very High)", uvIndex);
        }

        private void LoadForecastProperties(XmlNodeList forecastNodes)
        {
            for (int i = 1; i < forecastNodes.Count; i++) // skipping forecastNode[0] because it is for the current night
            {
                Forecast.Add(new ForecastItem()
                {
                    DayOfTheWeek = String.Format("{0} {1}/{2}",
                                                    forecastNodes[i].SelectSingleNode("date/weekday_short").InnerText.ToUpper(),
                                                    forecastNodes[i].SelectSingleNode("date/month").InnerText,
                                                    forecastNodes[i].SelectSingleNode("date/day").InnerText),
                    ForecastIcon = "resx://myForecast/myForecast.Resources/" + forecastNodes[i].SelectSingleNode("icon").InnerText,
                    Condition = CleanForecastConditionDescription(forecastNodes[i].SelectSingleNode("conditions").InnerText),
                    LowTemp = GetFormattedForecastTemperature(forecastNodes[i].SelectSingleNode("low")),
                    HighTemp = GetFormattedForecastTemperature(forecastNodes[i].SelectSingleNode("high")),
                    PopIcon = GetFormattedPopIconResx(forecastNodes[i]),
                    Pop = GetFormattedPop(forecastNodes[i])
                });
            }
        }

        private void LoadDefaultWeatherModel()
        {
            LastUpdateTimestamp = "N/A";
            LocationName = "N/A";

            // current conditions
            CurrentConditionIcon = "resx://myForecast/myForecast.Resources/unknown";
            CurrentConditionDescription = "N/A";
            CurrentConditionHumidity = "N/A";
            CurrentConditionTemperature = "N/A";
            CurrentConditionFeelsLike = "N/A";
            CurrentConditionDewPoint = "N/A";
            CurrentConditionWind = "N/A";
            CurrentConditionPressure = "N/A";
            CurrentConditionUvIndex = "N/A";

            // six days forecast
            Forecast.Clear();
            for (int i = 1; i < 7; i++)
            {
                Forecast.Add(new ForecastItem()
                {
                    DayOfTheWeek = "N/A",
                    ForecastIcon = "resx://myForecast/myForecast.Resources/unknown",
                    Condition = "N/A",
                    LowTemp = "N/A",
                    HighTemp = "N/A"
                });
            }

            System.Threading.Thread.Sleep(2000);

            IsLoaded = true;
        }

        private string GetFormattedTimestampFromEpoch(string epoch)
        {
            //December 19, 6:05 PM
            DateTime parsedTimestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Double.Parse(epoch)).ToLocalTime();
            string formattedTimestamp;

            switch (_weatherClockTimeFormat)
            {
                case ClockTimeFormat.Hours12:
                    formattedTimestamp = parsedTimestamp.ToString("dddd dd, h:mm tt");
                    break;
                default:
                    formattedTimestamp = parsedTimestamp.ToString("dddd dd, HH:mm");
                    break;
            }

            return formattedTimestamp;
        }

        private string GetFormattedForecastTemperature(XmlNode temperatureNode)
        {
            string temperature;

            switch (_weatherUnit)
            {
                case WeatherUnit.Imperial:
                    temperature = temperatureNode.SelectSingleNode("fahrenheit").InnerText + "F";
                    break;
                default:
                    temperature = temperatureNode.SelectSingleNode("celsius").InnerText + "°C";
                    break;
            }

            return temperature;
        }

        private string GetFormattedPopIconResx(XmlNode forecastNode)
        {
            string popIconResx = "resx://myForecast/myForecast.Resources/";

            if (String.IsNullOrEmpty(GetFormattedPop(forecastNode)) == true)
                popIconResx += "Blank";
            else
            {
                if (forecastNode.SelectSingleNode("snow_allday/in").InnerText == "0.0")
                    popIconResx += "Pop_Rain"; // assume it is raining
                else
                    popIconResx += "Pop_Snow"; // assume it is snowing
            }

            return popIconResx;
        }

        private string GetFormattedPop(XmlNode forecastNode)
        {
            string pop = forecastNode.SelectSingleNode("pop").InnerText;

            if (pop == "0")
                pop = null; // UI will not show null
            else
                pop += "%";

            return pop;
        }

        private string CleanForecastConditionDescription(string conditionDescription)
        {
            // forecast space is tight
            string[] chances = new string[] { "chance of a", "chance of" }; // for similar items the order is from most specific to less specific
            string result = conditionDescription;

            for (var i = 0; i < chances.Length; i++)
            {
                int indexOfValue = conditionDescription.ToLower().IndexOf(chances[i]);
                if (indexOfValue > -1)
                {
                    result = conditionDescription.Substring(indexOfValue + chances[i].Length);
                    break;
                }
            }

            result = result.Trim();

            // fix plural condition word
            switch (result.ToLower())
            {
                case "thunderstorm":
                    result = result + "s";
                    break;
            }

            return result;
        }

        private bool IsWeatherRefreshRequired()
        {
            bool isWeatherRefreshRequired = true;

            if (File.Exists(_weatherFileLocation) == true)
            {
                FileInfo fileInfo = new FileInfo(_weatherFileLocation);
                if (fileInfo.LastWriteTime < DateTime.Now.AddMinutes(-_weatherRefreshRateInMinutes))
                    return true;
                else
                    return false;
            }

            return isWeatherRefreshRequired;
        }

        private void ShowErrorDialog(string message, Exception exception = null, bool goToSettingsPage = false)
        {
            if (exception == null)
                Logger.LogError(message);
            else
                Logger.LogError(exception);

            // only show dialogs when running under Media Center
            if (MyAddIn.Instance != null)
            {
                // need this for error dialogs
                MediaCenterEnvironment mcEnvironment = MyAddIn.Instance.AddInHost.MediaCenterEnvironment;

                System.Collections.Generic.List<String> buttons = new System.Collections.Generic.List<String>();
                buttons.Add("Close"); // normal button
                if (goToSettingsPage == true)
                    buttons.Add("Go to settings"); // only show up if requested

                DialogResult result = mcEnvironment.Dialog(message, "myForecast - Weather Data Refresh", buttons, 60, true, null);
                if (result.ToString() == "101")
                {
                    _weatherRefreshTimer.Enabled = false;
                    MyAddIn.Instance.GoSettingsPage();
                }
                else
                    MyAddIn.Instance.AddInHost.ApplicationContext.CloseApplication();
            }
        }
    }

    public class ForecastItem : ModelItem
    {
        #region Private Properties

        private string _dayOfTheWeek;
        private string _forecastIcon;
        private string _condition;
        private string _lowTemp;
        private string _highTemp;
        private string _pop;
        private string _popIcon;

        #endregion

        public string DayOfTheWeek
        {
            get { return _dayOfTheWeek; }
            set { _dayOfTheWeek = value; FirePropertyChanged("DayOfTheWeek"); }
        }

        public string ForecastIcon
        {
            get { return _forecastIcon; }
            set { _forecastIcon = value; FirePropertyChanged("ForecastIcon"); }
        }

        public string Condition
        {
            get { return _condition; }
            set { _condition = value; FirePropertyChanged("Condition"); }
        }

        public string LowTemp
        {
            get { return _lowTemp; }
            set { _lowTemp = value; FirePropertyChanged("LowTemp"); }
        }

        public string HighTemp
        {
            get { return _highTemp; }
            set { _highTemp = value; FirePropertyChanged("HighTemp"); }
        }

        public string PopIcon
        {
            get { return _popIcon; }
            set { _popIcon = value; FirePropertyChanged("PopIcon"); }
        }

        public string Pop
        {
            get { return _pop; }
            set { _pop = value; FirePropertyChanged("Pop"); }
        }
    }
}