using LaptopRequisition.Domain;
using System; // Added for Guid
using System.Collections.Generic;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs; // Added for PaginatedResultDto
using LaptopRequisition.Application.DTOs.Laptop;
using LaptopRequisition.Application.DTOs.Page; // Added for LaptopFilterDto
using LaptopRequisition.Domain.Enums; // Added for LaptopStatus

namespace LaptopRequisition.Application.Interfaces
{
    public interface ILaptopRepository
    {
        Task<Laptop?> GetByIdAsync(Guid id); // Changed to nullable
        Task<Laptop?> GetBySerialNumberAsync(string serialNumber); // Changed to nullable
        Task<Laptop?> GetByAssetTagAsync(string assetTag); // New method
        Task<IEnumerable<Laptop>> GetAllAsync();
        Task AddAsync(Laptop laptop);
        Task UpdateAsync(Laptop laptop);
        Task DeleteAsync(Guid id);
        Task<Laptop?> GetAssignedLaptopByEmployeeIdAsync(Guid employeeId); // Added

        // New methods for Admin Dashboard
        Task<int> CountAllAsync();
        Task<int> CountAvailableAsync();
        Task<int> CountByStatusAsync(LaptopStatus status); // New method

        // New method for User Management Summary
        Task<List<Guid>> GetAllAssignedToEmployeeIdsAsync();

        // New method for filtered and paginated laptops
        Task<PaginatedResultDto<Laptop>> GetFilteredAndPaginatedLaptopsAsync(LaptopFilterDto filter);

        // New method to get any assigned laptop for an employee
        Task<Laptop?> GetAnyAssignedLaptopByEmployeeIdAsync(Guid employeeId);
    }
}