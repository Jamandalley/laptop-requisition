using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LaptopRequisition.Domain.Enums;

namespace LaptopRequisition.Domain
{
    public class Laptop
    {
        public Guid Id { get; set; }
        [MaxLength(255)]
        public string AssetTag { get; set; } = string.Empty;
        [MaxLength(255)]
        public string Brand { get; set; } = string.Empty;
        [MaxLength(255)]
        public string Model { get; set; } = string.Empty;
        [MaxLength(255)]
        public string SerialNumber { get; set; } = string.Empty;
        [MaxLength(255)]
        public string Processor { get; set; } = string.Empty;
        [MaxLength(255)]
        public string RAM { get; set; } = string.Empty;
        [MaxLength(255)]
        public string Storage { get; set; } = string.Empty;
        public OperatingSystemEnum OperatingSystem { get; set; }
        [MaxLength(255)]
        public string ScreenSize { get; set; } = string.Empty;

        public LaptopStatus Status { get; set; } = LaptopStatus.Available;

        // Removed direct assignment properties from Laptop, now handled by LaptopAssignments
        // public Guid? AssignedToEmployeeId { get; set; }
        // public virtual Employee? AssignedToEmployee { get; set; }
        // public DateTime? AssignedAt { get; set; }
        public DateTime CreatedAt { get; set; } 
        public DateTime UpdatedAt { get; set; } 

        public DateTime PurchaseDate { get; set; }
        public DateTime WarrantyExpiryDate { get; set; }

        
        public ICollection<Request> Requests { get; set; } = new List<Request>();
        public ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();
        public ICollection<LaptopAssignments> LaptopAssignments { get; set; } = new List<LaptopAssignments>(); // NEW: Added LaptopAssignments navigation property
    }
}