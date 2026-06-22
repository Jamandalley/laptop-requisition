using System;

namespace LaptopRequisition.Domain;

public class LaptopAssignmentHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LaptopId { get; set; }
    public Laptop? Laptop { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
}
