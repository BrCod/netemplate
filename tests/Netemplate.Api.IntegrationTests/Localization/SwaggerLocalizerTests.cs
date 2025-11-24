using FluentAssertions;
using Netemplate.Api.Swagger.Localization;
using System.Globalization;

namespace Netemplate.Api.IntegrationTests.Localization;

/// <summary>
/// Tests for Swagger/OpenAPI localization with English and Turkish.
/// </summary>
public sealed class SwaggerLocalizerTests
{
    private readonly SwaggerLocalizer _localizer;

    public SwaggerLocalizerTests()
    {
        _localizer = new SwaggerLocalizer();
    }

    [Fact]
    public void GetTitle_EnglishCulture_ReturnsEnglishTitle()
    {
        // Arrange
        var culture = new CultureInfo("en-US");

        // Act
        var result = _localizer.GetTitle(culture);

        // Assert
        result.Should().Be("Clean Architecture API");
    }

    [Fact]
    public void GetTitle_TurkishCulture_ReturnsTurkishTitle()
    {
        // Arrange
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _localizer.GetTitle(culture);

        // Assert
        result.Should().Be("Temiz Mimari API");
    }

    [Fact]
    public void GetDescription_EnglishCulture_ReturnsEnglishDescription()
    {
        // Arrange
        var culture = new CultureInfo("en-US");

        // Act
        var result = _localizer.GetDescription(culture);

        // Assert
        result.Should().Contain("Clean Architecture API Template");
        result.Should().Contain("observability");
    }

    [Fact]
    public void GetDescription_TurkishCulture_ReturnsTurkishDescription()
    {
        // Arrange
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _localizer.GetDescription(culture);

        // Assert
        result.Should().Contain("Temiz Mimari API Şablonu");
        result.Should().Contain("gözlemlenebilirlik");
    }

    [Fact]
    public void GetContactName_EnglishCulture_ReturnsEnglishContactName()
    {
        // Arrange
        var culture = new CultureInfo("en-US");

        // Act
        var result = _localizer.GetContactName(culture);

        // Assert
        result.Should().Be("API Support Team");
    }

    [Fact]
    public void GetContactName_TurkishCulture_ReturnsTurkishContactName()
    {
        // Arrange
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _localizer.GetContactName(culture);

        // Assert
        result.Should().Be("API Destek Ekibi");
    }

    [Fact]
    public void GetContactEmail_EnglishCulture_ReturnsEmail()
    {
        // Arrange
        var culture = new CultureInfo("en-US");

        // Act
        var result = _localizer.GetContactEmail(culture);

        // Assert
        result.Should().Be("support@example.com");
    }

    [Fact]
    public void GetContactEmail_TurkishCulture_ReturnsLocalizedEmail()
    {
        // Arrange
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _localizer.GetContactEmail(culture);

        // Assert
        result.Should().Be("destek@example.com");
    }

    [Fact]
    public void GetLicenseName_EnglishCulture_ReturnsEnglishLicense()
    {
        // Arrange
        var culture = new CultureInfo("en-US");

        // Act
        var result = _localizer.GetLicenseName(culture);

        // Assert
        result.Should().Be("MIT License");
    }

    [Fact]
    public void GetLicenseName_TurkishCulture_ReturnsTurkishLicense()
    {
        // Arrange
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _localizer.GetLicenseName(culture);

        // Assert
        result.Should().Be("MIT Lisansı");
    }

    [Fact]
    public void GetTermsOfService_EnglishCulture_ReturnsEnglishUrl()
    {
        // Arrange
        var culture = new CultureInfo("en-US");

        // Act
        var result = _localizer.GetTermsOfService(culture);

        // Assert
        result.Should().Be("https://example.com/terms");
    }

    [Fact]
    public void GetTermsOfService_TurkishCulture_ReturnsTurkishUrl()
    {
        // Arrange
        var culture = new CultureInfo("tr-TR");

        // Act
        var result = _localizer.GetTermsOfService(culture);

        // Assert
        result.Should().Be("https://example.com/tr/kullanim-kosullari");
    }

    [Fact]
    public void GetTitle_NullCulture_UsesCurrentCulture()
    {
        // Act
        var result = _localizer.GetTitle(null);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetSupportedCultures_ReturnsEnglishAndTurkish()
    {
        // Act
        var cultures = _localizer.GetSupportedCultures();

        // Assert
        cultures.Should().HaveCount(2);
        cultures.Should().Contain(c => c.Name == "en-US");
        cultures.Should().Contain(c => c.Name == "tr-TR");
    }

    [Fact]
    public void GetTitle_UnsupportedCulture_FallsBackToEnglish()
    {
        // Arrange
        var culture = new CultureInfo("ja-JP"); // Unsupported

        // Act
        var result = _localizer.GetTitle(culture);

        // Assert
        result.Should().Be("Clean Architecture API"); // English fallback
    }

    [Fact]
    public void GetDescription_UnsupportedCulture_FallsBackToEnglish()
    {
        // Arrange
        var culture = new CultureInfo("de-DE"); // Unsupported

        // Act
        var result = _localizer.GetDescription(culture);

        // Assert
        result.Should().Contain("Clean Architecture API Template"); // English fallback
    }

    [Theory]
    [InlineData("en-US", "Clean Architecture API")]
    [InlineData("en-GB", "Clean Architecture API")] // Falls back to en-US
    [InlineData("tr-TR", "Temiz Mimari API")]
    public void GetTitle_VariousCultures_ReturnsExpectedTitle(string cultureName, string expectedTitle)
    {
        // Arrange
        var culture = new CultureInfo(cultureName);

        // Act
        var result = _localizer.GetTitle(culture);

        // Assert
        result.Should().Be(expectedTitle);
    }

    [Fact]
    public async Task Localizer_ThreadSafe_CanBeCalledConcurrently()
    {
        // Arrange
        var enCulture = new CultureInfo("en-US");
        var trCulture = new CultureInfo("tr-TR");
        var tasks = new List<Task<string>>();

        // Act - Call localizer concurrently from multiple threads
        for (int i = 0; i < 100; i++)
        {
            var culture = i % 2 == 0 ? enCulture : trCulture;
            tasks.Add(Task.Run(() => _localizer.GetTitle(culture)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All calls should succeed
        results.Should().AllSatisfy(r => r.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public void GetSupportedCultures_ImmutableList_CannotBeModified()
    {
        // Act
        var cultures = _localizer.GetSupportedCultures();

        // Assert
        cultures.Should().BeAssignableTo<IReadOnlyList<CultureInfo>>();
    }
}
