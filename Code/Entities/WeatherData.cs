using LightWeightJsonParser;
using System;
using System.Collections.Generic;

namespace myForecast
{
    public class WeatherData
    {
        // https://progklb.github.io/lightweight-json-parser/

        public bool IsWeatherInfoAvailable { get; set; }
        public CurrentItem CurrentForecast { get; set; }
        public List<ForecastItem> DailyForecast { get; set; }
        public List<ForecastItem> HourlyForecast { get; set; }
        public List<AlertItem> Alerts { get; set; }

        public WeatherData(string weatherDataJson)
        {
            IsWeatherInfoAvailable = false;

            try
            {
                LWJson weatherDataObject = LWJson.Parse(weatherDataJson);

                // check first if weather info is available
                if (weatherDataObject.Contains("flags") == true && weatherDataObject["flags"].IsObject == true)
                {
                    if (weatherDataObject["flags"].Contains("darksky-unavailable") == true)
                        return;
                }

                // load current condition
                if (weatherDataObject.Contains("currently") == true && weatherDataObject["currently"].IsObject == true)
                {
                    CurrentForecast = new CurrentItem()
                    {
                        TimestampEpoch = weatherDataObject["currently"]["time"].AsString(),
                        Icon = weatherDataObject["currently"]["icon"].AsString(),
                        Temperature = weatherDataObject["currently"]["temperature"].AsString(),
                        Description = weatherDataObject["currently"]["summary"].AsString(),
                        FeelsLike = weatherDataObject["currently"]["apparentTemperature"].AsString(),
                        Humidity = weatherDataObject["currently"]["humidity"].AsString(),
                        DewPoint = weatherDataObject["currently"]["dewPoint"].AsString(),
                        WindSpeed = weatherDataObject["currently"]["windSpeed"].AsString(),
                        WindDirection = weatherDataObject["currently"].Contains("windBearing") == true ? weatherDataObject["currently"]["windBearing"].AsString() : null,
                        UvIndex = weatherDataObject["currently"]["uvIndex"].AsString(),
                        Pressure = weatherDataObject["currently"]["pressure"].AsString()
                    };
                }

                // load daily forecast 
                if (weatherDataObject.Contains("daily") == true && weatherDataObject["daily"].IsObject == true)
                {
                    if (weatherDataObject["daily"]["data"].IsArray == true && weatherDataObject["daily"]["data"].AsArray().Count > 0)
                    {
                        LWJsonArray dailyData = weatherDataObject["daily"]["data"].AsArray();

                        DailyForecast = new List<ForecastItem>();
                        for (int i = 0; i < dailyData.Count; i++)
                        {
                            // DarkSky quirk for daily data: Treat "partly-cloudy-night" as an alias for "clear-day"
                            string icon = dailyData[i]["icon"].AsString();
                            string condition = dailyData[i]["summary"].AsString();

                            DailyForecast.Add(new ForecastItem()
                            {
                                TimestampEpoch = dailyData[i]["time"].AsString(),
                                Icon = (icon.Equals("partly-cloudy-night", StringComparison.InvariantCultureIgnoreCase) == true) ? "clear-day" : icon,
                                Condition = condition,
                                LowTemp = dailyData[i]["temperatureLow"].AsString(),
                                HighTemp = dailyData[i]["temperatureHigh"].AsString(),
                                Pop = dailyData[i]["precipProbability"].AsString()
                            });
                        }
                    }
                }

                // load hourly forecast for the next 36 hours
                if (weatherDataObject.Contains("hourly") == true && weatherDataObject["hourly"].IsObject == true)
                {
                    if (weatherDataObject["hourly"]["data"].IsArray == true && weatherDataObject["hourly"]["data"].AsArray().Count > 0)
                    {
                        LWJsonArray hourlyData = weatherDataObject["hourly"]["data"].AsArray();

                        HourlyForecast = new List<ForecastItem>();
                        for (int i = 1; i < 37; i++) // only grab 36 hours, skipping first one which is the current hour
                        {
                            HourlyForecast.Add(new ForecastItem()
                            {
                                TimestampEpoch = hourlyData[i]["time"].AsString(),
                                Icon = hourlyData[i]["icon"].AsString(),
                                Condition = hourlyData[i]["summary"].AsString(),
                                LowTemp = hourlyData[i]["temperature"].AsString(), // no low temperature in the API
                                HighTemp = hourlyData[i]["temperature"].AsString(),
                                Pop = hourlyData[i]["precipProbability"].AsString()
                            });
                        }
                    }
                }

                // load alerts data
                if (weatherDataObject.Contains("alerts") == true && weatherDataObject["alerts"].IsArray == true)
                {
                    if (weatherDataObject["alerts"].AsArray().Count > 0)
                    {
                        LWJsonArray alertsData = weatherDataObject["alerts"].AsArray();

                        Alerts = new List<AlertItem>();
                        for (int i = 0; i < alertsData.Count; i++)
                        {
                            Alerts.Add(new AlertItem()
                            {
                                Caption = alertsData[i]["title"].AsString(),
                                Type = alertsData[i]["severity"].AsString(),
                                StartDateTime = alertsData[i]["time"].AsString(),
                                ExpireDateTime = alertsData[i]["expires"].AsString(),
                                Description = alertsData[i]["description"].AsString()
                            });
                        }
                    }
                }

                IsWeatherInfoAvailable = true;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception);
            }
        }

        public class CurrentItem
        {
            public string TimestampEpoch { get; set; }
            public string Icon { get; set; }
            public string Temperature { get; set; }
            public string Description { get; set; }
            public string FeelsLike { get; set; }
            public string Humidity { get; set; }
            public string DewPoint { get; set; }
            public string WindSpeed { get; set; }
            public string WindDirection { get; set; }
            public string UvIndex { get; set; }
            public string Pressure { get; set; }
        }

        public class ForecastItem
        {
            public string TimestampEpoch { get; set; }
            public string Icon { get; set; }
            public string Condition { get; set; }
            public string LowTemp { get; set; }
            public string HighTemp { get; set; }
            public string Pop { get; set; }
        }

        public class AlertItem
        {
            public string Caption { get; set; }
            public string Type { get; set; }
            public string StartDateTime { get; set; }
            public string ExpireDateTime { get; set; }
            public string Description { get; set; }
        }
    }
}