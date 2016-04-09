using Microsoft.MediaCenter.UI;
using myForecast.Localization;
using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace myForecast
{
    public class LocalizationModel : ModelItem
    {
        private Hashtable _items;

        public LocalizationModel()
        {
            _items = new Hashtable();

            // set the correct language for the UI thread
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Enum.GetName(typeof(Language), Configuration.Instance.Language));

            // load all properties of the LanguageStrings - these are the translated strings
            PropertyInfo[] properties = typeof(LanguageStrings).GetProperties(BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var property in properties)
            {
                // only need the special fields that starts with "ui_"
                if (property.PropertyType.FullName == typeof(String).ToString() || property.Name.StartsWith("ui_") == true)
                {
                    if (_items.ContainsKey(property.Name) == false)
                        _items[property.Name] = LanguageStrings.ResourceManager.GetString(property.Name);
                }
            }
        }

        public Hashtable Items
        {
            get { return _items; }
        }
    }
}
