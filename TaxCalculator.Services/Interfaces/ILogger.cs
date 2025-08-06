using System;

namespace TaxCalculator.Services.Interfaces
{
    public interface ILogger
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message, Exception exception = null);
        void LogDebug(string message);
    }
}
