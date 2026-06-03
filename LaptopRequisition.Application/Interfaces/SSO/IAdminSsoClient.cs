using Refit;
using LaptopRequisition.Application.DTOs.SSO;
using System.Threading.Tasks;

namespace LaptopRequisition.Application.Interfaces.SSO
{
    public interface IAdminSsoClient
    {
        /// <summary>
        /// Performs a direct login to the SSO system for admin users using JSON credentials.
        /// </summary>
        /// <param name="request">The login request details (username, password).</param>
        /// <returns>The full SSO login response, including token and profile details.</returns>
        [Post("/api/v1/authentication/login")]
        [Headers("Content-Type: application/json")]
        Task<SsoLoginResponseRootDto> LoginSsoUser([Body] SsoLoginRequestDto request);
    }
}