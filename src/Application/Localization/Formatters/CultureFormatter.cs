using System.Globalization;

namespace Netemplate.Application.Localization.Formatters;

/// <summary>
/// Default implementation of culture-aware formatting.
/// Formats dates, numbers, currencies, and handles time zone conversions.
/// </summary>
public sealed class CultureFormatter : ICultureFormatter
{
    private readonly CultureInfo _defaultCulture;

    public CultureFormatter()
    {
        _defaultCulture = CultureInfo.GetCultureInfo("en-US");
    }

    public string FormatDate(DateTime date, CultureInfo? culture = null)
    {
        var cultureToUse = culture ?? _defaultCulture;
        return date.ToString("d", cultureToUse); // Short date pattern
    }

    public string FormatDateTime(DateTime dateTime, CultureInfo? culture = null)
    {
        var cultureToUse = culture ?? _defaultCulture;
        return dateTime.ToString("g", cultureToUse); // General short date/time pattern
    }

    public string FormatTime(DateTime time, CultureInfo? culture = null)
    {
        var cultureToUse = culture ?? _defaultCulture;
        return time.ToString("t", cultureToUse); // Short time pattern
    }

    public string FormatNumber(decimal number, CultureInfo? culture = null, int? decimalPlaces = null)
    {
        var cultureToUse = culture ?? _defaultCulture;
        
        if (decimalPlaces.HasValue)
        {
            var format = $"N{decimalPlaces.Value}";
            return number.ToString(format, cultureToUse);
        }

        return number.ToString("N", cultureToUse); // Number with group separators
    }

    public string FormatCurrency(decimal amount, string currencyCode, CultureInfo? culture = null)
    {
        var cultureToUse = culture ?? _defaultCulture;

        // Create a NumberFormatInfo clone to customize currency symbol
        var numberFormat = (NumberFormatInfo)cultureToUse.NumberFormat.Clone();
        numberFormat.CurrencySymbol = GetCurrencySymbol(currencyCode);

        return amount.ToString("C", numberFormat);
    }

    public string FormatPercentage(decimal value, CultureInfo? culture = null, int? decimalPlaces = null)
    {
        var cultureToUse = culture ?? _defaultCulture;

        if (decimalPlaces.HasValue)
        {
            var format = $"P{decimalPlaces.Value}";
            return value.ToString(format, cultureToUse);
        }

        return value.ToString("P", cultureToUse); // Percentage
    }

    public string FormatWithTimeZone(DateTime utcDateTime, string timeZoneId, CultureInfo? culture = null)
    {
        var cultureToUse = culture ?? _defaultCulture;

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
            
            var formattedTime = localTime.ToString("g", cultureToUse);
            var abbreviation = GetTimeZoneAbbreviation(timeZone, localTime);
            
            return $"{formattedTime} {abbreviation}";
        }
        catch (TimeZoneNotFoundException)
        {
            // Fall back to UTC if time zone not found
            return $"{utcDateTime:g} UTC";
        }
    }

    public string GetTimeZoneDisplayName(string timeZoneId, CultureInfo? culture = null)
    {
        var cultureToUse = culture ?? _defaultCulture;

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            
            // Use DisplayName which is localized on Windows
            return timeZone.DisplayName;
        }
        catch (TimeZoneNotFoundException)
        {
            return timeZoneId;
        }
    }

    private static string GetCurrencySymbol(string currencyCode)
    {
        // Map common currency codes to symbols
        return currencyCode.ToUpperInvariant() switch
        {
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            "JPY" => "¥",
            "CNY" => "¥",
            "CAD" => "C$",
            "AUD" => "A$",
            "CHF" => "Fr",
            "INR" => "₹",
            "BRL" => "R$",
            "MXN" => "MX$",
            "KRW" => "₩",
            "RUB" => "₽",
            "SEK" => "kr",
            "NOK" => "kr",
            "DKK" => "kr",
            "PLN" => "zł",
            "TRY" => "₺",
            "ZAR" => "R",
            "HKD" => "HK$",
            "SGD" => "S$",
            "NZD" => "NZ$",
            _ => currencyCode // Fall back to code if unknown
        };
    }

    private static string GetTimeZoneAbbreviation(TimeZoneInfo timeZone, DateTime localTime)
    {
        // Return standard or daylight abbreviation
        return timeZone.IsDaylightSavingTime(localTime)
            ? timeZone.DaylightName
            : timeZone.StandardName;
    }
}
