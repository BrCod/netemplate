using System.Globalization;

namespace Netemplate.Api.Swagger.Localization;

/// <summary>
/// Service for localizing OpenAPI/Swagger documentation elements.
/// </summary>
public interface ISwaggerLocalizer
{
    /// <summary>
    /// Gets the localized API title.
    /// </summary>
    string GetTitle(CultureInfo? culture = null);

    /// <summary>
    /// Gets the localized API description.
    /// </summary>
    string GetDescription(CultureInfo? culture = null);

    /// <summary>
    /// Gets the localized terms of service.
    /// </summary>
    string GetTermsOfService(CultureInfo? culture = null);

    /// <summary>
    /// Gets the localized contact name.
    /// </summary>
    string GetContactName(CultureInfo? culture = null);

    /// <summary>
    /// Gets the localized contact email.
    /// </summary>
    string GetContactEmail(CultureInfo? culture = null);

    /// <summary>
    /// Gets the localized license name.
    /// </summary>
    string GetLicenseName(CultureInfo? culture = null);

    /// <summary>
    /// Gets all supported cultures for API documentation.
    /// </summary>
    IReadOnlyList<CultureInfo> GetSupportedCultures();
}
