using System.Globalization;

namespace Netemplate.Application.Localization.Formatters;

/// <summary>
/// Interface for culture-aware formatting of dates, numbers, currencies, and time zones.
/// </summary>
public interface ICultureFormatter
{
    /// <summary>
    /// Formats a date according to the specified culture.
    /// </summary>
    string FormatDate(DateTime date, CultureInfo? culture = null);

    /// <summary>
    /// Formats a date and time according to the specified culture.
    /// </summary>
    string FormatDateTime(DateTime dateTime, CultureInfo? culture = null);

    /// <summary>
    /// Formats a time according to the specified culture.
    /// </summary>
    string FormatTime(DateTime time, CultureInfo? culture = null);

    /// <summary>
    /// Formats a number according to the specified culture.
    /// </summary>
    string FormatNumber(decimal number, CultureInfo? culture = null, int? decimalPlaces = null);

    /// <summary>
    /// Formats a currency value according to the specified culture and currency code.
    /// </summary>
    string FormatCurrency(decimal amount, string currencyCode, CultureInfo? culture = null);

    /// <summary>
    /// Formats a percentage according to the specified culture.
    /// </summary>
    string FormatPercentage(decimal value, CultureInfo? culture = null, int? decimalPlaces = null);

    /// <summary>
    /// Converts a UTC DateTime to the specified time zone and formats it.
    /// </summary>
    string FormatWithTimeZone(DateTime utcDateTime, string timeZoneId, CultureInfo? culture = null);

    /// <summary>
    /// Gets the time zone display name for the specified time zone and culture.
    /// </summary>
    string GetTimeZoneDisplayName(string timeZoneId, CultureInfo? culture = null);
}
