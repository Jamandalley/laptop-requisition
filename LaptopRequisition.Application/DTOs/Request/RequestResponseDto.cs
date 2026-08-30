using LaptopRequisition.Domain.Enums;
using System; // Added for Guid and DateTime

namespace LaptopRequisition.Application.DTOs.Request
{
    public class RequestResponseDto
    {
        public Guid Id { get; set; }

        public Guid? EmployeeId { get; set; } // Changed to nullable

        public string EmployeeName { get; set; } = string.Empty; // Initialized to prevent CS8618 warning
        public string? EmployeeEmail { get; set; } // Added
        public string? StaffId { get; set; } // Added
        public string? DepartmentName { get; set; } // Added

        public RequestStatus Status { get; set; }

        public string Purpose { get; set; } = string.Empty; // Initialized to prevent CS8618 warning

        public string? PreferredSpecs { get; set; } 

        public bool IsSwapRequest { get; set; }

        public string? RejectionReason { get; set; }

        public Guid? LaptopId { get; set; }

        public string? LaptopName { get; set; }

        public bool IsReceiptConfirmed { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ApprovedRejectedAt { get; set; }

        public DateTime? AssignedAt { get; set; }

        public DateTime? ReceiptConfirmedAt { get; set; }

        public string? AlternativeDeviceNote { get; set; } // Added
    }
}