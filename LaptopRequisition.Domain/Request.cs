using LaptopRequisition.Domain.Enums;
using System;
using System.Collections.Generic; // NEW: Added for ICollection

namespace LaptopRequisition.Domain
{
    public class Request
    {
        public Guid Id { get; set; }

        public Guid? EmployeeId { get; set; }

        public Guid? LaptopId { get; set; }

        public bool IsSwapRequest { get; set; }

        public RequestStatus Status { get; set; }

        public string Purpose { get; set; } = string.Empty;

        public string PreferredSpecs { get; set; } = string.Empty;

        public string? RejectionReason { get; set; }

        public bool IsReceiptConfirmed { get; set; }

        public DateTime? ReceiptConfirmedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? ApprovedRejectedAt { get; set; }

        // Removed: public DateTime? AssignedAt { get; set; }

        public bool IsDismissed { get; set; }

        public string? AlternativeDeviceNote { get; set; }

        public Employee? Employee { get; set; }

        public Laptop? Laptop { get; set; }

        // Removed: public ICollection<LaptopAssignments>? LaptopAssignments { get; set; }
    }
}