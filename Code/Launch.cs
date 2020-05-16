using Microsoft.MediaCenter;
using Microsoft.MediaCenter.Hosting;
using System.Collections.Generic;

namespace myForecast
{
    public class MyAddIn : IAddInModule, IAddInEntryPoint
    {
        /// <summary>
        /// Gets the singleton instance of the AddIn class.
        /// </summary>
        public static MyAddIn Instance { get; private set; }

        /// <summary>
        /// Gets the page session used by the add-on.
        /// </summary>
        public HistoryOrientedPageSession Session { get; private set; }

        /// <summary>
        /// Gets the AddInHost instance of the add-on.
        /// </summary>
        public AddInHost AddInHost { get; private set; }

        #region IAddInModule Members

        public void Initialize(Dictionary<string, object> appInfo, Dictionary<string, object> entryPointInfo)
        {
            // no code needed here; all init code needs an application thread in which to run
        }

        public void Uninitialize()
        {
            // no cleanup required
        }

        #endregion

        /// <summary>
        /// Default constructor, invoked by Windows Media Center.
        /// </summary>
        public MyAddIn()
        {
            Instance = this;
        }

        /// <summary>
        /// Launches the add-on.
        /// </summary>
        /// <param name="host">Represents the media center host.</param        
        public void Launch(AddInHost host)
        {
            System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            if (host != null && host.ApplicationContext != null)
            {
                host.ApplicationContext.SingleInstance = true;
                AddInHost = host;
            }

#if DEBUG
            AddInHost.MediaCenterEnvironment.Dialog("Attach debugger to ehexthost.exe and hit ok", "myForecast - Debug", DialogButtons.Ok, 100, true);
#endif
            Session = new HistoryOrientedPageSession();

            GoMainPage();
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            Logger.LogError(e.ExceptionObject.ToString());
            AddInHost.ApplicationContext.CloseApplication();
        }

        /// <summary>
        /// Navigates to the main page.
        /// </summary>
        public void GoMainPage()
        {
            if (Session == null)
                Logger.LogInformation("Calling Main Page");
            else
            {
                Dictionary<string, object> properties = new Dictionary<string, object>();
                properties["Session"] = Session;
                Session.GoToPage("resx://MyForecast/MyForecast.Resources/Main", properties);
            }
        }

        /// <summary>
        /// Navigates to the settings page.
        /// </summary>
        public void GoSettingsPage()
        {
            if (Session == null)
                Logger.LogInformation("Calling Settings Page");
            else
            {
                Dictionary<string, object> properties = new Dictionary<string, object>();
                properties["Session"] = Session;
                Session.GoToPage("resx://MyForecast/MyForecast.Resources/Settings", properties);
            }
        }

        /// <summary>
        /// Navigates to the about page
        /// </summary>
        public void GoAboutPage()
        {
            if (Session == null)
                Logger.LogInformation("Calling About Page");
            else
            {
                Dictionary<string, object> properties = new Dictionary<string, object>();
                properties["Session"] = Session;
                Session.GoToPage("resx://MyForecast/MyForecast.Resources/About", properties);
            }
        }

        /// <summary>
        /// Navigates to the weather alert page
        /// </summary>
        public void GoWeatherAlertPage(string weatherAlertText)
        {
            if (Session == null)
                Logger.LogInformation("Calling Weather Alert Page");
            else
            {
                Dictionary<string, object> properties = new Dictionary<string, object>();
                properties["Session"] = Session;
                properties["WeatherAlertText"] = weatherAlertText;
                Session.GoToPage("resx://MyForecast/MyForecast.Resources/WeatherAlert", properties);
            }
        }
    }
}