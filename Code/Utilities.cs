using myForecast.Localization;
using System;
using System.Globalization;
using System.IO;

namespace myForecast
{
    public static class Utilities
    {
        internal static void PurgeAllWeatherDataFiles(string weatherDataFilesLocation)
        {
            try
            {
                if (Directory.Exists(weatherDataFilesLocation) == true)
                {
                    foreach (string file in Directory.GetFiles(weatherDataFilesLocation, "*.dat"))
                    {
                        if (File.Exists(file) == true)
                        {
                            File.Delete(file);
                            Logger.LogInformation("Purged weather data file: " + file);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception);
            }
        }

        internal static void GetLatLonCoordinates(string weatherLocationCode, out string latitude, out string longitude)
        {
            latitude = String.Empty;
            longitude = String.Empty;

            try
            {
                string[] coordinates = weatherLocationCode.Trim('[').Trim(']').Split(',');
                if (coordinates.Length > 1)
                {
                    latitude = coordinates[0];
                    longitude = coordinates[1];
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception);
            }
        }

        internal static string LoadWeatherDataFromFile(string weatherDataFileLocation)
        {
            string result = String.Empty;

            if (File.Exists(weatherDataFileLocation) == true)
                result = File.ReadAllText(weatherDataFileLocation);

            return result;
        }

        internal static DateTime GetUtcTimestampFromEpochKeep(string epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Double.Parse(epoch));
        }

        internal static DateTime GetTimestampFromEpoch(string epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Double.Parse(epoch)).ToLocalTime();
        }

        internal static DateTime GetTimestampFromIso8601(string iso8601)
        {
            return DateTime.Parse(iso8601).ToLocalTime();
        }

        internal static string GetFormattedTimestampFromEpoch(string epoch)
        {
            DateTime parsedTimestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Double.Parse(epoch)).ToLocalTime();
            string formattedTimestamp;

            switch (Configuration.Instance.ClockTimeFormat.GetValueOrDefault(ClockTimeFormat.Hours12))
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

        internal static string GetFormattedTimestampFromIso8601(string iso8601)
        {
            DateTime parsedTimestamp = DateTime.Parse(iso8601);
            string formattedTimestamp;

            switch (Configuration.Instance.ClockTimeFormat.GetValueOrDefault(ClockTimeFormat.Hours12))
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

        internal static string GetFormattedTimeFromEpoch(string epoch)
        {
            DateTime parsedTimestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Double.Parse(epoch)).ToLocalTime();
            string formattedTime;

            switch (Configuration.Instance.ClockTimeFormat.GetValueOrDefault(ClockTimeFormat.Hours12))
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
       
        internal static string GetFormattedWeatherValue(string weatherValue, WeatherValueFormatType formatType)
        {
            string formattedValue = String.Empty;

            switch (formatType)
            {
                case WeatherValueFormatType.Temperature:
                    // round up temperature value
                    weatherValue = Math.Round(GetDecimalFromString(weatherValue), MidpointRounding.AwayFromZero).ToString();
                    switch (Configuration.Instance.WeatherUnit.GetValueOrDefault(WeatherUnit.Imperial))
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
                    switch (Configuration.Instance.WeatherUnit.GetValueOrDefault(WeatherUnit.Imperial))
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
                    decimal uvIndex = Math.Floor(GetDecimalFromString(weatherValue));
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
                    switch (Configuration.Instance.WeatherUnit.GetValueOrDefault(WeatherUnit.Imperial))
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

        internal static string GetFormattedIconResx(string iconId, string epoch)
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
                        result = "thunder_night";
                    else
                        result = "thunder_day";
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

        internal static string GetForecastConditionDescription(string iconResource)
        {
            // forecast description space is tight - currently 14 characters
            string iconName = iconResource.Substring(iconResource.LastIndexOf("/") + 1);

            // retrieve condition name by using the icon name
            string condition = iconName.Replace("_day", String.Empty).Replace("_night", String.Empty);
            return LanguageStrings.ResourceManager.GetObject("ui_Condition" + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(condition).Replace("_", String.Empty)).ToString();
        }

        internal static decimal GetDecimalFromString(string weatherValue)
        {
            // Force en-US culture for parsing decimal since the decimal separator from OpenWeather API is always a comma, example: 32.34
            // this should solve the issue with incorrectly parsing decimals under different regional settings like Dutch (Netherlands)
            return Convert.ToDecimal(weatherValue, new CultureInfo("en-US"));
        }
    }
}