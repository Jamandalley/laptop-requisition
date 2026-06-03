using LaptopRequisition.Application.DTOs.Admin;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs; // Added for PaginatedResultDto
using System; // Added for Guid
using System.Collections.Generic;
using LaptopRequisition.Application.DTOs.Page; // Added for IEnumerable
using Microsoft.AspNetCore.Http; // Added for IFormFile

namespace LaptopRequisition.Application.Interfaces
{
    public interface IUserManagementService
    {
        Task<UserManagementSummaryDto> GetUserManagementSummaryAsync();
        Task<PaginatedResultDto<AdminEmployeeResponseDto>> GetFilteredAndPaginatedEmployeesAsync(EmployeeFilterDto filter);

        // New methods for employee profile management
        Task<AdminEmployeeProfileDto> GetEmployeeProfileForAdminAsync(Guid employeeId);
        Task<AdminEmployeeProfileDto> UpdateEmployeeDetailsAsync(Guid employeeId, UpdateEmployeeDto dto);

        // New methods for account status management
        Task DeactivateEmployeeAsync(Guid employeeId);
        Task ReactivateEmployeeAsync(Guid employeeId);

        // New method for admin-initiated password reset
        Task AdminInitiatePasswordResetAsync(Guid employeeId);

        // New methods for soft delete and recycle bin
        Task SoftDeleteEmployeeAsync(Guid employeeId);
        Task RestoreEmployeeAsync(Guid employeeId);
        Task<IEnumerable<AdminEmployeeResponseDto>> GetDeletedEmployeesAsync();

        // New method for bulk employee registration
        Task<List<BulkUploadResultDto>> BulkRegisterEmployeesAsync(IFormFile csvFile);

        // New method for changing employee role
        Task UpdateEmployeeRoleAsync(Guid employeeId, Guid newRoleId);

        // New method for admin-initiated single employee creation
        Task<AdminEmployeeProfileDto> AdminCreateEmployeeAsync(AdminCreateEmployeeDto dto);

        // New method for hard delete (purge)
        Task PurgeEmployeeAsync(Guid employeeId);
    }
}