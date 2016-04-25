## What is myForecast?
myForecast is a free Windows Media Center weather application. Its use is governed by MIT License.
Powered by Weather Underground ® API. Icons provided by The Weather Channel, LLC weather.com ®.
For more information regarding the Weather Underground ® API, please visit http://www.wunderground.com/weather/api

## Requirements
- Windows Media Center 7/8
- Internet connection
- Api key from WeatherUnderground

## Limitations
- Only 16:9/16:10 screen layouts (widescreen) are supported at the moment
- Single weather location only

## How to get the API key from Weather Underground
The application is designed to use a free Weather Underground developer account which limits you to 500 calls per day/10 calls per minute. To prevent the customer from going over the limit the minimum weather refresh interval is set to 5 minutes at the application level. Of course if you already have an API key (free or paid) from Weather Underground API you can use it too.

1. Go to http://www.wunderground.com/weather/api and click the "Sign Up for FREE!" button
2. Fill in the "Create Your Free Account!" section, agree to the Terms of Service and click "Sign Up" button
3. Check the email address you used in Step 2 for the activation email from Weather Underground and click on the activation link
4. Fill in the "Already have a wunderground.com account?" section and click "Login" button
5. After login you will be redirected to the main API page. Click the "Pricing" link next to the "API Home" link
6. Ensure you have the following plan selected:
	- Customize a plan that suits your needs: CUMULUS PLAN
	- How much will you use our service? DEVELOPER option
	- Your TOTAL on top should be: $0 USD per month
7. Click "Purchase Key" button and fill in the form:
	- Contact name: myForecast
	- Project contact email: the same email you used in Step 2
	- Project name: myForecast
	- Project website: https://github.com/nelinory/myForecast
	- Where will the API be used?: Other
	- Will the API be used for commercial use?: No
	- Will the API be used for manufacturing mobile chip processing?: No
	- What country are you or your company based in?: United States
	- Agree on:
		* I understand that usage of the Weather Underground API requires proper attribution
		* I agree to the Terms of Service
8. Click "Purchase Key" button once ready.
9. If all is good you will be given an API key which looks like: ma80b279fake22

## How to get the location code from Weather Underground
Go to http://www.wunderground.com - in most of the cases (or at least for US) your location will be automatically detected and shown
in the top right corner. In case it is not the one you are looking for, please use the "Search Locations" text box in the same corner.
For example if I search for "New York, NY" I will be redirected to a new page that shows the New York, NY current weather.

Look at your browser address bar and you will see something like: http://www.wunderground.com/q/zmw:10001.5.99999
The location code we need is: zmw:10001.5.99999 (for US locations just using the zip code may be enough).
When you configure myForecast, please enter the full location code, i.e. **zmw:10001.5.99999**

## Screenshot
![Screenshot](https://cloud.githubusercontent.com/assets/15143882/13309459/9ab44c96-db42-11e5-8d13-bd3e63df076a.png)


## Feedback
Any comments, questions and ideas are greatly appreciated.
All application generated files (configuration/location cache and error logs) can be found at: C:\Users\<YOUR USER NAME>\Documents\myForecast
