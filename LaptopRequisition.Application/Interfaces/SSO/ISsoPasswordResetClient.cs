using Refit;
using LaptopRequisition.Application.DTOs.SSO;
using System.Threading.Tasks;

namespace LaptopRequisition.Application.Interfaces.SSO
{
    public interface ISsoPasswordResetClient
    {
        [Post("/api/users/initiate-reset")] // Base URL will be configured in Program.cs
        Task<ApiResponse<SsoInitiatePasswordResetResponseDto>> InitiatePasswordReset([Body] SsoInitiatePasswordResetRequestDto request);

        [Post("/api/users/reset-password")] // NEW: Added for SSO Complete Password Reset
        Task<ApiResponse<SsoCompletePasswordResetResponseDto>> CompletePasswordReset([Body] SsoCompletePasswordResetRequestDto request);

        [Post("/api/users/change-password")] // NEW: Added for SSO Change Password
        Task<ApiResponse<SsoChangePasswordResponseDto>> ChangePassword([Body] SsoChangePasswordRequestDto request);
    }
}