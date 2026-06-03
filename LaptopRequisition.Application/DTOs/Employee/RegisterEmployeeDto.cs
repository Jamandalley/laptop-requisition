using System.ComponentModel.DataAnnotations;
using System; // Added for Guid

namespace LaptopRequisition.Application.DTOs
{
    public class RegisterEmployeeDto
    {
        [Required]
        public string StaffId { get; set; } = string.Empty; // Initialized to prevent CS8618 warning

        [Required]
        public string FullName { get; set; } = string.Empty; // Initialized to prevent CS8618 warning

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty; // Initialized to prevent CS8618 warning

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty; // Initialized to prevent CS8618 warning

        [Required]
        public Guid DepartmentId { get; set; } // Changed from String Department to Guid DepartmentId

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty; // Initialized to prevent CS8618 warning

        [Required] // Added for OTP validation
        public string ValidationReference { get; set; } = string.Empty;
    }
}