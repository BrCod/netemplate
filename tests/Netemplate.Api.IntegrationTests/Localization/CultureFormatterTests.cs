using FluentAssertions;
using Netemplate.Application.Localization.Formatters;
using System.Globalization;

namespace Netemplate.Api.IntegrationTests.Localization;

/// <summary>
/// Tests for culture-aware formatting with English and Turkish cultures.
/// </summary>
public sealed class CultureFormatterTests
{
    private readonly CultureFormatter _formatter;

    public CultureFormatterTests()
    {
        _formatter = new CultureFormatter();
    }

    [Fact]
    public void FormatDate_EnglishCulture_UsesCorrectFormat()
    {
        // Arrange
        var date = new DateTime(2025, 11, 24);
        var culture = new CultureInfo("en-US");

        // Act
        var result = _formatter.FormatDate(date, culture);

        // Assert
        result.Should().Be("11/24/2025");
    }

    [Fact]
    public void FormatDate_TurkishCulture_UsesCorrectFormat()
    {
        // Arrange
        var date = new DateTime(2025, 11, 24);
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _formatter.FormatDate(date, culture);

        // Assert
        result.Should().Be("24.11.2025");
    }

    [Fact]
    public void FormatDateTime_EnglishCulture_IncludesTimeComponent()
    {
        // Arrange
        var dateTime = new DateTime(2025, 11, 24, 14, 30, 0);
        var culture = new CultureInfo("en-US");

        // Act
        var result = _formatter.FormatDateTime(dateTime, culture);

        // Assert
        result.Should().Contain("11/24/2025");
        result.Should().Contain("2:30");
    }

    [Fact]
    public void FormatDateTime_TurkishCulture_IncludesTimeComponent()
    {
        // Arrange
        var dateTime = new DateTime(2025, 11, 24, 14, 30, 0);
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _formatter.FormatDateTime(dateTime, culture);

        // Assert
        result.Should().Contain("24.11.2025");
        result.Should().Contain("14:30");
    }

    [Fact]
    public void FormatNumber_EnglishCulture_UsesCommaThousandsSeparator()
    {
        // Arrange
        var number = 1234567.89m;
        var culture = new CultureInfo("en-US");

        // Act
        var result = _formatter.FormatNumber(number, culture, decimalPlaces: 2);

        // Assert
        result.Should().Be("1,234,567.89");
    }

    [Fact]
    public void FormatNumber_TurkishCulture_UsesDotThousandsSeparator()
    {
        // Arrange
        var number = 1234567.89m;
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _formatter.FormatNumber(number, culture, decimalPlaces: 2);

        // Assert
        result.Should().Be("1.234.567,89");
    }

    [Fact]
    public void FormatCurrency_USD_EnglishCulture_UsesDollarSign()
    {
        // Arrange
        var amount = 1234.56m;
        var culture = new CultureInfo("en-US");

        // Act
        var result = _formatter.FormatCurrency(amount, "USD", culture);

        // Assert
        result.Should().Be("$1,234.56");
    }

    [Fact]
    public void FormatCurrency_USD_TurkishCulture_UsesDollarSignWithTurkishFormat()
    {
        // Arrange
        var amount = 1234.56m;
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _formatter.FormatCurrency(amount, "USD", culture);

        // Assert
        result.Should().Be("$1.234,56");
    }

    [Fact]
    public void FormatCurrency_TRY_TurkishCulture_UsesTurkishLiraSymbol()
    {
        // Arrange
        var amount = 5678.90m;
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _formatter.FormatCurrency(amount, "TRY", culture);

        // Assert
        result.Should().Be("₺5.678,90");
    }

    [Fact]
    public void FormatCurrency_EUR_EnglishCulture_UsesEuroSymbol()
    {
        // Arrange
        var amount = 999.99m;
        var culture = new CultureInfo("en-US");

        // Act
        var result = _formatter.FormatCurrency(amount, "EUR", culture);

        // Assert
        result.Should().Be("€999.99");
    }

    [Fact]
    public void FormatCurrency_EUR_TurkishCulture_UsesEuroSymbolWithTurkishFormat()
    {
        // Arrange
        var amount = 999.99m;
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _formatter.FormatCurrency(amount, "EUR", culture);

        // Assert
        result.Should().Be("€999,99");
    }

