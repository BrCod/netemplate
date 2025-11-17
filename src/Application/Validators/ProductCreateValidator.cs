using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    /// <summary>
    /// FluentValidation validator for Product creation requests.
    /// </summary>
    public class ProductCreateValidator : AbstractValidator<ProductDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductCreateValidator"/> class.
        /// </summary>
        public ProductCreateValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required.")
                .NotNull().WithMessage("Product name cannot be null.")
                .Length(3, 200).WithMessage("Product name must be between 3 and 200 characters.")
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Product name cannot be only whitespace.");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
                .When(x => x.Description != null);
        }
    }
}
