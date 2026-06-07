using LaptopRequisition.Domain;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LaptopRequisition.Application.Interfaces
{
    public interface ILaptopAssignmentRepository
    {
        Task AddAsync(LaptopAssignments assignment);
        Task UpdateAsync(LaptopAssignments assignment);
        Task RemoveAsync(LaptopAssignments assignment);
        Task<LaptopAssignments?> GetByIdAsync(Guid employeeId, Guid laptopId);
        Task<LaptopAssignments?> GetCurrentAssignmentForEmployeeAsync(Guid employeeId);
        Task<LaptopAssignments?> GetCurrentAssignmentForLaptopAsync(Guid laptopId);
        Task<IEnumerable<LaptopAssignments>> GetAllAssignmentsForEmployeeAsync(Guid employeeId);
        Task<bool> AnyAssignmentForEmployeeAsync(Guid employeeId);
        Task<bool> AnyAssignmentForLaptopAsync(Guid laptopId);
        Task<IEnumerable<LaptopAssignments>> GetAllAsync();
        // NEW: Method to get current assignment for a specific employee and laptop
        Task<LaptopAssignments?> GetCurrentAssignmentForEmployeeAndLaptopAsync(Guid employeeId, Guid laptopId);
    }
}