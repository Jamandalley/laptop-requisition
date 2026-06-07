using System;

namespace LaptopRequisition.Domain;

public class LaptopAssignments
{
    public Guid EmployeeId { get; set; } // FIX: Changed back to non-nullable
    public Employee? Employee { get; set; }
    public Guid LaptopId { get; set; } // FIX: Changed back to non-nullable
    public Laptop? Laptop { get; set; }
    public DateTime AssignedDate { get; set; }
}