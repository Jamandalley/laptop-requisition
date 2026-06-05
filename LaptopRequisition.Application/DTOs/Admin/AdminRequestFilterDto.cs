using LaptopRequisition.Domain.Enums; // Added for RequestStatus
using System;
using LaptopRequisition.Application.DTOs.Page; // Added for Guid

namespace LaptopRequisition.Application.DTOs.Admin
{
    public class AdminRequestFilterDto : PaginatedFilterDto
    {
        public string? SearchTerm { get; set; } // Search by employee name, staff ID, laptop serial number
        public RequestStatus? Status { get; set; }
        public Guid? EmployeeId { get; set; }
        public Guid? DepartmentId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IncludeDismissed { get; set; } = false; // To view dismissed requests
    }
}