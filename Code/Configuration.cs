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

        public readonly string WeatherFileNamePattern = "wd_{0}_{1}_{2}.dat";
        public readonly string WeatherAlertsFileNamePattern = "wda_{0}_{1}_{2}.dat";
        public readonly string WeatherProviderApiUrlPattern = "https://api.openweathermap.org/data/2.5/onecall?lat={0}&lon={1}&appid={2}&lang={3}&units={4}";
        public readonly string WeatherAlertsProviderApiUrlPattern = "https://api.weather.gov/alerts/active?point={0}";
        public readonly string ConfigFileFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), "myForecast");

        public string ApiKey;
        public string LocationCode;
        public string LocationName;
        public WeatherUnit? WeatherUnit;
        public int? RefreshRateInMinutes;
        public ClockTimeFormat? ClockTimeFormat;
        public Language? Language;
        public string Version;

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
            // does the myForecast configuration folder exists ?
            if (Directory.Exists(ConfigFileFolder) == false)
                Directory.CreateDirectory(ConfigFileFolder);

            // try to load an existing configuration if available
            if (Load() == false)
            {
                // load default values
                ApiKey = "YourApiKey";
                LocationCode = "40.7309,-73.9872";
                LocationName = "New York, NY";
                WeatherUnit = myForecast.WeatherUnit.Imperial;
                RefreshRateInMinutes = 10; // default 10 minutes
                ClockTimeFormat = myForecast.ClockTimeFormat.Hours12;
                Language = myForecast.Language.en;
                Version = String.Empty;

                Save();
            }
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

                XmlNode locationNameNode = xmlDocument.CreateElement("LocationName");
                locationNameNode.InnerText = LocationName;
                rootNode.AppendChild(locationNameNode);

                XmlNode weatherUnitNode = xmlDocument.CreateElement("WeatherUnit");
                weatherUnitNode.InnerText = WeatherUnit.ToString();
                rootNode.AppendChild(weatherUnitNode);

                XmlNode refreshRateInMinutesNode = xmlDocument.CreateElement("RefreshRateInMinutes");
                refreshRateInMinutesNode.InnerText = RefreshRateInMinutes.ToString();
                rootNode.AppendChild(refreshRateInMinutesNode);

                XmlNode clockTimeFormatNode = xmlDocument.CreateElement("ClockTimeFormat");
                clockTimeFormatNode.InnerText = ClockTimeFormat.ToString();
                rootNode.AppendChild(clockTimeFormatNode);

                XmlNode languageNode = xmlDocument.CreateElement("Language");
                languageNode.InnerText = Language.ToString();
                rootNode.AppendChild(languageNode);

                XmlNode versionNode = xmlDocument.CreateElement("Version");
                versionNode.InnerText = Version;
                rootNode.AppendChild(versionNode);

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
                        ApiKey = GetXmlNodeValue(root, "ApiKey") ?? "YourApiKey";
                        LocationCode = GetXmlNodeValue(root, "LocationCode") ?? "40.7309,-73.9872";
                        LocationName = GetXmlNodeValue(root, "LocationName") ?? "New York, NY";
                        WeatherUnit = (WeatherUnit)Enum.Parse(typeof(WeatherUnit), GetXmlNodeValue(root, "WeatherUnit") ?? myForecast.WeatherUnit.Imperial.ToString());
                        RefreshRateInMinutes = Int32.Parse(GetXmlNodeValue(root, "RefreshRateInMinutes") ?? "10");
                        ClockTimeFormat = (ClockTimeFormat)Enum.Parse(typeof(ClockTimeFormat), GetXmlNodeValue(root, "ClockTimeFormat") ?? myForecast.ClockTimeFormat.Hours12.ToString());
                        Language = (Language)Enum.Parse(typeof(Language), GetXmlNodeValue(root, "Language") ?? myForecast.Language.en.ToString(), true);
                        Version = GetXmlNodeValue(root, "Version") ?? String.Empty;

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

        private string GetXmlNodeValue(XmlNode rootNode, string nodeName)
        {
            string nodeValue = null;

            XmlNode xmlNode = rootNode.SelectSingleNode(nodeName);
            if (xmlNode != null && String.IsNullOrEmpty(xmlNode.InnerText) == false)
                nodeValue = xmlNode.InnerText;

            return nodeValue;
        }
    }
}