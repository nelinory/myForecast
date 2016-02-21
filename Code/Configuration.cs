using System;
using System.IO;
using System.Xml;

namespace myForecast
{
    public sealed class Configuration
    {
        #region Private Properties

        private static volatile Configuration instance;
        private static object _lock = new Object();

        #endregion

        #region Public Properties

        public readonly string WeatherFileNamePattern = "wu_{0}.xml";
        public readonly string ApiUrlPattern = "http://api.wunderground.com/api/{0}/conditions/alerts/hourly/forecast7day/q/{1}.xml";
        public readonly string ConfigFileFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), "myForecast");

        public string ApiKey;
        public string LocationCode;
        public WeatherUnit? WeatherUnit;
        public int? RefreshRateInMinutes;
        public ClockTimeFormat? ClockTimeFormat;

        public static Configuration Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (_lock)
                    {
                        if (instance == null)
                            instance = new Configuration();
                    }
                }

                return instance;
            }
        }

        #endregion

        private Configuration()
        {
            // does the myForecast config folder exists ?
            if (Directory.Exists(ConfigFileFolder) == false)
                Directory.CreateDirectory(ConfigFileFolder);

            // try to load an existing config if available
            if (Load() == false)
            {
                // load default values
                ApiKey = String.Empty;
                LocationCode = String.Empty;
                WeatherUnit = myForecast.WeatherUnit.Imperial;
                RefreshRateInMinutes = 10; // default 10 minutes
                ClockTimeFormat = myForecast.ClockTimeFormat.Hours12;

                Save();
            }
        }

        public bool IsValid()
        {
            // main config items must present
            if (String.IsNullOrEmpty(ApiKey) == true) return false;
            if (String.IsNullOrEmpty(LocationCode) == true) return false;
            if (WeatherUnit.HasValue == false) return false;
            if (ClockTimeFormat.HasValue == false) return false;
            if (RefreshRateInMinutes.HasValue == false) return false;

            return true;
        }

        public bool Save()
        {
            bool configSaved = false;
            string configFileFullName = Path.Combine(ConfigFileFolder, "Config.xml");

            try
            {
                XmlDocument xmlDocument = new XmlDocument();

                XmlNode rootNode = xmlDocument.CreateElement("Configuration");
                xmlDocument.AppendChild(rootNode);

                XmlNode apiKeyNode = xmlDocument.CreateElement("ApiKey");
                apiKeyNode.InnerText = ApiKey;
                rootNode.AppendChild(apiKeyNode);

                XmlNode locationCodeNode = xmlDocument.CreateElement("LocationCode");
                locationCodeNode.InnerText = LocationCode;
                rootNode.AppendChild(locationCodeNode);

                XmlNode weatherUnitNode = xmlDocument.CreateElement("WeatherUnit");
                weatherUnitNode.InnerText = WeatherUnit.ToString();
                rootNode.AppendChild(weatherUnitNode);

                XmlNode refreshRateInMinutesNode = xmlDocument.CreateElement("RefreshRateInMinutes");
                refreshRateInMinutesNode.InnerText = RefreshRateInMinutes.ToString();
                rootNode.AppendChild(refreshRateInMinutesNode);

                XmlNode clockTimeFormatNode = xmlDocument.CreateElement("ClockTimeFormat");
                clockTimeFormatNode.InnerText = ClockTimeFormat.ToString();
                rootNode.AppendChild(clockTimeFormatNode);

                // using the xmlTextWriter to get the nice formatting of the xml file
                XmlTextWriter xmlTextWriter = new XmlTextWriter(configFileFullName, System.Text.Encoding.UTF8);
                xmlTextWriter.Formatting = Formatting.Indented;
                xmlDocument.Save(xmlTextWriter);

                configSaved = true;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception);
            }

            return configSaved;
        }

        private bool Load()
        {
            bool configLoaded = false;
            string configFileFullName = Path.Combine(ConfigFileFolder, "Config.xml");

            if (File.Exists(configFileFullName) == true)
            {
                try
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(configFileFullName);

                    XmlNode root = xmlDocument.SelectSingleNode("Configuration");
                    if (root != null && root.HasChildNodes == true)
                    {
                        ApiKey = root.SelectSingleNode("ApiKey").InnerText;
                        LocationCode = root.SelectSingleNode("LocationCode").InnerText;
                        WeatherUnit = (WeatherUnit)Enum.Parse(typeof(WeatherUnit), root.SelectSingleNode("WeatherUnit").InnerText);
                        RefreshRateInMinutes = Int32.Parse(root.SelectSingleNode("RefreshRateInMinutes").InnerText);
                        ClockTimeFormat = (ClockTimeFormat)Enum.Parse(typeof(ClockTimeFormat), root.SelectSingleNode("ClockTimeFormat").InnerText);

                        configLoaded = true;
                    }
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception);
                }
            }

            return configLoaded;
        }
    }
}