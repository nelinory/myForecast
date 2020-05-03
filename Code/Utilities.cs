using System;
using System.IO;

namespace myForecast
{
    public static class Utilities
    {
        internal static DateTime GetTimestampFromEpoch(string epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Double.Parse(epoch)).ToLocalTime();
        }

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
    }
}