    [Fact]
    public void FormatPercentage_EnglishCulture_UsesCorrectFormat()
    {
        // Arrange
        var value = 0.1525m;
        var culture = new CultureInfo("en-US");

        // Act
        var result = _formatter.FormatPercentage(value, culture, decimalPlaces: 2);

        // Assert
        result.Should().Be("15.25%");
    }

    [Fact]
    public void FormatPercentage_TurkishCulture_UsesCorrectFormat()
    {
        // Arrange
        var value = 0.1525m;
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _formatter.FormatPercentage(value, culture, decimalPlaces: 2);

        // Assert
        result.Should().Be("%15,25");
    }

    [Fact]
    public void FormatWithTimeZone_EnglishCulture_ConvertsToEasternTime()
    {
        // Arrange
        var utcTime = new DateTime(2025, 11, 24, 20, 0, 0, DateTimeKind.Utc);
        var culture = new CultureInfo("en-US");

        // Act
        var result = _formatter.FormatWithTimeZone(utcTime, "America/New_York", culture);

        // Assert
        result.Should().Contain("11/24/2025");
        result.Should().Contain("Eastern"); // Eastern Standard Time
    }

    [Fact]
    public void FormatWithTimeZone_TurkishCulture_ConvertsToIstanbulTime()
    {
        // Arrange
        var utcTime = new DateTime(2025, 11, 24, 20, 0, 0, DateTimeKind.Utc);
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _formatter.FormatWithTimeZone(utcTime, "Europe/Istanbul", culture);

        // Assert
        result.Should().Contain("24.11.2025");
        result.Should().Contain("Turkey"); // Turkey Time
    }

    [Fact]
    public void GetTimeZoneDisplayName_EnglishCulture_ReturnsEnglishName()
    {
        // Arrange
        var culture = new CultureInfo("en-US");

        // Act
        var result = _formatter.GetTimeZoneDisplayName("America/New_York", culture);

        // Assert
        result.Should().Contain("Eastern");
    }

    [Fact]
    public void GetTimeZoneDisplayName_TurkishCulture_ReturnsTurkishName()
    {
        // Arrange
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _formatter.GetTimeZoneDisplayName("Europe/Istanbul", culture);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FormatWithTimeZone_InvalidTimeZone_FallsBackToUtc()
    {
        // Arrange
        var utcTime = new DateTime(2025, 11, 24, 20, 0, 0, DateTimeKind.Utc);
        var culture = new CultureInfo("en-US");

        // Act
        var result = _formatter.FormatWithTimeZone(utcTime, "Invalid/TimeZone", culture);

        // Assert
        result.Should().Contain("UTC");
    }

    [Theory]
    [InlineData("en-US", "USD", "$")]
    [InlineData("tr-TR", "TRY", "₺")]
    [InlineData("en-GB", "GBP", "£")]
    [InlineData("ja-JP", "JPY", "¥")]
    public void FormatCurrency_VariousCultures_UsesCorrectSymbol(string cultureName, string currencyCode, string expectedSymbol)
    {
        // Arrange
        var amount = 100m;
        var culture = new CultureInfo(cultureName);

        // Act
        var result = _formatter.FormatCurrency(amount, currencyCode, culture);

        // Assert
        result.Should().Contain(expectedSymbol);
    }

    [Fact]
    public void CurrentCultureFormatter_UsesThreadCulture_English()
    {
        // Arrange
        var baseFormatter = new CultureFormatter();
        var currentFormatter = new CurrentCultureFormatter(baseFormatter);
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            var date = new DateTime(2025, 11, 24);

            // Act
            var result = currentFormatter.FormatDate(date);

            // Assert
            result.Should().Be("11/24/2025");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void CurrentCultureFormatter_UsesThreadCulture_Turkish()
    {
        // Arrange
        var baseFormatter = new CultureFormatter();
        var currentFormatter = new CurrentCultureFormatter(baseFormatter);
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            var date = new DateTime(2025, 11, 24);

            // Act
            var result = currentFormatter.FormatDate(date);

            // Assert
            result.Should().Be("24.11.2025");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
