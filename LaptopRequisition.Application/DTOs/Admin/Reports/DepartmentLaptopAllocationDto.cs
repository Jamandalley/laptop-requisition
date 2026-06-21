using System;

namespace LaptopRequisition.Application.DTOs.Admin.Reports
{
    public class DepartmentLaptopAllocationDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int LaptopCount { get; set; }
    }
}
