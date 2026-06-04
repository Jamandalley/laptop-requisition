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
        
        // NEW: More detailed assigned laptop properties
        public Guid? AssignedLaptopId { get; set; }
        public string? AssignedLaptopSerialNumber { get; set; }
        public string? AssignedLaptopAssetTag { get; set; }
        public string? AssignedLaptopBrand { get; set; }
        public string? AssignedLaptopModel { get; set; }
        public DateTime? AssignedLaptopDate { get; set; }
    }
}