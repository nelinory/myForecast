using System;
using System.IO;

namespace myForecast
{
    public static class Logger
    {
        private static readonly object _lock = new object();

        public static void LogInformation(string formatPattern, params object[] arguments)
        {
            LogInformation(String.Format(formatPattern, arguments));
        }

        public static void LogInformation(string message)
        {
            WriteToLog("Information", message);
        }

        public static void LogWarning(string formatPattern, params object[] arguments)
        {
            LogWarning(String.Format(formatPattern, arguments));
        }

        public static void LogWarning(string message)
        {
            WriteToLog("Warning", message);
        }

        public static void LogError(string formatPattern, params object[] arguments)
        {
            LogError(String.Format(formatPattern, arguments));
        }

        public static void LogError(Exception exception)
        {
            LogError(String.Format("{0}{1}Stack:{2}", exception.Message,Environment.NewLine, exception.StackTrace));
        }

        public static void LogError(string message)
        {
            WriteToLog("Error", message);
        }

        private static void WriteToLog(string messageType, string message)
        {
            string logFileName = String.Format("myForecastLog_{0:yyyy_MM_dd}.txt", DateTime.Now);
            string formattedMessage = String.Format("{0}\t\t{1}\t\t{2}{3}",
                                                    DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"),
                                                    messageType,
                                                    message,
                                                    Environment.NewLine);

            lock (_lock)
            {
                try
                {
                    CleanupLogFiles();

                    File.AppendAllText(Configuration.Instance.ConfigFileFolder + @"\" + logFileName, formattedMessage);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private static void CleanupLogFiles()
        {
            int retries = 0;
            bool done = false;
            string[] logFiles = Directory.GetFiles(Configuration.Instance.ConfigFileFolder, "myForecastLog_*");

            foreach (string logFile in logFiles)
            {
                while (done == false)
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(logFile);

                        // delete all log files older than a week
                        if (fileInfo.LastAccessTime < DateTime.Now.AddDays(-7))
                            fileInfo.Delete();

                        done = true;
                    }
                    catch (Exception)
                    {
                        retries++;
                        System.Threading.Thread.Sleep(500);
                        // if we reached max attempt give up
                        if (retries == 7)
                            done = true;
                    }
                }
            }
        }
    }
}