using Microsoft.MediaCenter;
using Microsoft.MediaCenter.UI;
using Microsoft.Win32;
using myForecast.Localization;
using System;
using System.Collections.Generic;

namespace myForecast
{
    public class ConfigModel : ModelItem
    {
        #region Private Properties

        private Choice _spinnerWeatherUnits;
        private Choice _spinnerRefreshRateInMinutes;
        private Choice _spinnerClockTimeFormats;
        private Boolean _checkboxShowInStartMenu;
        private Choice _spinnerLanguage;

        #endregion

        #region Public Properties

        public string ApiKey
        {
            get { return Configuration.Instance.ApiKey; }
            set { Configuration.Instance.ApiKey = value; FirePropertyChanged("ApiKey"); }
        }

        public string LocationCode
        {
            get { return Configuration.Instance.LocationCode; }
            set { Configuration.Instance.LocationCode = value; FirePropertyChanged("LocationCode"); }
        }

        public string LocationName
        {
            get { return Configuration.Instance.LocationName; }
            set { Configuration.Instance.LocationName = value; FirePropertyChanged("LocationName"); }
        }

        public Choice WeatherUnit
        {
            get
            {
                int selectedIndex;
                switch (Configuration.Instance.WeatherUnit)
                {
                    case myForecast.WeatherUnit.Imperial:
                        selectedIndex = 1;
                        break;
                    default:
                        selectedIndex = 0;
                        break;
                }
                _spinnerWeatherUnits.ChosenIndex = selectedIndex;

                return _spinnerWeatherUnits;
            }
            set
            {
                WeatherUnit selectedValue;
                switch (value.ChosenIndex)
                {
                    case 1:
                        selectedValue = myForecast.WeatherUnit.Imperial;
                        break;
                    default:
                        selectedValue = myForecast.WeatherUnit.Metric;
                        break;
                }
                Configuration.Instance.WeatherUnit = selectedValue;

                FirePropertyChanged("WeatherUnit");
            }
        }

        public Choice RefreshRateInMinutes
        {
            get
            {
                int selectedIndex;
                switch (Configuration.Instance.RefreshRateInMinutes.ToString())
                {
                    case "10":
                        selectedIndex = 1;
                        break;
                    case "20":
                        selectedIndex = 2;
                        break;
                    case "30":
                        selectedIndex = 3;
                        break;
                    case "40":
                        selectedIndex = 4;
                        break;
                    case "50":
                        selectedIndex = 5;
                        break;
                    case "60":
                        selectedIndex = 6;
                        break;
                    default:
                        selectedIndex = 0; // 5 minutes
                        break;
                }
                _spinnerRefreshRateInMinutes.ChosenIndex = selectedIndex;

                return _spinnerRefreshRateInMinutes;
            }
            set
            {
                int selectedValue;
                switch (value.ChosenIndex)
                {
                    case 1:
                        selectedValue = 10;
                        break;
                    case 2:
                        selectedValue = 20;
                        break;
                    case 3:
                        selectedValue = 30;
                        break;
                    case 4:
                        selectedValue = 40;
                        break;
                    case 5:
                        selectedValue = 50;
                        break;
                    case 6:
                        selectedValue = 60;
                        break;
                    default:
                        selectedValue = 5; // 5 minutes
                        break;
                }
                Configuration.Instance.RefreshRateInMinutes = selectedValue;

                FirePropertyChanged("RefreshRateInMinutes");
            }
        }

        public Choice ClockTimeFormat
        {
            get
            {
                int selectedIndex;
                switch (Configuration.Instance.ClockTimeFormat)
                {
                    case myForecast.ClockTimeFormat.Hours12:
                        selectedIndex = 0;
                        break;
                    default:
                        selectedIndex = 1;
                        break;
                }
                _spinnerClockTimeFormats.ChosenIndex = selectedIndex;

                return _spinnerClockTimeFormats;
            }
            set
            {
                ClockTimeFormat selectedValue;
                switch (value.ChosenIndex)
                {
                    case 0:
                        selectedValue = myForecast.ClockTimeFormat.Hours12;
                        break;
                    default:
                        selectedValue = myForecast.ClockTimeFormat.Hours24;
                        break;
                }
                Configuration.Instance.ClockTimeFormat = selectedValue;

                FirePropertyChanged("ClockTimeFormat");
            }
        }

        public Choice Language
        {
            get
            {
                int selectedIndex;
                switch (Configuration.Instance.Language)
                {
                    case myForecast.Language.en:
                        selectedIndex = 0;
                        break;
                    default:
                        selectedIndex = 1;
                        break;
                }
                _spinnerLanguage.ChosenIndex = selectedIndex;

                return _spinnerLanguage;
            }
            set
            {
                Language selectedValue;
                switch (value.ChosenIndex)
                {
                    case 0:
                        selectedValue = myForecast.Language.en;
                        break;
                    default:
                        selectedValue = myForecast.Language.fr;
                        break;
                }
                Configuration.Instance.Language = selectedValue;

                FirePropertyChanged("Language");
            }
        }

