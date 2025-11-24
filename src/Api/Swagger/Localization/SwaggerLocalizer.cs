using System.Globalization;
using System.Resources;

namespace Netemplate.Api.Swagger.Localization;

/// <summary>
/// Default implementation of Swagger localization using resource files.
/// Supports English (en-US) and Turkish (tr-TR) by default.
/// </summary>
public sealed class SwaggerLocalizer : ISwaggerLocalizer
{
    private readonly ResourceManager _resourceManager;
    private static readonly CultureInfo[] SupportedCultures = new[]
    {
        new CultureInfo("en-US"),
        new CultureInfo("tr-TR")
    };

    public SwaggerLocalizer()
    {
        _resourceManager = new ResourceManager(
            "Netemplate.Api.Swagger.Localization.SwaggerResources",
            typeof(SwaggerLocalizer).Assembly);
    }

    public string GetTitle(CultureInfo? culture = null)
    {
        return GetString("ApiTitle", culture) ?? "Clean Architecture API";
    }

    public string GetDescription(CultureInfo? culture = null)
    {
        return GetString("ApiDescription", culture) ?? ".NET 8 Clean Architecture API Template";
    }

    public string GetTermsOfService(CultureInfo? culture = null)
    {
        return GetString("TermsOfService", culture) ?? "https://example.com/terms";
    }

    public string GetContactName(CultureInfo? culture = null)
    {
        return GetString("ContactName", culture) ?? "API Support";
    }

    public string GetContactEmail(CultureInfo? culture = null)
    {
        return GetString("ContactEmail", culture) ?? "support@example.com";
    }

    public string GetLicenseName(CultureInfo? culture = null)
    {
        return GetString("LicenseName", culture) ?? "MIT License";
    }

    public IReadOnlyList<CultureInfo> GetSupportedCultures()
    {
        return SupportedCultures;
    }

    private string? GetString(string key, CultureInfo? culture = null)
    {
        var targetCulture = culture ?? CultureInfo.CurrentCulture;
        
        // Try exact culture match
        var value = _resourceManager.GetString(key, targetCulture);
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Fallback to neutral culture (e.g., "tr" from "tr-TR")
        if (!targetCulture.IsNeutralCulture && targetCulture.Parent != CultureInfo.InvariantCulture)
        {
            value = _resourceManager.GetString(key, targetCulture.Parent);
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        // Fallback to English
        return _resourceManager.GetString(key, new CultureInfo("en-US"));
    }
}
