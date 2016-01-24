using Microsoft.MediaCenter.UI;

namespace myForecast
{
    /// <summary>
    /// Controls the navigation to the application pages, allows passing parameters to the target page
    /// </summary>
    public class NavigationController : ModelItem
    {
        public void GoToSettingsPage()
        {
            MyAddIn.Instance.GoSettingsPage();
        }

        public void GoToAboutPage()
        {
            MyAddIn.Instance.GoAboutPage();
        }

        public void GoToWeatherAlertPage(string weatherAlertText)
        {
            MyAddIn.Instance.GoWeatherAlertPage(weatherAlertText);
        }
    }
}
