using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Api.Logging
{
    /// <summary>
    /// Configuration for application logging.
    /// </summary>
    public static class LoggingConfig
    {
        /// <summary>
        /// Configures the logging builder with console logging and formatting.
        /// </summary>
        /// <param name="builder">The logging builder to configure.</param>
        public static void Configure(ILoggingBuilder builder)
        {
            builder.ClearProviders();
            builder.AddConsole(options =>
            {
                options.FormatterName = ConsoleFormatterNames.Simple;
            });
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
            });
            builder.AddFilter((category, level) => level >= LogLevel.Information);
        }
    }
}
