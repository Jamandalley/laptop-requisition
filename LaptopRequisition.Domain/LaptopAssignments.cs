namespace LaptopRequisition.Domain;

public class LaptopAssignments
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid LaptopId { get; set; }
    public Laptop? Laptop { get; set; }
    public DateTime AssignedDate { get; set; }
}