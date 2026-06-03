using LaptopRequisition.Application.DTOs.SSO;
using System; 
using LaptopRequisition.Application.DTOs.Employee;

namespace LaptopRequisition.Application.DTOs.Login
{
    public class LoginResponseDto
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }

        // Changed from SsoTokenResponseDto to SsoTokenDetailsDto
        public SsoTokenDetailsDto TokenDetails { get; set; } = new SsoTokenDetailsDto();
        public EmployeeDto EmployeeDetails { get; set; } = new EmployeeDto();

        public bool IsLocked { get; set; }
        public DateTime? LockoutEndDate { get; set; }
        public bool IsFirstLogin { get; set; }
        public bool IsAdmin { get; set; }
 
    }
}