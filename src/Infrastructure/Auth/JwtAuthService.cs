using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Infrastructure.Auth
{
    /// <summary>
    /// Provides JWT-based authentication services.
    /// </summary>
    public class JwtAuthService : IAuthService
    {
        /// <summary>
        /// Validates a JWT token asynchronously.
        /// </summary>
        /// <param name="token">The JWT token to validate.</param>
        /// <returns>A task representing the validation result.</returns>
        public Task<bool> ValidateTokenAsync(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var jwt = handler.ReadJwtToken(token);
                // Add validation logic for issuer, audience, keys
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Retrieves the user ID from a JWT token asynchronously.
        /// </summary>
        /// <param name="token">The JWT token.</param>
        /// <returns>A task representing the user ID.</returns>
        public Task<string?> GetUserIdAsync(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var userId = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return Task.FromResult(userId);
        }
    }
}
