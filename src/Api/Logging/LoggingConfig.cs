using Microsoft.Extensions.Logging;

namespace Api.Logging
{
    public static class LoggingConfig
    {
        public static void Configure(ILoggingBuilder builder)
        {
            builder.ClearProviders();
            builder.AddConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
            });
            builder.AddFilter((category, level) => level >= LogLevel.Information);
        }
    }
}
