﻿myForecast Version 1.1.1
Copyright © 2016-2020 by Chavdar Kostov


What is myForecast?
===================
myForecast is a free Windows Media Center weather application. Its use is governed by MIT License.
Powered by OpenWeather ® API. All weather icons are provided by d3stroy (https://www.deviantart.com/d3stroy - weezle icon set).
For more information regarding the OpenWeather ® API, please visit https://openweathermap.org/api.


Requirements
============
- Windows Media Center 7/8
- Internet connection
- Api key from OpenWeather ® API
- Location coordinates from OpenWeather ® API
- Windows Media Center must support Tls 1.2 security protocol.
  If you receive: "Tls v1.2 protocol support is required." message when running myForecast for first time,
  please follow this article to resolve the issue: https://support.microsoft.com/en-us/help/3154518


Limitations
===========
- Only 16:9 screen layout (widescreen) is supported at the moment
- Single weather location only


How to get the API key from OpenWeather ® API
=============================================
The application is designed to use a free OpenWeather ® API account which limits you to 1000 calls per day.
To prevent the customer from going over the limit the minimum weather refresh interval is set to 5 minutes at the application level.
Of course if you already have an API key (free or paid) from OpenWeather ® API you can use it too.

1. Go to https://home.openweathermap.org/users/sign_up
2. Fill in the "Create New Account" form, agree to the Terms of Service, fill the captcha and click "Create Account" button
3. Check the email address you used in Step 2 for the activation email from OpenWeather ® API (OWM Team <robot@openweathermap.org>) and click on the "Verify your email" link.
4. Go to https://home.openweathermap.org/users/sign_in
5. After login you will be redirected to the main account page. If all is good you will be given an API key (secret key) which looks like: ma80b279fake22ma80b279fake22
   and can be found in "API Keys" section (you will also receive the API key in an email).
6. It takes up to an hour for the API key to be activated after initial generation.


How to get the location coordinates from OpenWeather ® API
==========================================================
Go to https://openweathermap.org/ - in most of the cases (or at least for US) your location will be automatically detected and shown.
in the location box. In case it is not the one you are looking for, please use the location box to find the correct location.

For example when searching for "New York, US" OpenWeather will show two results: New York City, US and New York, US.
You need the geo coords which are under each search result, for New York City, US they are: 40.7143,-74.006.
When you configure myForecast, please enter the full location coordinates, i.e. 40.7143,-74.006


Feedback
========
Any comments, questions and ideas are greatly appreciated.
All application generated files (configuration/location cache and error logs) can be found at: C:\Users\<YOUR USER NAME>\Documents\myForecast


Author
======
Chavdar Kostov <chavdar.kostov@gmail.com>