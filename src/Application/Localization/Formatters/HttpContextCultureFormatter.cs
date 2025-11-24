using System.Globalization;

namespace Netemplate.Application.Localization.Formatters;

/// <summary>
/// Culture formatter that uses the current thread's culture when no explicit culture is provided.
/// This respects the culture set by ASP.NET Core's localization middleware.
/// </summary>
public sealed class CurrentCultureFormatter : ICultureFormatter
{
    private readonly ICultureFormatter _baseFormatter;

    public CurrentCultureFormatter(CultureFormatter baseFormatter)
    {
        _baseFormatter = baseFormatter;
    }

    private static CultureInfo GetCurrentCulture()
    {
        return CultureInfo.CurrentCulture;
    }

    public string FormatDate(DateTime date, CultureInfo? culture = null)
    {
        return _baseFormatter.FormatDate(date, culture ?? GetCurrentCulture());
    }

    public string FormatDateTime(DateTime dateTime, CultureInfo? culture = null)
    {
        return _baseFormatter.FormatDateTime(dateTime, culture ?? GetCurrentCulture());
    }

    public string FormatTime(DateTime time, CultureInfo? culture = null)
    {
        return _baseFormatter.FormatTime(time, culture ?? GetCurrentCulture());
    }

    public string FormatNumber(decimal number, CultureInfo? culture = null, int? decimalPlaces = null)
    {
        return _baseFormatter.FormatNumber(number, culture ?? GetCurrentCulture(), decimalPlaces);
    }

    public string FormatCurrency(decimal amount, string currencyCode, CultureInfo? culture = null)
    {
        return _baseFormatter.FormatCurrency(amount, currencyCode, culture ?? GetCurrentCulture());
    }

    public string FormatPercentage(decimal value, CultureInfo? culture = null, int? decimalPlaces = null)
    {
        return _baseFormatter.FormatPercentage(value, culture ?? GetCurrentCulture(), decimalPlaces);
    }

    public string FormatWithTimeZone(DateTime utcDateTime, string timeZoneId, CultureInfo? culture = null)
    {
        return _baseFormatter.FormatWithTimeZone(utcDateTime, timeZoneId, culture ?? GetCurrentCulture());
    }

    public string GetTimeZoneDisplayName(string timeZoneId, CultureInfo? culture = null)
    {
        return _baseFormatter.GetTimeZoneDisplayName(timeZoneId, culture ?? GetCurrentCulture());
    }
}
