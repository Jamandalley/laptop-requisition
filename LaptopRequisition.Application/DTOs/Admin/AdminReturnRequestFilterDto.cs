using LaptopRequisition.Domain.Enums; // Added for ReturnRequestStatus
using System;
using LaptopRequisition.Application.DTOs.Page; // Added for Guid

namespace LaptopRequisition.Application.DTOs.Admin
{
    public class AdminReturnRequestFilterDto : PaginatedFilterDto
    {
        public string? SearchTerm { get; set; } // Search by employee name, staff ID, laptop serial number
        public ReturnRequestStatus? Status { get; set; }
        public Guid? EmployeeId { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? LaptopId { get; set; } // Added
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}