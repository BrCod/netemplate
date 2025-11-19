using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Netemplate.Api.Logging;

public sealed class PiiRedactionProcessor : ILoggerProvider
{
    private static readonly Regex EmailPattern = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex SsnPattern = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex CreditCardPattern = new(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", RegexOptions.Compiled);

    public ILogger CreateLogger(string categoryName) => new RedactingLogger(categoryName);

    public void Dispose() { }

    private sealed class RedactingLogger : ILogger
    {
        private readonly string _categoryName;

        public RedactingLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var redacted = RedactPii(message);
            
            // Forward to actual console logger (simplified; in production use ILoggerFactory)
            Console.WriteLine($"[{logLevel}] {_categoryName}: {redacted}");
        }

        private static string RedactPii(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;

            message = EmailPattern.Replace(message, "[EMAIL_REDACTED]");
            message = PhonePattern.Replace(message, "[PHONE_REDACTED]");
            message = SsnPattern.Replace(message, "[SSN_REDACTED]");
            message = CreditCardPattern.Replace(message, "[CC_REDACTED]");

            return message;
        }
    }
}
