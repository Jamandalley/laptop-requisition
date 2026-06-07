using LaptopRequisition.Domain;
using System; // Added for Guid
using System.Collections.Generic; // Added for IEnumerable
using System.Threading.Tasks; // Added for Task

namespace LaptopRequisition.Application.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<Department?> GetByIdAsync(Guid id); // Changed to nullable
        Task<Department?> GetByNameAsync(string name); // Changed to nullable
        Task<IEnumerable<Department>> GetAllAsync();
        Task AddAsync(Department department);
        Task UpdateAsync(Department department);
        Task DeleteAsync(Guid id);
        Task<bool> AnyEmployeesInDepartmentAsync(Guid departmentId); // NEW: Added to check for associated employees
    }
}