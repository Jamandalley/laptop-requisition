using System;

namespace LaptopRequisition.Application.DTOs
{
    public class EmployeeDto
    {
        public Guid Id { get; set; }
        public string StaffId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty; // To display department name
        public string Role { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public bool IsFirstLogin { get; set; } 
    }
}