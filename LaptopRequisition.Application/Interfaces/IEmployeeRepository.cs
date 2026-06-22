using LaptopRequisition.Domain;
using System; // Added for Guid
using System.Collections.Generic; // Added for IEnumerable
using System.Threading.Tasks; // Added for Task
using LaptopRequisition.Application.DTOs; // Added for PaginatedResultDto
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page; // Added for EmployeeFilterDto

namespace LaptopRequisition.Application.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<Employee?> GetByIdAsync(Guid id); 
        Task<Employee?> GetByStaffIdAsync(string staffId); 
        Task<Employee?> GetByEmailAsync(string email); 
        Task<IEnumerable<Employee>> GetAllAsync();
        Task AddAsync(Employee employee);
        Task UpdateAsync(Employee employee);
        // Changed from DeleteAsync to SoftDeleteAsync
        Task SoftDeleteAsync(Guid employeeId); 
        
        Task<Employee?> GetByEmailWithDepartmentAndRoleAsync(string email);
        // Modified to include LaptopAssignments and Laptop
        Task<Employee?> GetByIdWithDepartmentAndRoleAsync(Guid employeeId, bool includeLaptopAssignments = false);
        Task<int> CountAllAsync();
        Task<int> CountUpToDateAsync(DateTime date);
        
        Task<int> CountActiveUsersAsync();
        Task<int> CountPendingOnboardingAsync();
        Task<int> CountUsersWithAssignedLaptopsAsync();
        Task<int> CountUsersWithoutLaptopsAsync();

        // New method for UserManagementService
        Task<IEnumerable<Employee>> GetAllWithDepartmentAndRoleAsync();

        // New method for filtered and paginated employees
        Task<PaginatedResultDto<Employee>> GetFilteredAndPaginatedEmployeesAsync(EmployeeFilterDto filter);

        // New methods for soft delete and recycle bin
        Task RestoreAsync(Guid employeeId);
        Task<IEnumerable<Employee>> GetDeletedEmployeesAsync();
        Task<Employee?> GetByIdIncludingDeletedAsync(Guid id);

        // New method for RoleService
        Task<IEnumerable<Employee>> GetEmployeesByRoleIdAsync(Guid roleId);

        // New method for hard delete
        Task HardDeleteAsync(Guid employeeId);

        // New method for login attempt tracking
        Task UpdateLoginAttemptsAsync(Employee employee);
        
        // Modified to include LaptopAssignments and Laptop
        Task<PaginatedResultDto<Employee>> GetFilteredAsync(EmployeeFilterDto filter, bool includeLaptopAssignments = false);
    }
}