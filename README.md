## What is myForecast?
myForecast is a free Windows Media Center weather application. Its use is governed by MIT License.
Powered by Dark Sky ® API. All weather icons are provided by d3stroy (https://www.deviantart.com/d3stroy - weezle icon set).
For more information regarding the Dark Sky ® API, please visit https://darksky.net/dev.

## Requirements
- Windows Media Center 7/8
- Internet connection
- Api key from Dark Sky ® API
- Location coordinates from Dark Sky ® API
> Windows Media Center **must support Tls 1.2 security protocol** - it is required by Dark Sky ® API<br/>
> If you receive: *"Tls v1.2 protocol support is required."* error when start myForecast for first time,<br/>
> please follow this article to resolve the issue: https://support.microsoft.com/en-us/help/3154518

## Limitations
- Only 16:9/16:10 screen layouts (widescreen) are supported at the moment
- Single weather location only

## How to get the API key from Dark Sky ® API
> Dark Sky ® API company have been acquired by Apple.<br/>
> <strong>As of 04/01/2020 Dark Sky ® API does not accepts new sign ups and will stop working at the end of 2021.</strong>

The application is designed to use a free Dark Sky ® API developer account which limits you to 1000 calls per day. To prevent the customer from going over the limit the minimum weather refresh interval is set to 5 minutes at the application level.

## How to get the location coordinates from Dark Sky ® API
Go to https://darksky.net - in most of the cases (or at least for US) your location will be automatically detected and shown
in the location box. In case it is not the one you are looking for, please use the location box to find the correct location.
For example if I search for "New York, NY" I will be redirected to a new page that shows the New York, NY current weather.

Look at your browser address bar and you will see something like: https://darksky.net/forecast/40.7309,-73.9872/us12/en
The location coordinates you need are: **40.7309,-73.9872**.
When you configure myForecast, please enter the full location coordinates, i.e. **40.7309,-73.9872**

## Screenshots
![MyForecastDaily](https://user-images.githubusercontent.com/15143882/55000286-355a9000-4fa0-11e9-825d-425d811667d9.png)
- Weather alerts (if present for your location) can be viewed by clicking on the exclamation point icon.
- Use the location name configuration settings to setup a custom location name.

![MyForecastHourly](https://user-images.githubusercontent.com/15143882/55000294-3be90780-4fa0-11e9-8a2b-07378849a2fc.png)
- Current weather condition panel acts as a button, when clicked the daily forecast will be replaced by hourly forecast weather data (36 hours ahead). You can use the left/right button on the remote control to scroll thru the data. To go back to the default layout just click again on the current weather condition panel. The scheduled data refresh will also restore the default layout.

## Feedback
Any comments, questions and ideas are greatly appreciated.
All application generated files (configuration/location cache and error logs) can be found at: C:\Users\<YOUR USER NAME>\Documents\myForecast
