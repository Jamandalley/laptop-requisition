using LaptopRequisition.Application.DTOs.Admin.Reports;
using System.Collections.Generic;
using System.Threading.Tasks;
using System; // Added for DateTime
using LaptopRequisition.Domain.Common; // NEW: Added for Response<T>

namespace LaptopRequisition.Application.Interfaces
{
    public interface IAdminReportingService
    {
        Task<Response<LaptopUtilizationReportDto>> GetLaptopUtilizationReportAsync();
        Task<Response<IEnumerable<RequestTrendReportDto>>> GetRequestTrendReportAsync(DateTime startDate, DateTime endDate);
        Task<Response<IEnumerable<EmployeeActivityReportDto>>> GetEmployeeActivityReportAsync(DateTime startDate, DateTime endDate);
        Task<Response<IEnumerable<DepartmentLaptopAllocationDto>>> GetDepartmentLaptopAllocationAsync();
    }
}