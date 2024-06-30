## What is myForecast?
![GitHub release](https://img.shields.io/github/v/release/nelinory/myForecast)
![GitHub downloads](https://img.shields.io/github/downloads/nelinory/myForecast/total)
![GitHub issues](https://img.shields.io/github/issues/nelinory/myForecast)
![Github license](https://img.shields.io/github/license/nelinory/myForecast)

myForecast is a free Windows Media Center weather application.

## Credits
- All weather icons are provided by d3stroy (https://www.deviantart.com/d3stroy - weezle icon set).
- Weather data is provided by OpenWeather ® API - https://openweathermap.org/api (Worldwide).
- Uv index data is provided by CurrentUvIndex® API - https://currentuvindex.com/api (Worldwide).
- Weather alert data is provided by NWS® API - https://www.weather.gov/documentation/services-web-api (USA Only).

## Requirements
- Windows Media Center 7/8
- Internet connection
- Api key from OpenWeather ® API with an active subscription for **Free plan**
- Location coordinates from OpenWeather ® API
- Windows Media Center **must support Tls 1.2 security protocol**.<br/>If you receive: *"Tls v1.2 protocol support is required."* message when running myForecast for first time, please follow these articles to resolve the issue:
  - Main patch: https://support.microsoft.com/kb/3154518
  - Alternative patch: https://support.microsoft.com/kb/3140245

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

For example when searching for "New York, US" OpenWeather will show two results: New York City, US and New York, US.
You need the **geo coords** which are on the right side of each search result, for New York City, US they are: **40.714,-74.006**.
When you configure myForecast, please enter the full location coordinates, i.e. **40.714,-74.006**

## Screenshots
<img src="https://user-images.githubusercontent.com/15143882/81483585-e05df580-9204-11ea-9fda-582552cbc3f0.png" alt="Daily forecast" width="914"/><br/>
- Weather alerts (if present for your location) can be viewed by clicking on the exclamation point icon.
- Use the location name configuration settings to setup a custom location name.

<img src="https://user-images.githubusercontent.com/15143882/81483592-e6ec6d00-9204-11ea-9e82-aa699960b4f0.png" alt="Hourly forecast" width="914"/><br/>
- Current weather condition panel acts as a button, when clicked the daily forecast will be replaced by hourly forecast weather data. You can use the left/right button on the remote control to scroll through the data. To go back to the default layout just click again on the current weather condition panel. The scheduled data refresh will also restore the default layout.

## Feedback
Any comments, questions and ideas are greatly appreciated.
All application generated files (configuration/location cache and error logs) can be found at: C:\Users\<YOUR USER NAME>\Documents\myForecast