        public Boolean ShowInStartMenu
        {
            get { return _checkboxShowInStartMenu; }
            set { _checkboxShowInStartMenu = value; FirePropertyChanged("ShowInStartMenu"); }
        }

        #endregion

        public ConfigModel()
        {
            // load the WeatherUnits spinner
            if (_spinnerWeatherUnits == null)
            {
                _spinnerWeatherUnits = new Choice();
                List<String> spinnerWeatherUnitsItems = new List<String>();
                spinnerWeatherUnitsItems.Add(LanguageStrings.ui_SettingsMetricUnits);
                spinnerWeatherUnitsItems.Add(LanguageStrings.ui_SettingsImperialUnits);

                _spinnerWeatherUnits.Options = spinnerWeatherUnitsItems;
            }

            // load the RefreshRateinMinutes spinner
            if (_spinnerRefreshRateInMinutes == null)
            {
                _spinnerRefreshRateInMinutes = new Choice();
                List<String> spinnerRefreshRateInMinutes = new List<String>();
                spinnerRefreshRateInMinutes.Add(String.Format("5 {0}", LanguageStrings.ui_SettingsMinutes));
                spinnerRefreshRateInMinutes.Add(String.Format("10 {0}", LanguageStrings.ui_SettingsMinutes));
                spinnerRefreshRateInMinutes.Add(String.Format("20 {0}", LanguageStrings.ui_SettingsMinutes));
                spinnerRefreshRateInMinutes.Add(String.Format("30 {0}", LanguageStrings.ui_SettingsMinutes));
                spinnerRefreshRateInMinutes.Add(String.Format("40 {0}", LanguageStrings.ui_SettingsMinutes));
                spinnerRefreshRateInMinutes.Add(String.Format("50 {0}", LanguageStrings.ui_SettingsMinutes));
                spinnerRefreshRateInMinutes.Add(String.Format("60 {0}", LanguageStrings.ui_SettingsMinutes));

                _spinnerRefreshRateInMinutes.Options = spinnerRefreshRateInMinutes;
            }

            // load the ClockTimeFormats spinner
            if (_spinnerClockTimeFormats == null)
            {
                _spinnerClockTimeFormats = new Choice();
                List<String> spinnerClockTimeFormats = new List<String>();
                spinnerClockTimeFormats.Add(String.Format("12 {0}", LanguageStrings.ui_SettingsHours));
                spinnerClockTimeFormats.Add(String.Format("24 {0}", LanguageStrings.ui_SettingsHours));

                _spinnerClockTimeFormats.Options = spinnerClockTimeFormats;
            }

            // load the Language spinner
            if (_spinnerLanguage == null)
            {
                _spinnerLanguage = new Choice();
                List<String> spinnerLanguage = new List<String>();
                spinnerLanguage.Add("English");
                spinnerLanguage.Add("Français");

                _spinnerLanguage.Options = spinnerLanguage;
            }

            // load the ShowInStartMenu checkbox state
            _checkboxShowInStartMenu = GetShowInStartMenuRegistryValue();
        }

        public void Save()
        {
            Configuration.Instance.Save();

            // start menu entry point, update if needed
            if (GetShowInStartMenuRegistryValue() != _checkboxShowInStartMenu)
                SetShowInStartMenuRegistryValue();

        }

        #region Show on Start menu functionality

        private readonly string _startMenuAppRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Media Center\Start Menu\Applications\{81ca0920-238a-4dc9-8bfd-93bb0a43c5b8}";

        private bool GetShowInStartMenuRegistryValue()
        {
            object onStartMenuValue = null;

            try
            {
                using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(_startMenuAppRegistryKey))
                {
                    if (registryKey != null)
                        onStartMenuValue = registryKey.GetValue("OnStartMenu");
                    else
                        ShowWindowsMediaCenterDialog(LanguageStrings.ui_DialogStartMenuRegistryKeyNotFound);
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception);
            }

            if (onStartMenuValue != null)
                return Boolean.Parse((string)onStartMenuValue);
            else
                return false;
        }

        private void SetShowInStartMenuRegistryValue()
        {
            try
            {
                using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(_startMenuAppRegistryKey, true))
                {
                    if (registryKey != null)
                    {
                        registryKey.SetValue("OnStartMenu", _checkboxShowInStartMenu, RegistryValueKind.String);
                        ShowWindowsMediaCenterDialog(LanguageStrings.ui_DialogStartMenuChangesRestartNeeded);
                    }
                    else
                        ShowWindowsMediaCenterDialog(LanguageStrings.ui_DialogStartMenuRegistryKeyNotFound);
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception);
            }
        }

        private void ShowWindowsMediaCenterDialog(string message)
        {
            // only show dialogs when running under Media Center
            if (MyAddIn.Instance != null)
            {
                // need this for error dialogs
                MediaCenterEnvironment mcEnvironment = MyAddIn.Instance.AddInHost.MediaCenterEnvironment;

                mcEnvironment.Dialog(message, LanguageStrings.ui_DialogConfigurationSettingsCaption, DialogButtons.Ok, 60, true);
            }
        }

        #endregion
    }
}
