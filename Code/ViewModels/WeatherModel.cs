﻿using Microsoft.MediaCenter;
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
            if (Configuration.Instance.IsValid() == true)
            {
                _weatherApiKey = Configuration.Instance.ApiKey;
                _weatherLocationCode = Configuration.Instance.LocationCode;
                _weatherUnit = Configuration.Instance.WeatherUnit.GetValueOrDefault(WeatherUnit.Imperial);
                _weatherClockTimeFormat = Configuration.Instance.ClockTimeFormat.GetValueOrDefault(ClockTimeFormat.Hours12);
                _weatherRefreshRateInMinutes = Configuration.Instance.RefreshRateInMinutes.GetValueOrDefault(10);
                _weatherLanguage = Configuration.Instance.Language.GetValueOrDefault(Language.en);
            }

            // set the correct language for the UI thread
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Enum.GetName(typeof(Language), Configuration.Instance.Language));

            _weatherFileName = String.Format(Configuration.Instance.WeatherFileNamePattern, _weatherLocationCode, _weatherLanguage);
            _weatherFileName = _weatherFileName.Replace(".", "").Replace(",", "").Replace("-", ""); // small cleanup is needed for location coordinates
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
            // if the configuration is invalid load default weather collection
            if (Configuration.Instance.IsValid() == false)
            {
                LoadDefaultWeatherModel();
                return;
            }

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
                                ShowErrorDialog(String.Format("{0}: {1}", LanguageStrings.ui_DialogErrorReceivedFromWeatherProvider, "Weather station is down for maintenance."), null, true);
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
                    HttpWebResponse response = (HttpWebResponse)webException.Response;
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                        ShowErrorDialog(LanguageStrings.ui_DialogInvalidApiKeyReceivedFromWeatherProvider, webException);
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                        ShowErrorDialog(LanguageStrings.ui_DialogInvalidLocationDataReceivedFromWeatherProvider, webException);
                    else
                        ShowErrorDialog(LanguageStrings.ui_DialogErrorWhileConnectingToWeatherProvider, webException);
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
            LocationName = _weatherData.LocationName;

            // Weather alerts support
            LoadWeatherAlertInformation(_weatherData.Alerts);

            // Current condition support
            LoadCurrentConditionProperties(_weatherData.CurrentForecast);

            // Daily/Current forecast support
            DailyForecast.Clear();
            LoadCurrentForecastProperties(_xmlWeatherData.SelectSingleNode("response/hourly_forecast"));
            LoadDailyForecastProperties(_xmlWeatherData.SelectNodes("response/forecast/simpleforecast/forecastdays/forecastday"));

            // Hourly forecast support
            LoadHourlyForecastProperties(_xmlWeatherData.SelectNodes("response/hourly_forecast/forecast"));

            IsLoaded = true;
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
            DailyForecast.Clear();
            for (int i = 1; i < 7; i++)
            {
                DailyForecast.Add(new ForecastItem()
                {
                    DayOfTheWeek = "N/A",
                    ForecastIcon = "resx://myForecast/myForecast.Resources/unknown",
                    Condition = "N/A",
                    LowTemp = "N/A",
                    HighTemp = "N/A"
                });
            }

            // 36 hours forecast by default
            HourlyForecast.Clear();
            for (int i = 1; i < 36; i++)
            {
                HourlyForecast.Add(new ForecastItem()
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

        private void LoadCurrentConditionProperties(WeatherData.CurrentItem currentCondition)
        {
            // TODO ICONS !!!!
            //string icon_url = currentConditionNode.SelectSingleNode("icon_url").InnerText;          // icons.wxug.com/i/c/k/nt_clear.gif
            //string[] iconNameParts = icon_url.Substring(icon_url.LastIndexOf("/") + 1).Split('.');  // [0]nt_clear [1].gif
            //CurrentConditionIcon = "resx://myForecast/myForecast.Resources/" + iconNameParts[0];
            CurrentConditionDescription = currentCondition.Description;
            CurrentConditionHumidity = currentCondition.Humidity;
            CurrentConditionTemperature = GetFormattedWeatherValue(currentCondition.Temperature, WeatherValueFormatType.Temperature);
            CurrentConditionFeelsLike = GetFormattedWeatherValue(currentCondition.FeelsLike, WeatherValueFormatType.Temperature);
            CurrentConditionDewPoint = GetFormattedWeatherValue(currentCondition.DewPoint, WeatherValueFormatType.Temperature);
            CurrentConditionWind = String.Format("{0} {1}",
                                                    GetFormattedWeatherValue(currentCondition.WindSpeed, WeatherValueFormatType.WindSpeed),
                                                    GetFormattedWeatherValue(currentCondition.WindDirection, WeatherValueFormatType.WindDirection));
            CurrentConditionUvIndex = GetFormattedWeatherValue(currentCondition.UvIndex, WeatherValueFormatType.UvIndex);
            CurrentConditionPressure = GetFormattedWeatherValue(currentCondition.Pressure, WeatherValueFormatType.Pressure);
        }

        private void LoadCurrentForecastProperties(XmlNode hourlyForecastNode)
        {
            int forecastHour = DateTime.Now.Hour;
            XmlNode currentForecastNode = hourlyForecastNode.SelectSingleNode("forecast[FCTTIME/hour='" + DateTime.Now.AddHours(2).Hour + "']");
            XmlNode previousForecastNode = hourlyForecastNode.SelectSingleNode("forecast[FCTTIME/hour='" + DateTime.Now.AddHours(1).Hour + "']");

            string currentForecastTitle = LanguageStrings.ui_ForecastCaption.ToUpper();
            string forecastIconName = currentForecastNode.SelectSingleNode("icon").InnerText;

            // switch to night icons
            if (forecastHour < 6 || forecastHour > 18)
                forecastIconName = "nt_" + forecastIconName;

            // find the low/hi temperatures
            XmlNode lowTemperatureNode;
            XmlNode highTemperatureNode;
            if (float.Parse(currentForecastNode.SelectSingleNode("temp/english").InnerText) > float.Parse(previousForecastNode.SelectSingleNode("temp/english").InnerText))
            {
                lowTemperatureNode = previousForecastNode;
                highTemperatureNode = currentForecastNode;
            }
            else
            {
                lowTemperatureNode = currentForecastNode;
                highTemperatureNode = previousForecastNode;
            }

            DailyForecast.Add(new ForecastItem()
            {
                DayOfTheWeek = currentForecastTitle,
                ForecastIcon = "resx://myForecast/myForecast.Resources/" + forecastIconName,
                Condition = CleanForecastConditionDescription(currentForecastNode.SelectSingleNode("condition").InnerText),
                LowTemp = GetFormattedCurrentForecastTemperature(lowTemperatureNode.SelectSingleNode("temp")),
                HighTemp = GetFormattedCurrentForecastTemperature(highTemperatureNode.SelectSingleNode("temp")),
                PopIcon = GetFormattedPopIconResx(currentForecastNode),
                Pop = GetFormattedPop(currentForecastNode)
            });
        }

        private void LoadDailyForecastProperties(XmlNodeList forecastNodes)
        {
            for (int i = 1; i < forecastNodes.Count - 1; i++) // skip node[0] for the tonight forecast
            {
                string dayOfTheWeek = String.Empty;

                switch (Configuration.Instance.Language)
                {
                    case Language.fr:
                        dayOfTheWeek = String.Format("{0} {1} {2}",
                                                    forecastNodes[i].SelectSingleNode("date/weekday_short").InnerText.ToUpper(),
                                                    forecastNodes[i].SelectSingleNode("date/day").InnerText,
                                                    forecastNodes[i].SelectSingleNode("date/monthname_short").InnerText.ToUpper());
                        break;
                    default:    // US
                        dayOfTheWeek = String.Format("{0} {1}/{2}",
                                                    forecastNodes[i].SelectSingleNode("date/weekday_short").InnerText.ToUpper(),
                                                    forecastNodes[i].SelectSingleNode("date/month").InnerText,
                                                    forecastNodes[i].SelectSingleNode("date/day").InnerText);
                        break;
                }

                DailyForecast.Add(new ForecastItem()
                {
                    DayOfTheWeek = dayOfTheWeek,
                    ForecastIcon = "resx://myForecast/myForecast.Resources/" + forecastNodes[i].SelectSingleNode("icon").InnerText,
                    Condition = CleanForecastConditionDescription(forecastNodes[i].SelectSingleNode("conditions").InnerText),
                    LowTemp = GetFormattedDailyForecastTemperature(forecastNodes[i].SelectSingleNode("low")),
                    HighTemp = GetFormattedDailyForecastTemperature(forecastNodes[i].SelectSingleNode("high")),
                    PopIcon = GetFormattedPopIconResx(forecastNodes[i]),
                    Pop = GetFormattedPop(forecastNodes[i])
                });
            }
        }

        private void LoadHourlyForecastProperties(XmlNodeList hourlyForecastNodes)
        {
            HourlyForecast.Clear();
            for (int i = 0; i < hourlyForecastNodes.Count; i++)
            {
                string forecastIconName = hourlyForecastNodes[i].SelectSingleNode("icon").InnerText;
                int forecastHour = GetTimestampFromEpoch(hourlyForecastNodes[i].SelectSingleNode("FCTTIME/epoch").InnerText).Hour;

                // switch to night icons
                if (forecastHour < 6 || forecastHour > 18)
                    forecastIconName = "nt_" + forecastIconName;

                HourlyForecast.Add(new ForecastItem()
                {
                    DayOfTheWeek = hourlyForecastNodes[i].SelectSingleNode("FCTTIME/weekday_name_abbrev").InnerText.ToUpper(),
                    TimeOfTheDay = GetFormattedTimeFromEpoch(hourlyForecastNodes[i].SelectSingleNode("FCTTIME/epoch").InnerText),
                    ForecastIcon = "resx://myForecast/myForecast.Resources/" + forecastIconName,
                    HighTemp = GetFormattedCurrentForecastTemperature(hourlyForecastNodes[i].SelectSingleNode("temp")),
                    PopIcon = GetFormattedPopIconResx(hourlyForecastNodes[i]),
                    Pop = GetFormattedPop(hourlyForecastNodes[i])
                });
            }
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

        private DateTime GetTimestampFromEpoch(string epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Double.Parse(epoch)).ToLocalTime();
        }

        private string GetFormattedCurrentForecastTemperature(XmlNode temperatureNode)
        {
            string temperature;

            switch (_weatherUnit)
            {
                case WeatherUnit.Imperial:
                    temperature = temperatureNode.SelectSingleNode("english").InnerText + "°F";
                    break;
                default:
                    temperature = temperatureNode.SelectSingleNode("metric").InnerText + "°C";
                    break;
            }

            return temperature;
        }

        private string GetFormattedDailyForecastTemperature(XmlNode temperatureNode)
        {
            string temperature;

            switch (_weatherUnit)
            {
                case WeatherUnit.Imperial:
                    temperature = temperatureNode.SelectSingleNode("fahrenheit").InnerText + "°F";
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
                popIconResx += "Pop";

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
            // forecast description space is tight - currently 13 characters
            // for similar items the order is from most specific to less specific
            string[] removeStrings = new string[] { "chance of a", "chance of",     // EN
                                                    "risque de", "risque d'" };     // FR 

            string result = conditionDescription;
            for (var i = 0; i < removeStrings.Length; i++)
            {
                int indexOfValue = conditionDescription.ToLower().IndexOf(removeStrings[i]);
                if (indexOfValue > -1)
                {
                    result = conditionDescription.Substring(indexOfValue + removeStrings[i].Length);
                    break;
                }
            }

            result = result.Trim();

            // fix plural condition words
            switch (result.ToLower())
            {
                case "thunderstorm":        // EN
                case "orage":               // FR
                    result = result + "s";
                    break;
            }

            // check for max size
            if (result.Length > 13)
            {
                // this may produce strange results with other languages than English
                string[] sentenceParts = result.Split(' ');
                result = sentenceParts[sentenceParts.Length - 1];
            }

            return UppercaseFirstLetter(result);
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

        private void LoadWeatherAlertInformation(List<WeatherData.AlertItem> alertItems)
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

        #region Utilities

        private string UppercaseFirstLetter(string text)
        {
            if (string.IsNullOrEmpty(text))
                return String.Empty;

            return Char.ToUpper(text[0]) + text.Substring(1);
        }

        public string GetFormattedTimestampFromEpoch(string epoch)
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

        public string GetFormattedWeatherValue(string weatherValue, WeatherValueFormatType formatType)
        {
            string formattedValue = String.Empty;

            switch (formatType)
            {
                case WeatherValueFormatType.Temperature:
                    // round up temperature value
                    weatherValue = Math.Round(Decimal.Parse(weatherValue), MidpointRounding.AwayFromZero).ToString();
                    switch (_weatherUnit)
                    {
                        case WeatherUnit.Imperial:
                            formattedValue = weatherValue + "°F";
                            break;
                        default:
                            formattedValue = weatherValue + "°C";
                            break;
                    }
                    break;
                case WeatherValueFormatType.WindSpeed:
                    // check if no wind
                    if (Math.Round(Decimal.Parse(weatherValue), MidpointRounding.AwayFromZero) == 0)
                    {
                        formattedValue = "Calm";
                        break;
                    }
                    switch (_weatherUnit)
                    {
                        case WeatherUnit.Imperial:
                            formattedValue = String.Format("{0} mph", weatherValue);
                            break;
                        default:
                            formattedValue = String.Format("{0} kph", weatherValue);
                            break;
                    }
                    break;
                case WeatherValueFormatType.WindDirection:
                    // check if no wind
                    if (String.IsNullOrEmpty(weatherValue) == true)
                        break;

                    // calculate the abbreviation
                    string[] windDirections = { "N", "NE", "E", "SE", "S", "SW", "W", "NW", "N" };
                    formattedValue = windDirections[(int)Math.Round((Decimal.Parse(weatherValue) % 360) / 45)];
                    break;
                case WeatherValueFormatType.UvIndex:
                    // UV index format parsing based on https://en.wikipedia.org/wiki/Ultraviolet_index
                    int uvIndex = Int32.Parse(weatherValue.Replace(".0", ""));
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
                default: // WeatherValueFormatType.Pressure
                    switch (_weatherUnit)
                    {
                        case WeatherUnit.Imperial:
                            formattedValue = weatherValue + " in";
                            break;
                        default:
                            formattedValue = weatherValue + " hPa";
                            break;
                    }
                    break;
            }

            return formattedValue;
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