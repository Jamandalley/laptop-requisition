using System;

namespace LaptopRequisition.Application.DTOs.Employee
{
    public class ProfileDto
    {
        public Guid Id { get; set; }
        public string StaffId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; } 
        public bool IsFirstLogin { get; set; } 
        public Guid? AssignedLaptopId { get; set; }
        public string? AssignedLaptopSerialNumber { get; set; }
    }
}