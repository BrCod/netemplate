using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Interface for JWT authentication service.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>Validate JWT token.</summary>
        Task<bool> ValidateTokenAsync(string token);
        /// <summary>Extract user ID from JWT token.</summary>
        Task<string?> GetUserIdAsync(string token);
    }
}
