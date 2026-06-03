namespace LaptopRequisition.Domain.Common;

public class EmployeeFilterDto
{
    public string? Search { get; set; } // name/email/staffId
    public Guid? DepartmentId { get; set; }
    public Guid? RoleId { get; set; }
    public bool? IsActive { get; set; }
}