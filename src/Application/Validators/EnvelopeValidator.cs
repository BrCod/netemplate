using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    /// <summary>
    /// FluentValidation validator for MessageEnvelope.
    /// </summary>
    public class EnvelopeValidator : AbstractValidator<MessageEnvelope>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvelopeValidator"/> class.
        /// </summary>
        public EnvelopeValidator()
        {
            RuleFor(x => x.CorrelationId)
                .NotEmpty().WithMessage("CorrelationId is required.");

            RuleFor(x => x.SchemaVersion)
                .GreaterThan(0).WithMessage("SchemaVersion must be positive.");

            RuleFor(x => x.Timestamp)
                .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
                .WithMessage("Timestamp must not be more than 5 minutes in the future (clock skew tolerance).");

            RuleFor(x => x.EventType)
                .NotEmpty().WithMessage("EventType is required.");

            RuleFor(x => x.Payload)
                .NotNull().WithMessage("Payload is required.");
        }
    }
}
