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

            string latitude;
            string longitude;
            Utilities.GetLatLonCoordinates(_weatherLocationCode, out latitude, out longitude);

            _weatherFileName = String.Format(Configuration.Instance.WeatherFileNamePattern, _weatherLocationCode.Replace(".", "_").Replace(",", ""), _weatherLanguage, _weatherUnit.ToString().ToLower());
            _weatherFileLocation = Path.Combine(Configuration.Instance.ConfigFileFolder, _weatherFileName);
            _weatherApiUri = String.Format(Configuration.Instance.ApiUrlPattern, latitude, longitude, _weatherApiKey, _weatherLanguage, _weatherUnit.ToString().ToLower());

            _xmlWeatherData = new XmlDocument();
            _dailyForecast = new ArrayListDataSet();
            _hourlyForecast = new ArrayListDataSet();

            _uiRefreshNeeded = true;
            _isLoaded = false;

            // check current version, cleanup weather data files when the version change
            if (Configuration.Instance.Version.Equals(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()) == false)
            {
                Utilities.PurgeAllWeatherDataFiles(Configuration.Instance.ConfigFileFolder);
                Configuration.Instance.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                Configuration.Instance.Save();
            }

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
                        if (response.StatusCode == HttpStatusCode.Unauthorized)
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
            CurrentConditionIcon = GetFormattedIconResx(currentCondition.IconId, currentCondition.TimestampEpoch);
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
                ForecastIcon = GetFormattedIconResx(currentForecastItem.IconId, currentForecastItem.TimestampEpoch),
                Condition = GetForecastConditionDescription(currentForecastItem.Condition, currentForecastItem.IconId),
                LowTemp = GetFormattedWeatherValue(lowTemperature, WeatherValueFormatType.Temperature),
                HighTemp = GetFormattedWeatherValue(highTemperature, WeatherValueFormatType.Temperature),
                Pop = GetFormattedWeatherValue(currentForecastItem.Pop, WeatherValueFormatType.Pop)
            });

            // next 5 days forecast
            int daysLoaded = 0;
            foreach (WeatherData.ForecastItem dailyForecastItem in dailyForecastItems)
            {
                DateTime timestamp = Utilities.GetTimestampFromEpoch(dailyForecastItem.TimestampEpoch);

                // skip all days until tomorrow
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
                    ForecastIcon = GetFormattedIconResx(dailyForecastItem.IconId, null),
                    Condition = GetForecastConditionDescription(dailyForecastItem.Condition, dailyForecastItem.IconId),
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
                DateTime timestamp = Utilities.GetTimestampFromEpoch(hourlyForecastItem.TimestampEpoch);

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
                    ForecastIcon = GetFormattedIconResx(hourlyForecastItem.IconId, hourlyForecastItem.TimestampEpoch),
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

                // adding more information for "Catastrophic Error"
                if (String.Equals(LanguageStrings.ui_DialogCatastrophicError, message, StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    // check for Tsl 1.2 support
                    using (WebClientWithCompression webClient = new WebClientWithCompression())
                    {
                        bool tls12Supported = webClient.IsTls12Supported();
                        if (tls12Supported == false)
                            message = LanguageStrings.ui_DialogTls12Error;
                    }
                }

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
                    formattedValue = String.Format("{0}%", Math.Floor(GetDecimalFromString(weatherValue)));
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
                    int uvIndex = (int)Math.Round(GetDecimalFromString(weatherValue), MidpointRounding.AwayFromZero);
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
                    decimal popValue = Decimal.Parse(weatherValue); // pop value is decimal in millimeters
                    if (popValue > 0)
                    {
                        switch (_weatherUnit)
                        {
                            case WeatherUnit.Imperial:
                                formattedValue = String.Format("{0:0.##}\"", popValue * 0.03937008M); // 1mm = 0.03937008 inch
                                break;
                            default:
                                formattedValue = String.Format("{0}mm", weatherValue);
                                break;
                        }
                    }
                    break;
            }

            return formattedValue;
        }

        private string GetFormattedIconResx(string iconId, string epoch)
        {
            string result = "none";
            bool isNight = false;

            // epoch is provided, day/night icon will be returned
            if (String.IsNullOrEmpty(epoch) == false)
            {
                int hour = Utilities.GetTimestampFromEpoch(epoch).Hour;
                if (hour < 6 || hour > 18)
                    isNight = true;
            }

            switch (iconId)
            {
                // Group 2xx: Thunderstorm
                case "200": // thunderstorm with light rain
                case "201": // thunderstorm with rain
                case "202": // thunderstorm with heavy rain
                case "210": // light thunderstorm
                case "211": // thunderstorm		
                case "212": // heavy thunderstorm
                case "221": // ragged thunderstorm
                case "230": // thunderstorm with light drizzle
                case "231": // thunderstorm with drizzle
                case "232": // thunderstorm with heavy drizzle
                    if (isNight == true)
                        result = "weezle_night_thunder_rain";
                    else
                        result = "weezle_cloud_thunder_rain";
                    break;

                // Group 3xx: Drizzle
                case "300": // light intensity drizzle
                case "301": // drizzle
                case "302": // heavy intensity drizzle
                case "310": // light intensity drizzle rain		
                case "311": // drizzle rain
                case "312": // heavy intensity drizzle rain
                case "313": // shower rain and drizzle		
                case "314": // heavy shower rain and drizzle
                case "321": // shower drizzle
                    if (isNight == true)
                        result = "drizzle_night";
                    else
                        result = "drizzle_day";
                    break;

                // Group 5xx: Rain
                case "500": // light rain
                case "501": // moderate rain
                case "502": // heavy intensity rain
                case "503": // very heavy rain
                case "504": // extreme rain
                case "511": // freezing rain
                case "520": // light intensity shower rain
                case "521": // shower rain
                case "522": // heavy intensity shower rain		
                case "531": // ragged shower rain
                    if (isNight == true)
                        result = "rain_night";
                    else
                        result = "rain_day";
                    break;

                // Group 6xx: Snow
                case "600": // light snow
                case "601": // snow
                case "602": // heavy snow
                case "620": // light shower snow
                case "621": // shower snow
                case "622": // heavy shower snow		
                    if (isNight == true)
                        result = "snow_night";
                    else
                        result = "snow_day";
                    break;
                case "611": // sleet
                case "612": // light shower sleet
                case "613": // shower sleet		
                case "615": // light rain and snow
                case "616": // rain and snow
                    if (isNight == true)
                        result = "sleet_night";
                    else
                        result = "sleet_day";
                    break;

                // Group 7xx: Atmosphere 
                case "701": // mist
                case "711": // smoke
                case "721": // haze
                case "731": // sand/dust whirls	
                case "741": // fog
                case "751": // sand
                case "761": // dust
                case "762": // volcanic ash
                case "771": // squalls
                case "781": // tornado	
                    if (isNight == true)
                        result = "fog_night";
                    else
                        result = "fog_day";
                    break;

                // Group 800: Clear
                case "800": // clear 
                    if (isNight == true)
                        result = "clear_night";
                    else
                        result = "clear_day";
                    break;

                // Group 80x: Clouds
                case "801": // few clouds
                case "802": // scattered clouds
                    if (isNight == true)
                        result = "partly_cloudy_night";
                    else
                        result = "partly_cloudy_day";
                    break;
                case "803": // broken clouds
                case "804": // overcast clouds
                    if (isNight == true)
                        result = "cloudy_night";
                    else
                        result = "cloudy_day";
                    break;

                default:
                    break;
            }

            return "resx://myForecast/myForecast.Resources/" + result;
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
            // Force en-US culture for parsing decimal since the decimal separator from OpenWeather API is always a comma, example: 32.34
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