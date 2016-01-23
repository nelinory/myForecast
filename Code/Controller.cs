using Microsoft.MediaCenter.UI;

namespace myForecast
{
    /// <summary>
    /// Command that changes page when invoked.
    /// </summary>
	public class NavigateCommand : Command
    {
        /// <summary>
        /// Gets or sets the page to change to.
        /// </summary>
		public string Page { get; set; }

        /// <summary>
        /// Changes the page.
        /// </summary>
		protected override void OnInvoked()
        {
            base.OnInvoked();

            if (Page.Equals("Settings"))
            {
                MyAddIn.Instance.GoSettingsPage();
            }
            else if (Page.Equals("About"))
            {
                MyAddIn.Instance.GoAboutPage();
            }
            else if (Page.Equals("WeatherAlert"))
            {
                MyAddIn.Instance.GoWeatherAlertPage();
            }
        }
    }
}
