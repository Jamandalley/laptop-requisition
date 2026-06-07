using LaptopRequisition.Domain.Enums;

namespace LaptopRequisition.Application.DTOs;

public class RequestStatusDto
{
    public Guid RequestId { get; set; }

    public RequestStatus Status { get; set; }

    public string? LaptopName { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? ApprovedRejectedAt { get; set; }

    public bool IsReceiptConfirmed { get; set; }
}