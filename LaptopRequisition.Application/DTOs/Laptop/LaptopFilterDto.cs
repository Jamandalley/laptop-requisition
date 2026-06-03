using LaptopRequisition.Domain.Enums; // Added for LaptopStatus
using System;
using LaptopRequisition.Application.DTOs.Page; // Added for Guid

namespace LaptopRequisition.Application.DTOs.Laptop
{
    public class LaptopFilterDto : PaginatedFilterDto // Changed from PaginationFilterDto to PaginatedFilterDto
    {
        public string? SearchTerm { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public LaptopStatus? Status { get; set; } // Filter by laptop status
        public bool? IsAssigned { get; set; } // Filter by assignment status
        public Guid? AssignedToEmployeeId { get; set; } // Filter by specific employee assignment
    }
}