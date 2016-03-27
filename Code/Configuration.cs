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

        public readonly string WeatherFileNamePattern = "wu_{0}_{1}.xml";
        public readonly string ApiUrlPattern = "http://api.wunderground.com/api/{0}/lang:{1}/conditions/alerts/hourly/forecast7day/q/{2}.xml";
        public readonly string ConfigFileFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), "myForecast");

        public string ApiKey;
        public string LocationCode;
        public WeatherUnit? WeatherUnit;
        public int? RefreshRateInMinutes;
        public ClockTimeFormat? ClockTimeFormat;
        public Language? Language;

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
                ApiKey = String.Empty;
                LocationCode = String.Empty;
                WeatherUnit = myForecast.WeatherUnit.Imperial;
                RefreshRateInMinutes = 10; // default 10 minutes
                ClockTimeFormat = myForecast.ClockTimeFormat.Hours12;
                Language = myForecast.Language.EN;

                Save();
            }
        }

        public bool IsValid()
        {
            // main configuration items must present
            if (String.IsNullOrEmpty(ApiKey) == true) return false;
            if (String.IsNullOrEmpty(LocationCode) == true) return false;
            if (WeatherUnit.HasValue == false) return false;
            if (ClockTimeFormat.HasValue == false) return false;
            if (RefreshRateInMinutes.HasValue == false) return false;
            if (Language.HasValue == false) return false;

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

                XmlNode languageNode = xmlDocument.CreateElement("Language");
                languageNode.InnerText = Language.ToString();
                rootNode.AppendChild(languageNode);

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
                        ApiKey = GetXmlNodeValue(root, "ApiKey") ?? String.Empty;
                        LocationCode = GetXmlNodeValue(root, "LocationCode") ?? String.Empty;
                        WeatherUnit = (WeatherUnit)Enum.Parse(typeof(WeatherUnit),
                                                              GetXmlNodeValue(root, "WeatherUnit") ?? myForecast.WeatherUnit.Imperial.ToString());
                        RefreshRateInMinutes = Int32.Parse(GetXmlNodeValue(root, "RefreshRateInMinutes") ?? "10");
                        ClockTimeFormat = (ClockTimeFormat)Enum.Parse(typeof(ClockTimeFormat),
                                                                      GetXmlNodeValue(root, "ClockTimeFormat") ?? myForecast.ClockTimeFormat.Hours12.ToString());
                        Language = (Language)Enum.Parse(typeof(Language),
                                                        GetXmlNodeValue(root, "Language") ?? myForecast.Language.EN.ToString());

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