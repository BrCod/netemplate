using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Text.Json;

namespace Netemplate.Api.Middleware.Localization;

/// <summary>
/// Middleware that intercepts problem+json responses and localizes error messages
/// based on the Accept-Language header or query string culture parameter.
/// </summary>
public class LocalizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IStringLocalizer<LocalizationMiddleware> _localizer;
    private readonly ILogger<LocalizationMiddleware> _logger;

    public LocalizationMiddleware(
        RequestDelegate next,
        IStringLocalizer<LocalizationMiddleware> localizer,
        ILogger<LocalizationMiddleware> logger)
    {
        _next = next;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Capture the original response body stream
        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            // Call the next middleware
            await _next(context);

            // Only process problem+json responses
            if (context.Response.ContentType?.Contains("application/problem+json") == true)
            {
                await LocalizeResponseAsync(context, responseBody, originalBodyStream);
            }
            else
            {
                // Copy response as-is for non-problem responses
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LocalizeResponseAsync(HttpContext context, MemoryStream responseBody, Stream originalBodyStream)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(responseBody).ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(responseText))
        {
            return;
        }

        try
        {
            // Parse the ProblemDetails JSON
            var problemDetails = JsonSerializer.Deserialize<Dictionary<string, object>>(responseText);
            if (problemDetails == null)
            {
                await WriteOriginalResponseAsync(responseText, originalBodyStream);
                return;
            }

            // Get the current culture from request
            var culture = context.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture
                          ?? CultureInfo.CurrentCulture;

            _logger.LogDebug("Localizing problem+json response for culture: {Culture}", culture.Name);

            // Localize title if present
            if (problemDetails.TryGetValue("title", out var titleObj) && titleObj is JsonElement titleElement)
            {
                var title = titleElement.GetString();
                if (!string.IsNullOrEmpty(title))
                {
                    var localizedTitle = GetLocalizedMessage(title, culture);
                    problemDetails["title"] = localizedTitle;
                }
            }

            // Localize detail if present
            if (problemDetails.TryGetValue("detail", out var detailObj) && detailObj is JsonElement detailElement)
            {
                var detail = detailElement.GetString();
                if (!string.IsNullOrEmpty(detail))
                {
                    var localizedDetail = GetLocalizedMessage(detail, culture);
                    problemDetails["detail"] = localizedDetail;
                }
            }

            // Add culture info to extensions
            if (problemDetails.TryGetValue("extensions", out var extensionsObj))
            {
                if (extensionsObj is JsonElement extensionsElement)
                {
                    var extensions = JsonSerializer.Deserialize<Dictionary<string, object>>(extensionsElement.GetRawText());
                    if (extensions != null)
                    {
                        extensions["culture"] = culture.Name;
                        problemDetails["extensions"] = extensions;
                    }
                }
            }
            else
            {
                problemDetails["extensions"] = new Dictionary<string, string>
                {
                    ["culture"] = culture.Name
                };
            }

            // Write localized response
            var localizedJson = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            await WriteOriginalResponseAsync(localizedJson, originalBodyStream);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse problem+json response for localization");
            await WriteOriginalResponseAsync(responseText, originalBodyStream);
        }
    }

    private string GetLocalizedMessage(string message, CultureInfo culture)
    {
        // Try to get localized version using the localizer
        // The localizer will look for resource files with the message as key
        var localized = _localizer[message];

        // If resource not found, return original message
        return localized.ResourceNotFound ? message : localized.Value;
    }

    private static async Task WriteOriginalResponseAsync(string content, Stream stream)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        await stream.WriteAsync(bytes);
    }
}

/// <summary>
/// Extension methods for registering localization middleware.
/// </summary>
public static class LocalizationMiddlewareExtensions
{
    /// <summary>
    /// Adds localization support for problem+json responses.
    /// </summary>
    public static IApplicationBuilder UseLocalization(this IApplicationBuilder app)
    {
        app.UseRequestLocalization();
        app.UseMiddleware<LocalizationMiddleware>();
        return app;
    }

    /// <summary>
    /// Configures localization services with supported cultures.
    /// </summary>
    public static IServiceCollection AddApiLocalization(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("es-ES"),
                new CultureInfo("fr-FR"),
                new CultureInfo("de-DE"),
                new CultureInfo("ja-JP"),
                new CultureInfo("zh-CN")
            };

            options.DefaultRequestCulture = new RequestCulture("en-US");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;

            // Priority: Query string > Cookie > Accept-Language header
            options.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                new QueryStringRequestCultureProvider { QueryStringKey = "culture", UIQueryStringKey = "ui-culture" },
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider()
            };
        });

        return services;
    }
}
