using System;
using System.IO;

namespace I2VISTools.Tools
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "application.log");

        public static void Log(string message)
        {
            try
            {
                using (var writer = new StreamWriter(LogFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                }
            }
            catch (Exception ex)
            {
                // If logging fails, we silently ignore it to avoid crashing the application.
                Console.WriteLine($"Logging failed: {ex.Message}");
            }
        }
    }
}
