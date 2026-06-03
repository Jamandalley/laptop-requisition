using Refit;
using LaptopRequisition.Application.DTOs.SSO;
using System.Threading.Tasks;
using System.Collections.Generic; // Added for FormUrlEncodedContent

namespace LaptopRequisition.Application.Interfaces.SSO
{
    public interface ISsoClient
    {
        /// <summary>
        /// Creates a new user in the SSO system.
        /// </summary>
        /// <param name="clientId">The client ID for the SSO application.</param>
        /// <param name="request">The user creation request details.</param>
        /// <returns>The SSO user creation response.</returns>
        [Post("/api/users/{clientId}/client/create")]
        Task<SsoUserCreationResponseDto> CreateSsoUser([AliasAs("clientId")] string clientId, [Body] SsoUserCreationRequestDto request);

        /// <summary>
        /// Requests an access token from the SSO system using form-urlencoded data (e.g., for client credentials flow).
        /// </summary>
        /// <param name="request">The token request details (username, password, client_id, client_secret).</param>
        /// <returns>The SSO token response.</returns>
        [Post("/connect/token")]
        [Headers("Content-Type: application/x-www-form-urlencoded")]
        Task<SsoTokenResponseDto> GetSsoToken([Body(BodySerializationMethod.UrlEncoded)] SsoTokenRequestDto request);

        /// <summary>
        /// Performs a direct login to the SSO system using JSON credentials.
        /// </summary>
        /// <param name="request">The login request details (username, password).</param>
        /// <returns>The full SSO login response, including token and profile details.</returns>
        [Post("/api/v1/authentication/login")]
        [Headers("Content-Type: application/json")]
        Task<SsoLoginResponseRootDto> LoginSsoUser([Body] SsoLoginRequestDto request);
    }
}