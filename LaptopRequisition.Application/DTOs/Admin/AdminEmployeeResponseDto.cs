using System;

namespace LaptopRequisition.Application.DTOs.Admin
{
    public class AdminEmployeeResponseDto
    {
        public Guid Id { get; set; }
        public string StaffId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public bool IsVerified { get; set; }
        public bool IsFirstLogin { get; set; }
        public bool HasAssignedLaptop { get; set; } // Derived property
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}