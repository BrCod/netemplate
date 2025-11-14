using Application.DTOs;

namespace Application.Validators
{
    /// <summary>
    /// Validator for message envelope DTO.
    /// </summary>
    public class EnvelopeValidator
    {
        /// <summary>Validate envelope fields and constraints.</summary>
        public bool Validate(MessageEnvelope envelope)
        {
            if (envelope.CorrelationId == Guid.Empty)
                return false;
            if (envelope.SchemaVersion <= 0)
                return false;
            if (envelope.Timestamp > DateTime.UtcNow.AddMinutes(5))
                return false;
            return true;
        }
    }
}
