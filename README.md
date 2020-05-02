## What is myForecast?
myForecast is a free Windows Media Center weather application. Its use is governed by MIT License.
Powered by OpenWeather ® API. All weather icons are provided by d3stroy (https://www.deviantart.com/d3stroy - weezle icon set).
For more information regarding the OpenWeather ® API, please visit https://openweathermap.org/api.

## Requirements
- Windows Media Center 7/8
- Internet connection
- Api key from OpenWeather ® API
- Location coordinates from OpenWeather ® API
> Windows Media Center **must support Tls 1.2 security protocol** - it is required by OpenWeather ® API<br/>
> If you receive: *"Tls v1.2 protocol support is required."* error when start myForecast for first time,<br/>
> please follow this article to resolve the issue: https://support.microsoft.com/en-us/help/3154518

## Limitations
- Only 16:9/16:10 screen layouts (widescreen) are supported at the moment
- Single weather location only

## How to get the API key from OpenWeather ® API
The application is designed to use a free OpenWeather ® API account which limits you to 1000 calls per day. To prevent the customer from going over the limit the minimum weather refresh interval is set to 5 minutes at the application level. Of course if you already have an API key (free or paid) from OpenWeather ® API you can use it too.

1. Go to https://home.openweathermap.org/users/sign_up
2. Fill in the "Create New Account" form, agree to the Terms of Service, fill the captcha and click "Create Account" button
3. Check the email address you used in Step 2 for the activation email from OpenWeather ® API (OWM Team <robot@openweathermap.org>) and click on the "Verify your email" link.
4. Go to https://home.openweathermap.org/users/sign_in
5. After login you will be redirected to the main account page. If all is good you will be given an API key (secret key) which looks like: **ma80b279fake22ma80b279fake22** and can be found in "API Keys" section (you will also receive the API key in an email).
6. **It takes up to an hour** for the API key **to be activated** after initial generation.

## How to get the location coordinates from OpenWeather ® API
Go to https://openweathermap.org/ - in most of the cases (or at least for US) your location will be automatically detected and shown.
in the location box. In case it is not the one you are looking for, please use the location box to find the correct location.

For example if I search for "New York, US" OpenWeather will show two results: New York City, US and New York, US.
You need the **geo coords** which are under each search result, for New York City, US they are: [**40.7143,-74.006**].
When you configure myForecast, please enter the full location coordinates, i.e. **40.7143,-74.006**

## Screenshots
![MyForecastDaily](https://user-images.githubusercontent.com/15143882/55000286-355a9000-4fa0-11e9-825d-425d811667d9.png)
- Weather alerts (if present for your location) can be viewed by clicking on the exclamation point icon.
- Use the location name configuration settings to setup a custom location name.

![MyForecastHourly](https://user-images.githubusercontent.com/15143882/55000294-3be90780-4fa0-11e9-8a2b-07378849a2fc.png)
- Current weather condition panel acts as a button, when clicked the daily forecast will be replaced by hourly forecast weather data (36 hours ahead). You can use the left/right button on the remote control to scroll thru the data. To go back to the default layout just click again on the current weather condition panel. The scheduled data refresh will also restore the default layout.

## Feedback
Any comments, questions and ideas are greatly appreciated.
All application generated files (configuration/location cache and error logs) can be found at: C:\Users\<YOUR USER NAME>\Documents\myForecast
