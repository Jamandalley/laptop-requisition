using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Domain;
using System.Threading.Tasks; // Added for Task
using System; // Added for Guid
using LaptopRequisition.Application.DTOs.Login; // Ensure this is present for the updated LoginResponseDto

namespace LaptopRequisition.Application.Interfaces
{
    public interface IAuthService
    {
        Task<Employee> RegisterEmployeeAsync(RegisterEmployeeDto registerDto);
        Task<LoginResponseDto> LoginAsync(string email, string password);
        Task<bool> RequestPasswordResetAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
        Task<bool> ChangePasswordAsync(Guid employeeId, string currentPassword, string newPassword);
        
        // New method for OTP verification - SIMPLIFIED
        Task VerifyAccountAsync(string validationReference, string otp); // No longer returns anything, just throws on failure

        // New method for Admin Login
        Task<LoginResponseDto> AdminLoginAsync(string email, string password);
    }
}