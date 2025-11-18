namespace Netemplate.Application.Validators;

public interface IValidator<in T>
{
    ValidationResult Validate(T instance);
}
