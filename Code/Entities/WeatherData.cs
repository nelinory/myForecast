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
                    // calculate dewPoint from relative humidity and current air temperature
                    double temp = weatherDataObject["current"]["main"].IsObject == true ? weatherDataObject["current"]["main"]["temp"].AsDouble() : 0;
                    double humidity = weatherDataObject["current"]["main"].IsObject == true ? weatherDataObject["current"]["main"]["humidity"].AsDouble() : 0;
                    string dewPoint = ((237.3 * (Math.Log(humidity / 100) + ((17.27 * temp) / (237.3 + temp)))) / (17.27 - (Math.Log(humidity / 100) + ((17.27 * temp) / (237.3 + temp))))).ToString("F");

                    // TODO: get uv index
                    string uvIndex = "-1";

                    CurrentForecast = new CurrentItem()
                    {
                        TimestampEpoch = weatherDataObject["current"]["dt"].AsInteger().ToString(),
                        IconId = weatherDataObject["current"]["weather"].IsArray == true ? weatherDataObject["current"]["weather"][0]["id"].AsString() : String.Empty,
                        Temperature = weatherDataObject["current"]["main"].IsObject == true ? weatherDataObject["current"]["main"]["temp"].AsDouble().ToString() : String.Empty,
                        FeelsLike = weatherDataObject["current"]["main"].IsObject == true ? weatherDataObject["current"]["main"]["feels_like"].AsDouble().ToString() : String.Empty,
                        Humidity = weatherDataObject["current"]["main"].IsObject == true ? weatherDataObject["current"]["main"]["humidity"].AsDouble().ToString() : String.Empty,
                        DewPoint = dewPoint,
                        WindSpeed = weatherDataObject["current"]["wind"].IsObject == true ? weatherDataObject["current"]["wind"]["speed"].AsDouble().ToString() : String.Empty,
                        WindDirection = weatherDataObject["current"]["wind"].IsObject == true ? weatherDataObject["current"]["wind"]["deg"].AsDouble().ToString() : String.Empty,
                        UvIndex = uvIndex,
                        Pressure = weatherDataObject["current"]["main"].IsObject == true ? weatherDataObject["current"]["main"]["pressure"].AsDouble().ToString() : String.Empty
                    };
                }
                else return;

                // load daily forecast
                if (weatherDataObject.Contains("forecast") == true && weatherDataObject["forecast"]["list"].IsArray == true && weatherDataObject["forecast"]["list"].AsArray().Count > 0)
                {
                    LWJsonArray dailyData = weatherDataObject["forecast"]["list"].AsArray();
                    DateTime dailyDate = DateTime.Now.Date.AddDays(1);

                    DailyForecast = new List<ForecastItem>();
                    for (int i = 0; i < 5; i++) // 5 days forecast
                    {
                        string timestampEpoch = String.Empty;
                        string iconId = String.Empty;
                        double lowTemp = 999;
                        double highTemp = -999;
                        string pop = String.Empty;
                        int dataSliceNumber = 1;

                        for (int c = 0; c < dailyData.Count; c++)
                        {
                            string timestampEpochDay = dailyData[c]["dt"].AsInteger().ToString();

                            // keep the timestamp in UTC for the daily forecast, since we only need the mean value
                            DateTime timestampDay = Utilities.GetUtcTimestampFromEpochKeep(timestampEpochDay);

                            // skip all days until desired date
                            if (dailyDate != timestampDay.Date)
                                continue;

                            // get the min/max temperatures for the day
                            if (dailyData[c]["main"].IsObject == true)
                            {
                                if (lowTemp > dailyData[c]["main"]["temp_min"].AsDouble())
                                    lowTemp = dailyData[c]["main"]["temp_min"].AsDouble();

                                if (highTemp < dailyData[c]["main"]["temp_max"].AsDouble())
                                    highTemp = dailyData[c]["main"]["temp_max"].AsDouble();
                            }

                            //Logger.LogInformation("{0} - {1}: {2}/{3} - {4}",
                            //    timestampDay,
                            //    dataSliceNumber,
                            //    dailyData[c]["main"]["temp_min"].AsDouble(),
                            //    dailyData[c]["main"]["temp_max"].AsDouble(),
                            //    dailyData[c]["weather"][0]["description"].AsString()
                            //    );

                            // select the 5th slice which is around noon
                            if (dataSliceNumber <= 5)
                            {
                                if (dailyData[c].Contains("pop") == true)
                                    pop = dailyData[c]["pop"].AsDouble().ToString();

                                iconId = dailyData[c]["weather"].IsArray == true ? dailyData[c]["weather"][0]["id"].AsString() : String.Empty;
                                timestampEpoch = timestampEpochDay;
                            }

                            dataSliceNumber++;
                        }

                        DailyForecast.Add(new ForecastItem()
                        {
                            TimestampEpoch = timestampEpoch,
                            IconId = iconId,
                            LowTemp = lowTemp.ToString(),
                            HighTemp = highTemp.ToString(),
                            Pop = pop
                        });

                        dailyDate = dailyDate.AddDays(1);
                    }
                }
                else return;

                // load hourly forecast for the next 36 data slices
                if (weatherDataObject.Contains("forecast") == true && weatherDataObject["forecast"]["list"].IsArray == true && weatherDataObject["forecast"]["list"].AsArray().Count > 0)
                {
                    LWJsonArray hourlyData = weatherDataObject["forecast"]["list"].AsArray();
                    int totalDataSlicesAdded = 1;

                    HourlyForecast = new List<ForecastItem>();
                    for (int i = 0; i < hourlyData.Count && totalDataSlicesAdded <= 36; i++)
                    {
                        string timestampEpoch = hourlyData[i]["dt"].AsInteger().ToString();
                        DateTime timestamp = Utilities.GetTimestampFromEpoch(timestampEpoch);

                        // skip all hours until next hour
                        if (DateTime.Now.Ticks >= timestamp.Ticks)
                            continue;

                        string pop = "0";
                        if (hourlyData[i].Contains("pop") == true)
                            pop = hourlyData[i]["pop"].AsDouble().ToString();

                        HourlyForecast.Add(new ForecastItem()
                        {
                            TimestampEpoch = timestampEpoch,
                            IconId = hourlyData[i]["weather"].IsArray == true ? hourlyData[i]["weather"][0]["id"].AsString() : String.Empty,
                            LowTemp = hourlyData[i]["main"].IsObject ? hourlyData[i]["main"]["temp_min"].AsDouble().ToString() : String.Empty,
                            HighTemp = hourlyData[i]["main"].IsObject ? hourlyData[i]["main"]["temp_max"].AsDouble().ToString() : String.Empty,
                            Pop = pop
                        });

                        totalDataSlicesAdded++;
                    }
                }
                else return;

                // load alerts data
                if (weatherDataObject.Contains("alerts") == true && weatherDataObject["alerts"].IsArray == true && weatherDataObject["alerts"].AsArray().Count > 0)
                {
                    LWJsonArray alertsData = weatherDataObject["alerts"].AsArray();

                    Alerts = new List<AlertItem>();
                    for (int i = 0; i < alertsData.Count; i++)
                    {
                        Alerts.Add(new AlertItem()
                        {
                            Caption = alertsData[i]["event"].AsString(),
                            StartDateTime = alertsData[i]["start"].AsString(),
                            ExpireDateTime = alertsData[i]["end"].AsString(),
                            Description = alertsData[i]["description"].AsString()
                        });
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
            public string IconId { get; set; }
            public string Temperature { get; set; }
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
            public string LowTemp { get; set; }
            public string HighTemp { get; set; }
            public string Pop { get; set; }
        }

        public class AlertItem
        {
            public string Caption { get; set; }
            public string StartDateTime { get; set; }
            public string ExpireDateTime { get; set; }
            public string Description { get; set; }
        }
    }
}