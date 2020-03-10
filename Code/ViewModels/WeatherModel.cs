using Microsoft.MediaCenter;
using Microsoft.MediaCenter.UI;
using myForecast.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace myForecast
{
    public class WeatherModel : ModelItem
    {
        #region Private Properties

        private readonly string _weatherFileName;
        private readonly string _weatherFileLocation;
        private readonly string _weatherApiUri;

        private bool _uiRefreshNeeded;
        private Timer _weatherRefreshTimer;
        private WeatherData _weatherData;
        private XmlDocument _xmlWeatherData;
        private WeatherUnit _weatherUnit;
        private int _weatherRefreshRateInMinutes;
        private ClockTimeFormat _weatherClockTimeFormat;
        private string _weatherApiKey;
        private string _weatherLocationCode;
        private Language _weatherLanguage;

        private bool _isLoaded;
        private string _lastUpdateTimestamp;
        private string _locationName;
        private string _weatherAlertCaption;
        private string _weatherAlertText;
        private string _currentConditionIcon;
        private string _currentConditionTemperature;
        private string _currentConditionDescription;
        private string _currentConditionFeelsLike;
        private string _currentConditionHumidity;
        private string _currentConditionDewPoint;
        private string _currentConditionWind;
        private string _currentConditionUvIndex;
        private string _currentConditionPressure;
        private ArrayListDataSet _dailyForecast;
        private ArrayListDataSet _hourlyForecast;

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

        public string WeatherAlertCaption
        {
            get { return _weatherAlertCaption; }
            set { _weatherAlertCaption = value; FirePropertyChanged("WeatherAlertCaption"); }
        }

        public string WeatherAlertText
        {
            get { return _weatherAlertText; }
            set { _weatherAlertText = value; FirePropertyChanged("WeatherAlertText"); }
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

        public ArrayListDataSet DailyForecast
        {
            get { return _dailyForecast; }
            set { _dailyForecast = value; FirePropertyChanged("DailyForecast"); }
        }

        public ArrayListDataSet HourlyForecast
        {
            get { return _hourlyForecast; }
            set { _hourlyForecast = value; FirePropertyChanged("HourlyForecast"); }
        }

        #endregion

        public WeatherModel()
        {
            _weatherApiKey = Configuration.Instance.ApiKey;
            _weatherLocationCode = Configuration.Instance.LocationCode;
            _weatherUnit = Configuration.Instance.WeatherUnit.GetValueOrDefault(WeatherUnit.Imperial);
            _weatherClockTimeFormat = Configuration.Instance.ClockTimeFormat.GetValueOrDefault(ClockTimeFormat.Hours12);
            _weatherRefreshRateInMinutes = Configuration.Instance.RefreshRateInMinutes.GetValueOrDefault(10);
            _weatherLanguage = Configuration.Instance.Language.GetValueOrDefault(Language.en);

            // set the correct language for the UI thread
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Enum.GetName(typeof(Language), Configuration.Instance.Language));

            _weatherFileName = String.Format(Configuration.Instance.WeatherFileNamePattern, _weatherLocationCode.Replace(".", "_").Replace(",", ""), _weatherLanguage, _weatherUnit == WeatherUnit.Imperial ? "us" : "ca");
            _weatherFileLocation = Path.Combine(Configuration.Instance.ConfigFileFolder, _weatherFileName);
            _weatherApiUri = String.Format(Configuration.Instance.ApiUrlPattern, _weatherApiKey, _weatherLocationCode, _weatherLanguage, _weatherUnit == WeatherUnit.Imperial ? "us" : "ca");

            _xmlWeatherData = new XmlDocument();
            _dailyForecast = new ArrayListDataSet();
            _hourlyForecast = new ArrayListDataSet();

            _uiRefreshNeeded = true;
            _isLoaded = false;

            // refresh timer
            if (_weatherRefreshTimer == null)
            {
                _weatherRefreshTimer = new Timer(this);
                _weatherRefreshTimer.Interval = 10000; // 10 seconds interval
                _weatherRefreshTimer.Tick += delegate { LoadWeatherData(); };
            }
        }

        public void LoadWeatherData()
        {
            using (WebClientWithCompression webClient = new WebClientWithCompression())
            {
                try
                {
                    if (IsWeatherRefreshRequired() == true)
                    {
                        // ensure correct security protocol is allowed
                        ServicePointManager.Expect100Continue = true;
                        //ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc0 | 0x300 | 0xc00); // Tls, Tls11, Tls12
                        ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc00); // Tls12

                        // download the new weather data in a temporary string
                        // in case there is an error the old weather file will be preserved
                        webClient.Encoding = Encoding.UTF8;
                        string weatherDataJson = webClient.DownloadString(_weatherApiUri);
                        if (String.IsNullOrEmpty(weatherDataJson) == false)
                        {
                            _weatherData = new WeatherData(weatherDataJson);

                            // check for WeatherProvider specific error
                            if (_weatherData.IsWeatherInfoAvailable == false)
                            {
                                ShowErrorDialog(String.Format("{0}: {1}", LanguageStrings.ui_DialogErrorReceivedFromWeatherProvider, "Weather station is down for maintenance."), null);
                                return;
                            }
                            else
                            {
                                File.WriteAllText(_weatherFileLocation, weatherDataJson, Encoding.UTF8);
                                _uiRefreshNeeded = true;
                            }
                        }
                        else
                            ShowErrorDialog(LanguageStrings.ui_DialogNoResponseReceivedFromWeatherProvider);
                    }
                    else
                        _weatherData = new WeatherData(File.ReadAllText(_weatherFileLocation));

                    if (_uiRefreshNeeded == true)
                    {
                        _uiRefreshNeeded = false;
                        LoadWeatherModel();
                    }

                    _weatherRefreshTimer.Enabled = true;
                }
                catch (WebException webException)
                {
                    if (webException.Response != null)
                    {
                        HttpWebResponse response = (HttpWebResponse)webException.Response;
                        if (response.StatusCode == HttpStatusCode.Forbidden)
                            ShowErrorDialog(LanguageStrings.ui_DialogInvalidApiKeyReceivedFromWeatherProvider, webException, true);
                        else if (response.StatusCode == HttpStatusCode.BadRequest)
                            ShowErrorDialog(LanguageStrings.ui_DialogInvalidLocationDataReceivedFromWeatherProvider, webException, true);
                        else
                            ShowErrorDialog(LanguageStrings.ui_DialogErrorWhileConnectingToWeatherProvider, webException);
                    }
                    else
                        ShowErrorDialog(LanguageStrings.ui_DialogCatastrophicError, webException);
                }
                catch (Exception exception)
                {
                    ShowErrorDialog(LanguageStrings.ui_DialogCatastrophicError, exception);
                }
            }
        }

        private void LoadWeatherModel()
        {
            LastUpdateTimestamp = GetFormattedTimestampFromEpoch(_weatherData.CurrentForecast.TimestampEpoch);
            LocationName = Configuration.Instance.LocationName;

            LoadWeatherAlertProperties(_weatherData.Alerts);
            LoadCurrentConditionProperties(_weatherData.CurrentForecast);
            LoadDailyForecastProperties(_weatherData.HourlyForecast, _weatherData.DailyForecast);
            LoadHourlyForecastProperties(_weatherData.HourlyForecast);

            IsLoaded = true;
        }

        private void LoadWeatherAlertProperties(List<WeatherData.AlertItem> alertItems)
        {
            string alertCaption = null;
            StringBuilder alertText = new StringBuilder();

            if (alertItems != null && alertItems.Count > 0)
            {
                // caption
                alertCaption = String.Format("*** {0} ***", alertItems[0].Caption);
                if (alertItems.Count > 1)
                    alertCaption = String.Format("{0} [+{1}]", alertCaption, alertItems.Count - 1);

                // text
                foreach (WeatherData.AlertItem alertItem in alertItems)
                {
                    alertText.AppendLine(String.Format("*** {0} ***", alertItem.Caption).Trim());
                    alertText.AppendLine(String.Format("{0}: {1}", LanguageStrings.ui_WeatherAlertStartDate, GetFormattedTimestampFromEpoch(alertItem.StartDateTime)).Trim());
                    alertText.AppendLine(String.Format("{0}: {1}", LanguageStrings.ui_WeatherAlertExpireDate, GetFormattedTimestampFromEpoch(alertItem.ExpireDateTime)).Trim());
                    alertText.AppendLine(alertItem.Description.Trim());
                }
            }

            if (alertText.Length == 0)
                alertText.AppendLine(LanguageStrings.ui_WeatherAlertInfoNotAvailable);

            WeatherAlertCaption = alertCaption;
            WeatherAlertText = alertText.ToString();
        }

        private void LoadCurrentConditionProperties(WeatherData.CurrentItem currentCondition)
        {
            CurrentConditionIcon = GetFormattedIconResx(currentCondition.Icon, currentCondition.TimestampEpoch);
            CurrentConditionDescription = currentCondition.Description;
            CurrentConditionHumidity = GetFormattedWeatherValue(currentCondition.Humidity, WeatherValueFormatType.Humidity);
            CurrentConditionTemperature = GetFormattedWeatherValue(currentCondition.Temperature, WeatherValueFormatType.Temperature);
            CurrentConditionFeelsLike = GetFormattedWeatherValue(currentCondition.FeelsLike, WeatherValueFormatType.Temperature);
            CurrentConditionDewPoint = GetFormattedWeatherValue(currentCondition.DewPoint, WeatherValueFormatType.Temperature);
            CurrentConditionWind = String.Format("{0} {1}",
                                                GetFormattedWeatherValue(currentCondition.WindSpeed, WeatherValueFormatType.WindSpeed),
                                                GetFormattedWeatherValue(currentCondition.WindDirection, WeatherValueFormatType.WindDirection));
            CurrentConditionUvIndex = GetFormattedWeatherValue(currentCondition.UvIndex, WeatherValueFormatType.UvIndex);
            CurrentConditionPressure = GetFormattedWeatherValue(currentCondition.Pressure, WeatherValueFormatType.Pressure);
        }

        private void LoadDailyForecastProperties(List<WeatherData.ForecastItem> hourlyForecastItems, List<WeatherData.ForecastItem> dailyForecastItems)
        {
            DailyForecast.Clear();

            // forecast for the next 2 hours
            WeatherData.ForecastItem currentForecastItem = hourlyForecastItems[2]; // 2 hours ahead
            WeatherData.ForecastItem previousForecastItem = hourlyForecastItems[1]; // 1 hours ahead

            // we need to find the low/hi temperatures, because hourly forecast only have one temperature reading, no low/hi values
            string lowTemperature = previousForecastItem.LowTemp;
            string highTemperature = currentForecastItem.LowTemp;
            if (float.Parse(previousForecastItem.LowTemp) > float.Parse(currentForecastItem.LowTemp))
            {
                lowTemperature = currentForecastItem.LowTemp;
                highTemperature = previousForecastItem.LowTemp;
            }

            DailyForecast.Add(new ForecastItem()
            {
                DayOfTheWeek = LanguageStrings.ui_ForecastCaption.ToUpper(),
                ForecastIcon = GetFormattedIconResx(currentForecastItem.Icon, currentForecastItem.TimestampEpoch),
                Condition = GetForecastConditionDescription(currentForecastItem.Condition, currentForecastItem.Icon),
                LowTemp = GetFormattedWeatherValue(lowTemperature, WeatherValueFormatType.Temperature),
                HighTemp = GetFormattedWeatherValue(highTemperature, WeatherValueFormatType.Temperature),
                Pop = GetFormattedWeatherValue(currentForecastItem.Pop, WeatherValueFormatType.Pop)
            });

            // next 5 days forecast
            int daysLoaded = 0;
            foreach (WeatherData.ForecastItem dailyForecastItem in dailyForecastItems)
            {
                DateTime timestamp = GetTimestampFromEpoch(dailyForecastItem.TimestampEpoch);

                // skip all days untill tomorrow
                if (DateTime.Now.Date >= timestamp.Date)
                    continue;

                string dayOfTheWeek = String.Empty;

                switch (Configuration.Instance.Language)
                {
                    case Language.fr:
                        dayOfTheWeek = String.Format("{0} {1} {2}", /* sun. 09 Dec */
                                                    timestamp.ToString("ddd", new CultureInfo("fr-FR")),
                                                    timestamp.ToString("dd"),
                                                    timestamp.ToString("MMM"), new CultureInfo("fr-FR"));
                        break;
                    default:    // US
                        dayOfTheWeek = String.Format("{0} {1}/{2}", /* Sun 12/09 */
                                                    timestamp.ToString("ddd", new CultureInfo("en-US")),
                                                    timestamp.ToString("MM"),
                                                    timestamp.ToString("dd"));
                        break;
                }

                DailyForecast.Add(new ForecastItem()
                {
                    DayOfTheWeek = dayOfTheWeek,
                    ForecastIcon = GetFormattedIconResx(dailyForecastItem.Icon, null),
                    Condition = GetForecastConditionDescription(dailyForecastItem.Condition, dailyForecastItem.Icon),
                    LowTemp = GetFormattedWeatherValue(dailyForecastItem.LowTemp, WeatherValueFormatType.Temperature),
                    HighTemp = GetFormattedWeatherValue(dailyForecastItem.HighTemp, WeatherValueFormatType.Temperature),
                    Pop = GetFormattedWeatherValue(dailyForecastItem.Pop, WeatherValueFormatType.Pop)
                });

                daysLoaded++;
                // max of 5 days can be shown
                if (daysLoaded == 5)
                    break;
            }
        }

        private void LoadHourlyForecastProperties(List<WeatherData.ForecastItem> hourlyForecastItems)
        {
            HourlyForecast.Clear();

            foreach (WeatherData.ForecastItem hourlyForecastItem in hourlyForecastItems)
            {
                string dayOfTheWeek = String.Empty;
                DateTime timestamp = GetTimestampFromEpoch(hourlyForecastItem.TimestampEpoch);

                switch (Configuration.Instance.Language)
                {
                    case Language.fr:
                        dayOfTheWeek = String.Format("{0}", timestamp.ToString("ddd", new CultureInfo("fr-FR"))); /* Sun, Mon, etc */
                        break;
                    default:    // US
                        dayOfTheWeek = String.Format("{0}", timestamp.ToString("ddd", new CultureInfo("en-US"))); /* Sun, Mon, etc */
                        break;
                }

                HourlyForecast.Add(new ForecastItem()
                {
                    DayOfTheWeek = dayOfTheWeek,
                    TimeOfTheDay = GetFormattedTimeFromEpoch(hourlyForecastItem.TimestampEpoch),
                    ForecastIcon = GetFormattedIconResx(hourlyForecastItem.Icon, hourlyForecastItem.TimestampEpoch),
                    HighTemp = GetFormattedWeatherValue(hourlyForecastItem.HighTemp, WeatherValueFormatType.Temperature),
                    Pop = GetFormattedWeatherValue(hourlyForecastItem.Pop, WeatherValueFormatType.Pop)
                });
            }
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

                List<String> buttons = new List<String>();
                buttons.Add(LanguageStrings.ui_ButtonClose); // normal button
                if (goToSettingsPage == true)
                    buttons.Add(LanguageStrings.ui_ButtonGoToSettings); // only show up if requested

                DialogResult result = mcEnvironment.Dialog(message, LanguageStrings.ui_DialogWeatherDataRefreshCaption, buttons, 60, true, null);
                if (result.ToString() == "101")
                {
                    _weatherRefreshTimer.Enabled = false;
                    MyAddIn.Instance.GoSettingsPage();
                }
                else
                    MyAddIn.Instance.AddInHost.ApplicationContext.CloseApplication();
            }
        }

        #region Utilities

        private DateTime GetTimestampFromEpoch(string epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Double.Parse(epoch)).ToLocalTime();
        }

        private string GetFormattedTimestampFromEpoch(string epoch)
        {
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

        private string GetFormattedTimeFromEpoch(string epoch)
        {
            DateTime parsedTimestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Double.Parse(epoch)).ToLocalTime();
            string formattedTime;

            switch (_weatherClockTimeFormat)
            {
                case ClockTimeFormat.Hours12:
                    formattedTime = parsedTimestamp.ToString("htt");
                    break;
                default:
                    formattedTime = parsedTimestamp.ToString("HH:mm");
                    break;
            }

            return formattedTime;
        }

        private string GetFormattedWeatherValue(string weatherValue, WeatherValueFormatType formatType)
        {
            string formattedValue = String.Empty;

            switch (formatType)
            {
                case WeatherValueFormatType.Temperature:
                    // round up temperature value
                    weatherValue = Math.Round(GetDecimalFromString(weatherValue), MidpointRounding.AwayFromZero).ToString();
                    switch (_weatherUnit)
                    {
                        case WeatherUnit.Imperial:
                            formattedValue = String.Format("{0}°F", weatherValue);
                            break;
                        default:
                            formattedValue = String.Format("{0}°C", weatherValue);
                            break;
                    }
                    break;
                case WeatherValueFormatType.Humidity:
                    formattedValue = String.Format("{0}%", Math.Floor(GetDecimalFromString(weatherValue) * 100));
                    break;
                case WeatherValueFormatType.WindSpeed:
                    decimal windSpeedValue = Math.Round(GetDecimalFromString(weatherValue), MidpointRounding.AwayFromZero);
                    // check if no wind
                    if (windSpeedValue == 0)
                    {
                        formattedValue = LanguageStrings.ui_WindSpeedZeroDescription;
                        break;
                    }
                    switch (_weatherUnit)
                    {
                        case WeatherUnit.Imperial:
                            formattedValue = String.Format("{0} mph", windSpeedValue);
                            break;
                        default:
                            formattedValue = String.Format("{0} kph", windSpeedValue);
                            break;
                    }
                    break;
                case WeatherValueFormatType.WindDirection:
                    // check if no wind
                    if (String.IsNullOrEmpty(weatherValue) == true)
                        break;

                    // calculate the abbreviation
                    string[] windDirections = { "N", "NE", "E", "SE", "S", "SW", "W", "NW", "N" };
                    formattedValue = windDirections[(int)Math.Round((GetDecimalFromString(weatherValue) % 360) / 45)];
                    break;
                case WeatherValueFormatType.UvIndex:
                    // UV index format parsing based on https://en.wikipedia.org/wiki/Ultraviolet_index
                    int uvIndex = Int32.Parse(weatherValue);
                    if (uvIndex < 3)
                        formattedValue = String.Format("{0} ({1})", uvIndex, LanguageStrings.ui_UvIndex_Low);
                    else if (uvIndex >= 3 && uvIndex < 6)
                        formattedValue = String.Format("{0} ({1})", uvIndex, LanguageStrings.ui_UvIndex_Moderate);
                    else if (uvIndex >= 6 && uvIndex < 8)
                        formattedValue = String.Format("{0} ({1})", uvIndex, LanguageStrings.ui_UvIndex_High);
                    else if (uvIndex >= 8 && uvIndex < 11)
                        formattedValue = String.Format("{0} ({1})", uvIndex, LanguageStrings.ui_UvIndex_VeryHigh);
                    else if (uvIndex >= 11)
                        formattedValue = String.Format("{0} ({1})", uvIndex, LanguageStrings.ui_UvIndex_Extreme);
                    break;
                case WeatherValueFormatType.Pressure:
                    decimal pressureValue = Math.Floor(GetDecimalFromString(weatherValue));
                    switch (_weatherUnit)
                    {
                        case WeatherUnit.Imperial:
                            formattedValue = String.Format("{0} mb", pressureValue);
                            break;
                        default:
                            formattedValue = String.Format("{0} hPa", pressureValue);
                            break;
                    }
                    break;
                default: // WeatherValueFormatType.Pop
                    formattedValue = null;  // UI will hide pop indicator if value is null
                    int popValue = (int)Math.Floor(GetDecimalFromString(weatherValue) * 100); // pop value is decimal, 0.14 for 14%
                    if (popValue > 0)
                        formattedValue = popValue + "%";
                    break;
            }

            return formattedValue;
        }

        private string GetFormattedIconResx(string iconName, string epoch)
        {
            string result = "none";

            if (iconName.EndsWith("-day", StringComparison.InvariantCultureIgnoreCase) == true
               || iconName.EndsWith("-night", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                // icon name is complete no additional formatting is needed
                result = iconName;
            }
            else
            {
                if (String.IsNullOrEmpty(epoch) == true)
                {
                    // epoch is not provided, day icon will be returned
                    result = iconName + "-day";
                }
                else
                {
                    // epoch is provided, day/night icon will be returned
                    int hour = GetTimestampFromEpoch(epoch).Hour;

                    if (hour < 6 || hour > 18)
                        result = iconName + "-night";
                    else
                        result = iconName + "-day";
                }
            }

            return "resx://myForecast/myForecast.Resources/" + result.Replace("-", "_"); // dash is not valid in resource name
        }

        private string GetForecastConditionDescription(string conditionDescription, string iconName)
        {
            // forecast description space is tight - currently 13 characters
            string result = conditionDescription;

            // check for max size
            if (result.Length > 13)
            {
                // retrieve condition name by using the icon name
                string condition = iconName.Replace("-day", "").Replace("-night", "").Replace("-", " ");
                result = LanguageStrings.ResourceManager.GetObject("ui_Condition" + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(condition).Replace(" ", "")).ToString();
            }

            return result;
        }

        private decimal GetDecimalFromString(string weatherValue)
        {
            // force en-US culture for parsing decimal since the decimal separator from Dark Sky API is always a comma, example: 32.34
            // this should solve the issue with incorrectly parsing decimals under different regional settings like Dutch (Netherlands)
            return Convert.ToDecimal(weatherValue, new CultureInfo("en-US"));
        }

        #endregion
    }

    public class ForecastItem : ModelItem
    {
        #region Private Properties

        private string _dayOfTheWeek;
        private string _timeOfTheDay;
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

        public string TimeOfTheDay
        {
            get { return _timeOfTheDay; }
            set { _timeOfTheDay = value; FirePropertyChanged("TimeOfTheDay"); }
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

        public string Pop
        {
            get { return _pop; }
            set { _pop = value; FirePropertyChanged("Pop"); }
        }
    }
}