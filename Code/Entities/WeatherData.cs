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

                // load current condition
                if (weatherDataObject.Contains("current") == true && weatherDataObject["current"].IsObject == true)
                {
                    CurrentForecast = new CurrentItem()
                    {
                        TimestampEpoch = weatherDataObject["current"]["dt"].AsInteger().ToString(),
                        IconId = weatherDataObject["current"]["weather"].IsArray == true ? weatherDataObject["current"]["weather"][0]["id"].AsString() : String.Empty,
                        Temperature = weatherDataObject["current"]["temp"].AsDouble().ToString(),
                        Description = weatherDataObject["current"]["weather"].IsArray == true ? weatherDataObject["current"]["weather"][0]["main"].AsString() : String.Empty,
                        FeelsLike = weatherDataObject["current"]["feels_like"].AsDouble().ToString(),
                        Humidity = weatherDataObject["current"]["humidity"].AsDouble().ToString(),
                        DewPoint = weatherDataObject["current"]["dew_point"].AsDouble().ToString(),
                        WindSpeed = weatherDataObject["current"]["wind_speed"].AsDouble().ToString(),
                        WindDirection = weatherDataObject["current"].Contains("wind_deg") == true ? weatherDataObject["current"]["wind_deg"].AsDouble().ToString() : String.Empty,
                        UvIndex = weatherDataObject["current"]["uvi"].AsDouble().ToString(),
                        Pressure = weatherDataObject["current"]["pressure"].AsDouble().ToString()
                    };
                }
                else return;

                // load daily forecast 
                if (weatherDataObject.Contains("daily") == true && weatherDataObject["daily"].IsArray == true && weatherDataObject["daily"].AsArray().Count > 0)
                {
                    LWJsonArray dailyData = weatherDataObject["daily"].AsArray();

                    DailyForecast = new List<ForecastItem>();
                    for (int i = 0; i < dailyData.Count; i++)
                    {
                        string timestampEpoch = dailyData[i]["dt"].AsInteger().ToString();
                        DateTime timestamp = Utilities.GetTimestampFromEpoch(timestampEpoch);

                        // skip all days until tomorrow
                        if (DateTime.Now.Date >= timestamp.Date)
                            continue;

                        // OpenWeather API provides: "rain precipitation" or "snow volume" in a separate fields - both in mm
                        string pop = "0";
                        if (dailyData[i].Contains("rain") == true)
                            pop = dailyData[i]["rain"].AsDouble().ToString();
                        else if (dailyData[i].Contains("snow") == true)
                            pop = dailyData[i]["snow"].AsDouble().ToString();

                        DailyForecast.Add(new ForecastItem()
                        {
                            TimestampEpoch = timestampEpoch,
                            IconId = dailyData[i]["weather"].IsArray == true ? dailyData[i]["weather"][0]["id"].AsString() : String.Empty,
                            Condition = dailyData[i]["weather"].IsArray == true ? dailyData[i]["weather"][0]["main"].AsString() : String.Empty,
                            LowTemp = dailyData[i]["temp"].IsObject ? dailyData[i]["temp"]["min"].AsDouble().ToString() : String.Empty,
                            HighTemp = dailyData[i]["temp"].IsObject ? dailyData[i]["temp"]["max"].AsDouble().ToString() : String.Empty,
                            Pop = pop
                        });
                    }
                }
                else return;

                // load hourly forecast for the next 36 hours
                if (weatherDataObject.Contains("hourly") == true && weatherDataObject["hourly"].IsArray == true && weatherDataObject["hourly"].AsArray().Count > 0)
                {
                    LWJsonArray hourlyData = weatherDataObject["hourly"].AsArray();
                    int totalHoursAdded = 1;

                    HourlyForecast = new List<ForecastItem>();
                    for (int i = 0; i < hourlyData.Count && totalHoursAdded <= 36; i++)
                    {
                        string timestampEpoch = hourlyData[i]["dt"].AsInteger().ToString();
                        DateTime timestamp = Utilities.GetTimestampFromEpoch(timestampEpoch);

                        // skip all hour until next hour
                        if (DateTime.Now.Ticks >= timestamp.Ticks)
                            continue;

                        HourlyForecast.Add(new ForecastItem()
                        {
                            TimestampEpoch = timestampEpoch,
                            IconId = hourlyData[i]["weather"].IsArray == true ? hourlyData[i]["weather"][0]["id"].AsString() : String.Empty,
                            Condition = hourlyData[i]["weather"].IsArray == true ? hourlyData[i]["weather"][0]["main"].AsString() : String.Empty,
                            LowTemp = hourlyData[i]["temp"].AsString(),     // no low temperature in the OpenWeather API
                            HighTemp = hourlyData[i]["temp"].AsString(),    // no high temperature in the OpenWeather API
                            Pop = "0" // no pop for now
                        });

                        totalHoursAdded++;
                    }
                }
                else return;

                //// load alerts data - no alerts data in OpenWeather API, so we use NWS for USA based weather alerts based on location coordinates
                //// https://github.com/SimpleAppProjects/SimpleWeather-Windows/blob/6a6bb8c1acecae11bd52937ba0abbfb448d7b7c8/SimpleWeather/NWS/NWSAlertProvider.cs
                //LWJson weatherAlertsObject = LWJson.Parse(weatherAlertsJson);
                //if (weatherAlertsObject.Contains("features") == true && weatherAlertsObject["features"].IsArray == true && weatherAlertsObject["features"].AsArray().Count > 0)
                //{
                //    LWJsonArray alertsData = weatherAlertsObject["features"].AsArray();

                //    Alerts = new List<AlertItem>();
                //    for (int i = 0; i < alertsData.Count; i++)
                //    {
                //        if (alertsData[i]["properties"].IsObject == true)
                //        {
                //            Alerts.Add(new AlertItem()
                //            {
                //                Caption = alertsData[i]["properties"]["event"].AsString(),
                //                Type = alertsData[i]["properties"]["severity"].AsString(),
                //                StartDateTime = alertsData[i]["properties"]["effective"].AsString(),
                //                ExpireDateTime = alertsData[i]["properties"]["expires"].AsString(),
                //                Description = alertsData[i]["properties"]["description"].AsString()
                //            });
                //        }
                //    }
                //}

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
            public string IconId { get; set; }
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
            public string IconId { get; set; }
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