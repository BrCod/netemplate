using Microsoft.AspNetCore.Mvc;
using Netemplate.Api.Swagger.Localization;
using System.Globalization;

namespace Netemplate.Api.Controllers;

/// <summary>
/// Provides API documentation metadata in different languages.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class ApiInfoController : ControllerBase
{
    private readonly ISwaggerLocalizer _localizer;

    public ApiInfoController(ISwaggerLocalizer localizer)
    {
        _localizer = localizer;
    }

    /// <summary>
    /// Get API information localized to the current culture.
    /// Use Accept-Language header or ?culture query parameter to change language.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<ApiInformation>(StatusCodes.Status200OK)]
    public IActionResult GetApiInfo()
    {
        var currentCulture = CultureInfo.CurrentCulture;
        
        return Ok(new ApiInformation
        {
            Culture = currentCulture.Name,
            DisplayName = currentCulture.DisplayName,
            Title = _localizer.GetTitle(currentCulture),
            Description = _localizer.GetDescription(currentCulture),
            ContactName = _localizer.GetContactName(currentCulture),
            ContactEmail = _localizer.GetContactEmail(currentCulture),
            LicenseName = _localizer.GetLicenseName(currentCulture),
            SupportedCultures = _localizer.GetSupportedCultures()
                .Select(c => new CultureInfoDto
                {
                    Code = c.Name,
                    DisplayName = c.DisplayName,
                    NativeName = c.NativeName
                })
                .ToList()
        });
    }

    /// <summary>
    /// Get API information in a specific language.
    /// </summary>
    /// <param name="cultureCode">Culture code (e.g., en-US, tr-TR)</param>
    [HttpGet("{cultureCode}")]
    [ProducesResponseType<ApiInformation>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult GetApiInfoForCulture(string cultureCode)
    {
        try
        {
            var culture = new CultureInfo(cultureCode);
            
            return Ok(new ApiInformation
            {
                Culture = culture.Name,
                DisplayName = culture.DisplayName,
                Title = _localizer.GetTitle(culture),
                Description = _localizer.GetDescription(culture),
                ContactName = _localizer.GetContactName(culture),
                ContactEmail = _localizer.GetContactEmail(culture),
                LicenseName = _localizer.GetLicenseName(culture),
                SupportedCultures = _localizer.GetSupportedCultures()
                    .Select(c => new CultureInfoDto
                    {
                        Code = c.Name,
                        DisplayName = c.DisplayName,
                        NativeName = c.NativeName
                    })
                    .ToList()
            });
        }
        catch (CultureNotFoundException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid culture code",
                Detail = $"The culture code '{cultureCode}' is not recognized.",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
    }
}

public sealed record ApiInformation
{
    public required string Culture { get; init; }
    public required string DisplayName { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string ContactName { get; init; }
    public required string ContactEmail { get; init; }
    public required string LicenseName { get; init; }
    public required List<CultureInfoDto> SupportedCultures { get; init; }
}

public sealed record CultureInfoDto
{
    public required string Code { get; init; }
    public required string DisplayName { get; init; }
    public required string NativeName { get; init; }
}
