using System;
using LaptopRequisition.Domain.Enums;
using LaptopRequisition.Application.DTOs.Page; // NEW: Added for PaginatedFilterDto

namespace LaptopRequisition.Application.DTOs.Request
{
    public class HistoryFilterDto : PaginatedFilterDto // FIX: Inherit from PaginatedFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public RequestStatus? Status { get; set; }
        public string? RequestType { get; set; } // "LaptopRequest" or "ReturnRequest"
        // Removed: PageNumber and PageSize as they are inherited from PaginatedFilterDto
        // Removed: SortBy and SortOrder as they are inherited from PaginatedFilterDto
    }
}