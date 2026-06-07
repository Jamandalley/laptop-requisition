using System;
using LaptopRequisition.Domain.Enums;

namespace LaptopRequisition.Application.DTOs.Request
{
    public class RequestHistoryDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        
        public DateTime? AssignedAt { get; set; }
        public string RequestType { get; set; } = string.Empty; // "Laptop Request" or "Return Request"
        public RequestStatus? Status { get; set; } // For Laptop Request
        public ReturnRequestStatus? ReturnStatus { get; set; } // For Return Request
        public string? LaptopDetails { get; set; } = string.Empty; // e.g., "HP Spectre x360 (SN: ABC123)"
        public string? Purpose { get; set; } // For Laptop Request
        public string? Reason { get; set; } // For Return Request
        public string? Duration { get; set; } // Calculated duration, e.g., "3 months"
        public string? Notes { get; set; } // Any additional notes, e.g., rejection reason
    }
}