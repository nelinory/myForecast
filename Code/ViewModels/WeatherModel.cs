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
        private readonly string _weatherProviderApiUri;
        private readonly string _weatherAlertsFileName;
        private readonly string _weatherAlertsFileLocation;
        private readonly string _weatherAlertsProviderApiUri;

        private bool _uiRefreshNeeded;
        private Timer _weatherRefreshTimer;
        private WeatherData _weatherData;

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
            // set the correct language for the UI thread
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Enum.GetName(typeof(Language), Configuration.Instance.Language));

            string latitude;
            string longitude;
            Utilities.GetLatLonCoordinates(Configuration.Instance.LocationCode, out latitude, out longitude);

            // weather data file
            _weatherFileName = String.Format(Configuration.Instance.WeatherFileNamePattern,
                                                Configuration.Instance.LocationCode.Replace(".", "_").Replace(",", ""),
                                                Configuration.Instance.Language.GetValueOrDefault(Language.en),
                                                Configuration.Instance.WeatherUnit.GetValueOrDefault(WeatherUnit.Imperial).ToString().ToLower());
            _weatherFileLocation = Path.Combine(Configuration.Instance.ConfigFileFolder, _weatherFileName);
            _weatherProviderApiUri = String.Format(Configuration.Instance.WeatherProviderApiUrlPattern,
                                                    latitude,
                                                    longitude,
                                                    Configuration.Instance.ApiKey,
                                                    Configuration.Instance.Language.GetValueOrDefault(Language.en),
                                                    Configuration.Instance.WeatherUnit.GetValueOrDefault(WeatherUnit.Imperial).ToString().ToLower());

            // weather alerts data file
            _weatherAlertsFileName = String.Format(Configuration.Instance.WeatherAlertsFileNamePattern,
                                                    Configuration.Instance.LocationCode.Replace(".", "_").Replace(",", ""),
                                                    Configuration.Instance.Language.GetValueOrDefault(Language.en),
                                                    Configuration.Instance.WeatherUnit.GetValueOrDefault(WeatherUnit.Imperial).ToString().ToLower());
            _weatherAlertsFileLocation = Path.Combine(Configuration.Instance.ConfigFileFolder, _weatherAlertsFileName);
            _weatherAlertsProviderApiUri = String.Format(Configuration.Instance.WeatherAlertsProviderApiUrlPattern, Configuration.Instance.LocationCode);

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

                        string weatherDataJson = webClient.DownloadString(_weatherProviderApiUri);
                        if (String.IsNullOrEmpty(weatherDataJson) == false)
                        {
                            string weatherAlertsJson = Utilities.GetWeatherAlertsDataFromUrl(_weatherAlertsProviderApiUri, Configuration.Instance.LocationCode);
                            _weatherData = new WeatherData(weatherDataJson, weatherAlertsJson);

                            // check for WeatherProvider specific error
                            if (_weatherData.IsWeatherInfoAvailable == false)
                            {
                                ShowErrorDialog(LanguageStrings.ui_DialogErrorReceivedFromWeatherProvider);
                                return;
                            }
                            else
                            {
                                File.WriteAllText(_weatherFileLocation, weatherDataJson, Encoding.UTF8);
                                File.WriteAllText(_weatherAlertsFileLocation, weatherAlertsJson, Encoding.UTF8);
                                _uiRefreshNeeded = true;
                            }
                        }
                        else
                            ShowErrorDialog(LanguageStrings.ui_DialogNoResponseReceivedFromWeatherProvider);
                    }
                    else
                    {
                        _weatherData = new WeatherData(Utilities.LoadWeatherDataFromFile(_weatherFileLocation),
                                                        Utilities.LoadWeatherDataFromFile(_weatherAlertsFileLocation));
                    }

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
            LastUpdateTimestamp = Utilities.GetFormattedTimestampFromEpoch(_weatherData.CurrentForecast.TimestampEpoch);
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
                    alertText.AppendLine(String.Format("{0}: {1}", LanguageStrings.ui_WeatherAlertStartDate, Utilities.GetFormattedTimestampFromIso8601(alertItem.StartDateTime)));
                    alertText.AppendLine(String.Format("{0}: {1}", LanguageStrings.ui_WeatherAlertExpireDate, Utilities.GetFormattedTimestampFromIso8601(alertItem.ExpireDateTime)));
                    alertText.AppendLine(alertItem.Description);
                }
            }

            if (alertText.Length == 0)
                alertText.AppendLine(LanguageStrings.ui_WeatherAlertInfoNotAvailable);

            WeatherAlertCaption = alertCaption;
            WeatherAlertText = alertText.ToString();
        }

        private void LoadCurrentConditionProperties(WeatherData.CurrentItem currentCondition)
        {
            CurrentConditionIcon = Utilities.GetFormattedIconResx(currentCondition.IconId, currentCondition.TimestampEpoch);
            CurrentConditionDescription = currentCondition.Description;
            CurrentConditionHumidity = Utilities.GetFormattedWeatherValue(currentCondition.Humidity, WeatherValueFormatType.Humidity);
            CurrentConditionTemperature = Utilities.GetFormattedWeatherValue(currentCondition.Temperature, WeatherValueFormatType.Temperature);
            CurrentConditionFeelsLike = Utilities.GetFormattedWeatherValue(currentCondition.FeelsLike, WeatherValueFormatType.Temperature);
            CurrentConditionDewPoint = Utilities.GetFormattedWeatherValue(currentCondition.DewPoint, WeatherValueFormatType.Temperature);
            CurrentConditionWind = String.Format("{0} {1}",
                                                Utilities.GetFormattedWeatherValue(currentCondition.WindSpeed, WeatherValueFormatType.WindSpeed),
                                                Utilities.GetFormattedWeatherValue(currentCondition.WindDirection, WeatherValueFormatType.WindDirection));
            CurrentConditionUvIndex = Utilities.GetFormattedWeatherValue(currentCondition.UvIndex, WeatherValueFormatType.UvIndex);
            CurrentConditionPressure = Utilities.GetFormattedWeatherValue(currentCondition.Pressure, WeatherValueFormatType.Pressure);
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
                ForecastIcon = Utilities.GetFormattedIconResx(currentForecastItem.IconId, currentForecastItem.TimestampEpoch),
                Condition = Utilities.GetForecastConditionDescription(currentForecastItem.Condition, currentForecastItem.IconId),
                LowTemp = Utilities.GetFormattedWeatherValue(lowTemperature, WeatherValueFormatType.Temperature),
                HighTemp = Utilities.GetFormattedWeatherValue(highTemperature, WeatherValueFormatType.Temperature),
                Pop = Utilities.GetFormattedWeatherValue(currentForecastItem.Pop, WeatherValueFormatType.Pop)
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
                    ForecastIcon = Utilities.GetFormattedIconResx(dailyForecastItem.IconId, null),
                    Condition = Utilities.GetForecastConditionDescription(dailyForecastItem.Condition, dailyForecastItem.IconId),
                    LowTemp = Utilities.GetFormattedWeatherValue(dailyForecastItem.LowTemp, WeatherValueFormatType.Temperature),
                    HighTemp = Utilities.GetFormattedWeatherValue(dailyForecastItem.HighTemp, WeatherValueFormatType.Temperature),
                    Pop = Utilities.GetFormattedWeatherValue(dailyForecastItem.Pop, WeatherValueFormatType.Pop)
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
                    TimeOfTheDay = Utilities.GetFormattedTimeFromEpoch(hourlyForecastItem.TimestampEpoch),
                    ForecastIcon = Utilities.GetFormattedIconResx(hourlyForecastItem.IconId, hourlyForecastItem.TimestampEpoch),
                    HighTemp = Utilities.GetFormattedWeatherValue(hourlyForecastItem.HighTemp, WeatherValueFormatType.Temperature),
                    Pop = Utilities.GetFormattedWeatherValue(hourlyForecastItem.Pop, WeatherValueFormatType.Pop)
                });
            }
        }

        private bool IsWeatherRefreshRequired()
        {
            bool isWeatherRefreshRequired = true;

            if (File.Exists(_weatherFileLocation) == true)
            {
                FileInfo fileInfo = new FileInfo(_weatherFileLocation);
                if (fileInfo.LastWriteTime < DateTime.Now.AddMinutes(-Configuration.Instance.RefreshRateInMinutes.GetValueOrDefault(10)))
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