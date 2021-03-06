﻿using System;
using System.Net;

namespace myForecast
{
    public class WebClientWithCompression : WebClient
    {
        public WebClientWithCompression()
        {
            // ensure correct security protocol is allowed
            ServicePointManager.Expect100Continue = true;

            // 0xc0     - Tls 1.0 (obsolete)
            // 0x300    - Tls 1.1 (obsolete)
            // 0xc00    - Tls 1.2 (current)
            // 0x3000   - Tls 1.3 (future - not supported by .NET 2.0 framework)
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc0) | (SecurityProtocolType)(0x300) | (SecurityProtocolType)(0xc00);
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            return request;
        }

        public bool IsTls12Supported()
        {
            bool result = true;

            // used for testing
            // ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc00);

            try
            {
                DownloadString("https://openweathermap.org/api");
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }
    }
}
