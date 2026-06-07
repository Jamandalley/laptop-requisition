using System;
using System.Collections.Generic;

namespace LaptopRequisition.Domain;

public class Employee
{
    public Guid Id { get; set; }
    public string StaffId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public int FailedLoginCount { get; set; }
    public bool IsLocked { get; set; }
    public string PreviousPasswordHashes { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string? ProfilePictureUrl { get; set; }
    public bool IsFirstLogin { get; set; } = true;
    public bool IsVerified { get; set; } = false;
    public bool IsDeleted { get; set; } = false;

    public DateTime? LockoutEndDate { get; set; }

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Department Department { get; set; } = null!;

    public ICollection<Request> Requests { get; set; } = new List<Request>();
    public ICollection<LaptopAssignments> LaptopAssignments { get; set; } = new List<LaptopAssignments>(); // NEW: Added LaptopAssignments navigation property
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}