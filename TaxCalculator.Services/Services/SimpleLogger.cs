using System;
using TaxCalculator.Services.Interfaces;

namespace TaxCalculator.Services.Services
{
    public class SimpleLogger : ILogger
    {
        public void LogDebug(string message)
        {
            Console.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        public void LogError(string message, Exception exception = null)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception}");
            }
        }

        public void LogInformation(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        public void LogWarning(string message)
        {
            Console.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }
    }
}
