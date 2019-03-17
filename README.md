> **Note:**<br/>
> WeatherUnderground discontinued free API accounts as of 03/01/2019. Working on replacement using Dark Sky API.

## What is myForecast?
myForecast is a free Windows Media Center weather application. Its use is governed by MIT License.
Powered by Dark Sky ® API. All weather icons are provided by d3stroy (https://www.deviantart.com/d3stroy - weezle icon set).
For more information regarding the Dark Sky ® API, please visit https://darksky.net/dev.

## Requirements
- Windows Media Center 7/8
- Internet connection
- Api key from Dark Sky ® API

## Limitations
- Only 16:9/16:10 screen layouts (widescreen) are supported at the moment
- Single weather location only

## How to get the API key from Dark Sky ® API
The application is designed to use a free Dark Sky ® API developer account which limits you to 1000 calls per day. To prevent the customer from going over the limit the minimum weather refresh interval is set to 5 minutes at the application level. Of course if you already have an API key (free or paid) from Dark Sky ® API you can use it too.

1. Go to https://darksky.net/dev and click the "TRY FOR FREE" button
2. Fill in the "Register" section, agree to the Terms of Service and click "REGISTER" button
3. Check the email address you used in Step 2 for the activation email from Dark Sky ® API (Dark Sky Developer Support <developer@darksky.net>) and click on the activation link. You have 30 minutes to activate.
4. Go to https://darksky.net/dev and click the "LOG IN" button
5. After login you will be redirected to the main API page. If all is good you will be given an API key (secret key) which looks like: ma80b279fake22ma80b279fake22

## How to get the location coordinates from Dark Sky ® API
Go to https://darksky.net - in most of the cases (or at least for US) your location will be automatically detected and shown
in the location box. In case it is not the one you are looking for, please use the location box to find the correct location.
For example if I search for "New York, NY" I will be redirected to a new page that shows the New York, NY current weather.

Look at your browser address bar and you will see something like: https://darksky.net/forecast/40.7309,-73.9872/us12/en
The location coordinates you need are: **40.7309,-73.9872**.
When you configure myForecast, please enter the full location coordinates, i.e. **40.7309,-73.9872**

## Screenshot
![Screenshot](https://cloud.githubusercontent.com/assets/15143882/13309459/9ab44c96-db42-11e5-8d13-bd3e63df076a.png)


## Feedback
Any comments, questions and ideas are greatly appreciated.
All application generated files (configuration/location cache and error logs) can be found at: C:\Users\<YOUR USER NAME>\Documents\myForecast
