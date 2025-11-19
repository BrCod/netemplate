using Netemplate.Application.DTOs;
using Netemplate.Application.Validators;

namespace Netemplate.Application.Validators;

public sealed class CreateProductRequestValidator : IValidator<CreateProductRequest>
{
    public ValidationResult Validate(CreateProductRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Product name is required");
        else if (request.Name.Length < 3 || request.Name.Length > 200)
            errors.Add("Product name must be between 3 and 200 characters");

        if (request.Price < 0)
            errors.Add("Price cannot be negative");
        else if (request.Price > 1_000_000)
            errors.Add("Price cannot exceed 1,000,000");

        if (string.IsNullOrWhiteSpace(request.Currency))
            errors.Add("Currency is required");
        else
        {
            var supportedCurrencies = new[] { "USD", "EUR", "GBP", "CAD" };
            if (!supportedCurrencies.Contains(request.Currency.ToUpper()))
                errors.Add($"Currency must be one of: {string.Join(", ", supportedCurrencies)}");
        }

        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 1000)
            errors.Add("Description cannot exceed 1000 characters");

        var result = new ValidationResult();
        foreach (var error in errors)
        {
            result.Add("ValidationError", error);
        }
        return result;
    }
}

public sealed class UpdateProductRequestValidator : IValidator<UpdateProductRequest>
{
    public ValidationResult Validate(UpdateProductRequest request)
    {
        var errors = new List<string>();

        if (request.Name != null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add("Product name cannot be empty");
            else if (request.Name.Length < 3 || request.Name.Length > 200)
                errors.Add("Product name must be between 3 and 200 characters");
        }

        if (request.Price.HasValue)
        {
            if (request.Price.Value < 0)
                errors.Add("Price cannot be negative");
            else if (request.Price.Value > 1_000_000)
                errors.Add("Price cannot exceed 1,000,000");
        }

        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            var supportedCurrencies = new[] { "USD", "EUR", "GBP", "CAD" };
            if (!supportedCurrencies.Contains(request.Currency.ToUpper()))
                errors.Add($"Currency must be one of: {string.Join(", ", supportedCurrencies)}");
        }

        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 1000)
            errors.Add("Description cannot exceed 1000 characters");

        var result = new ValidationResult();
        foreach (var error in errors)
        {
            result.Add("ValidationError", error);
        }
        return result;
    }
}
