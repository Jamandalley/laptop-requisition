using System; // Added for Guid
using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Application.DTOs.Page; // Added for PaginatedFilterDto

namespace LaptopRequisition.Application.DTOs.Admin
{
    public class EmployeeFilterDto : PaginatedFilterDto
    {
        public string? SearchTerm { get; set; } // For searching by name, staff ID, email
        public Guid? DepartmentId { get; set; }
        public Guid? RoleId { get; set; }
        public bool? IsActive { get; set; } // Corresponds to !IsLocked
        public bool? IsVerified { get; set; }
        public bool? HasAssignedLaptop { get; set; }
    }
}