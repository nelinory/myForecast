using System;

namespace myForecast
{
    public enum WeatherUnit
    {
        Metric,
        Imperial
    }

    public enum ClockTimeFormat
    {
        Hours12,
        Hours24
    }

    public enum Language
    {
        en,
        fr
    }

    public enum WeatherValueFormatType
    {
        Temperature,
        Humidity,
        WindSpeed,
        WindDirection,
        UvIndex,
        Pressure,
        Pop
    }

    //public enum PrecipitationType
    //{
    //    None,
    //    Rain,
    //    Snow,
    //    Sleet
    //}

    //public enum IconType
    //{
    //    None,
    //    ClearDay,
    //    ClearNight,
    //    Rain,
    //    Snow,
    //    Sleet,
    //    Wind,
    //    Fog,
    //    Cloudy,
    //    PartlyCloudyDay,
    //    PartlyCloudyNight
    //}
}
