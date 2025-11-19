using Netemplate.Domain.Entities;
using Netemplate.Domain.ValueObjects;

namespace Netemplate.Domain.Tests;

public class ProductTests
{
    [Fact]
    public void Create_ValidProduct_ReturnsProductWithEvent()
    {
        // Arrange
        var name = ProductName.Create("Test Product");
        var price = Money.Create(99.99m, "USD");

        // Act
        var product = Product.Create(name, price, "Test description");

        // Assert
        Assert.NotNull(product);
        Assert.Equal("Test Product", product.Name.Value);
        Assert.Equal(99.99m, product.Price.Amount);
        Assert.Equal("USD", product.Price.Currency);
        Assert.True(product.IsActive);
        Assert.Single(product.DomainEvents);
    }

    [Fact]
    public void Update_ValidChanges_UpdatesProductAndRaisesEvent()
    {
        // Arrange
        var product = Product.Create(
            ProductName.Create("Original"),
            Money.Create(50m, "USD"),
            "Original description");
        product.ClearDomainEvents();

        var newName = ProductName.Create("Updated");
        var newPrice = Money.Create(75m, "USD");

        // Act
        product.Update(newName, newPrice, "Updated description");

        // Assert
        Assert.Equal("Updated", product.Name.Value);
        Assert.Equal(75m, product.Price.Amount);
        Assert.Equal("Updated description", product.Description);
        Assert.Single(product.DomainEvents);
    }

    [Fact]
    public void Deactivate_ActiveProduct_DeactivatesAndRaisesEvent()
    {
        // Arrange
        var product = Product.Create(
            ProductName.Create("Test"),
            Money.Create(10m, "USD"));
        product.ClearDomainEvents();

        // Act
        product.Deactivate();

        // Assert
        Assert.False(product.IsActive);
        Assert.Single(product.DomainEvents);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("AB")] // Too short
    public void ProductName_InvalidInput_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ProductName.Create(invalidName));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Money_NegativeAmount_ThrowsArgumentException(decimal invalidAmount)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Money.Create(invalidAmount, "USD"));
    }

    [Fact]
    public void Money_UnsupportedCurrency_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Money.Create(100m, "JPY"));
    }
}
