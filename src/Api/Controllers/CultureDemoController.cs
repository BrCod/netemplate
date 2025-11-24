using Microsoft.AspNetCore.Mvc;
using Netemplate.Application.Localization.Formatters;
using System.Globalization;

namespace Netemplate.Api.Controllers;

/// <summary>
/// Demonstrates culture-aware formatting for dates, numbers, currencies, and time zones.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class CultureDemoController : ControllerBase
{
    private readonly ICultureFormatter _formatter;

    public CultureDemoController(ICultureFormatter formatter)
    {
        _formatter = formatter;
    }

    /// <summary>
    /// Get formatted data using the current request culture.
    /// Use ?culture=es-ES or Accept-Language header to change culture.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<CultureFormattingDemo>(StatusCodes.Status200OK)]
    public IActionResult GetFormattedData()
    {
        var now = DateTime.UtcNow;
        var sampleAmount = 1234567.89m;
        var samplePercentage = 0.1525m;

        return Ok(new CultureFormattingDemo
        {
            DetectedCulture = CultureInfo.CurrentCulture.Name,
            FormattedDate = _formatter.FormatDate(now),
            FormattedDateTime = _formatter.FormatDateTime(now),
            FormattedTime = _formatter.FormatTime(now),
            FormattedNumber = _formatter.FormatNumber(sampleAmount, decimalPlaces: 2),
            Currencies = new Dictionary<string, string>
            {
                ["USD"] = _formatter.FormatCurrency(sampleAmount, "USD"),
                ["EUR"] = _formatter.FormatCurrency(sampleAmount, "EUR"),
                ["GBP"] = _formatter.FormatCurrency(sampleAmount, "GBP"),
                ["JPY"] = _formatter.FormatCurrency(sampleAmount, "JPY")
            },
            FormattedPercentage = _formatter.FormatPercentage(samplePercentage, decimalPlaces: 2),
            TimeZones = new Dictionary<string, string>
            {
                ["America/New_York"] = _formatter.FormatWithTimeZone(now, "America/New_York"),
                ["Europe/London"] = _formatter.FormatWithTimeZone(now, "Europe/London"),
                ["Asia/Tokyo"] = _formatter.FormatWithTimeZone(now, "Asia/Tokyo"),
                ["Australia/Sydney"] = _formatter.FormatWithTimeZone(now, "Australia/Sydney")
            },
            TimeZoneNames = new Dictionary<string, string>
            {
                ["America/New_York"] = _formatter.GetTimeZoneDisplayName("America/New_York"),
                ["Europe/London"] = _formatter.GetTimeZoneDisplayName("Europe/London"),
                ["Asia/Tokyo"] = _formatter.GetTimeZoneDisplayName("Asia/Tokyo"),
                ["Australia/Sydney"] = _formatter.GetTimeZoneDisplayName("Australia/Sydney")
            }
        });
    }

    /// <summary>
    /// Compare formatting across different cultures.
    /// </summary>
    [HttpGet("compare")]
    [ProducesResponseType<CultureComparisonDemo>(StatusCodes.Status200OK)]
    public IActionResult CompareFormatting()
    {
        var now = DateTime.UtcNow;
        var amount = 9876543.21m;
        var percentage = 0.4567m;

        var cultures = new[] { "en-US", "es-ES", "fr-FR", "de-DE", "ja-JP" };

        return Ok(new CultureComparisonDemo
        {
            SampleDate = now,
            SampleAmount = amount,
            SamplePercentage = percentage,
            Comparisons = cultures.Select(cultureName =>
            {
                var culture = new CultureInfo(cultureName);
                return new CultureFormatComparison
                {
                    Culture = cultureName,
                    FormattedDate = _formatter.FormatDate(now, culture),
                    FormattedNumber = _formatter.FormatNumber(amount, culture, decimalPlaces: 2),
                    FormattedCurrency = _formatter.FormatCurrency(amount, "USD", culture),
                    FormattedPercentage = _formatter.FormatPercentage(percentage, culture, decimalPlaces: 2)
                };
            }).ToList()
        });
    }
}

public sealed record CultureFormattingDemo
{
    public required string DetectedCulture { get; init; }
    public required string FormattedDate { get; init; }
    public required string FormattedDateTime { get; init; }
    public required string FormattedTime { get; init; }
    public required string FormattedNumber { get; init; }
    public required Dictionary<string, string> Currencies { get; init; }
    public required string FormattedPercentage { get; init; }
    public required Dictionary<string, string> TimeZones { get; init; }
    public required Dictionary<string, string> TimeZoneNames { get; init; }
}

public sealed record CultureComparisonDemo
{
    public required DateTime SampleDate { get; init; }
    public required decimal SampleAmount { get; init; }
    public required decimal SamplePercentage { get; init; }
    public required List<CultureFormatComparison> Comparisons { get; init; }
}

public sealed record CultureFormatComparison
{
    public required string Culture { get; init; }
    public required string FormattedDate { get; init; }
    public required string FormattedNumber { get; init; }
    public required string FormattedCurrency { get; init; }
    public required string FormattedPercentage { get; init; }
}
