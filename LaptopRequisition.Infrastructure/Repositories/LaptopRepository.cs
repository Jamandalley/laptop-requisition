using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain;
using Microsoft.EntityFrameworkCore;
using System; // Added for Guid
using System.Collections.Generic; // Added for IEnumerable
using System.Linq; // Added for LINQ
using System.Threading.Tasks; // Added for Task
using LaptopRequisition.Domain.Enums; // Added for LaptopStatus
using LaptopRequisition.Application.DTOs; // Added for PaginatedResultDto
using LaptopRequisition.Application.DTOs.Laptop;
using LaptopRequisition.Application.DTOs.Page; // Added for LaptopFilterDto

namespace LaptopRequisition.Infrastructure.Repositories
{
    public class LaptopRepository : ILaptopRepository
    {
        private readonly ApplicationDbContext _context;

        public LaptopRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Laptop?> GetByIdAsync(Guid id) // Updated to return nullable
        {
            return await _context.Laptops.FindAsync(id);
        }

        public async Task<Laptop?> GetBySerialNumberAsync(string serialNumber) // Updated to return nullable
        {
            return await _context.Laptops.FirstOrDefaultAsync(l => l.SerialNumber == serialNumber);
        }

        public async Task<Laptop?> GetByAssetTagAsync(string assetTag) // New method
        {
            return await _context.Laptops.FirstOrDefaultAsync(l => l.AssetTag == assetTag);
        }

        public async Task<IEnumerable<Laptop>> GetAllAsync()
        {
            return await _context.Laptops.ToListAsync();
        }

        public async Task AddAsync(Laptop laptop)
        {
            await _context.Laptops.AddAsync(laptop);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Laptop laptop)
        {
            _context.Laptops.Update(laptop);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var laptop = await _context.Laptops.FindAsync(id);
            if (laptop != null)
            {
                _context.Laptops.Remove(laptop);
                await _context.SaveChangesAsync();
            }
        }

        // New method for DashboardService
        public async Task<Laptop?> GetAssignedLaptopByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.Laptops
                .FirstOrDefaultAsync(l => l.AssignedToEmployeeId == employeeId && l.Status == LaptopStatus.Assigned); // Updated check
        }

        // New methods for Admin Dashboard
        public async Task<int> CountAllAsync()
        {
            return await _context.Laptops.CountAsync();
        }

        public async Task<int> CountAvailableAsync()
        {
            return await _context.Laptops.CountAsync(l => l.Status == LaptopStatus.Available); // Updated check
        }

        public async Task<int> CountByStatusAsync(LaptopStatus status) // New method
        {
            return await _context.Laptops.CountAsync(l => l.Status == status);
        }

        // New method for User Management Summary
        public async Task<List<Guid>> GetAllAssignedToEmployeeIdsAsync()
        {
            return await _context.Laptops
                                 .Where(l => l.AssignedToEmployeeId != null)
                                 .Select(l => l.AssignedToEmployeeId!.Value) // Select the non-null Guid
                                 .Distinct()
                                 .ToListAsync();
        }

        // New method for filtered and paginated laptops
        public async Task<PaginatedResultDto<Laptop>> GetFilteredAndPaginatedLaptopsAsync(LaptopFilterDto filter)
        {
            IQueryable<Laptop> query = _context.Laptops.Include(l => l.AssignedToEmployee); // Include assigned employee for potential filtering/sorting

            // Apply search term
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(l => l.AssetTag.Contains(filter.SearchTerm) ||
                                         l.Brand.Contains(filter.SearchTerm) ||
                                         l.Model.Contains(filter.SearchTerm) ||
                                         l.SerialNumber.Contains(filter.SearchTerm) ||
                                         (l.AssignedToEmployee != null && l.AssignedToEmployee.FullName.Contains(filter.SearchTerm)));
            }

            // Apply Brand filter
            if (!string.IsNullOrEmpty(filter.Brand))
            {
                query = query.Where(l => l.Brand == filter.Brand);
            }

            // Apply Model filter
            if (!string.IsNullOrEmpty(filter.Model))
            {
                query = query.Where(l => l.Model == filter.Model);
            }

            // Apply Status filter
            if (filter.Status.HasValue)
            {
                query = query.Where(l => l.Status == filter.Status.Value);
            }

            // Apply IsAssigned filter
            if (filter.IsAssigned.HasValue)
            {
                if (filter.IsAssigned.Value)
                {
                    query = query.Where(l => l.AssignedToEmployeeId != null);
                }
                else
                {
                    query = query.Where(l => l.AssignedToEmployeeId == null);
                }
            }

            // Apply AssignedToEmployeeId filter
            if (filter.AssignedToEmployeeId.HasValue)
            {
                query = query.Where(l => l.AssignedToEmployeeId == filter.AssignedToEmployeeId.Value);
            }

            // Sorting (add default or specific sorting if needed)
            // Use filter.SortBy and filter.SortOrder from PaginatedFilterDto
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                switch (filter.SortBy.ToLower())
                {
                    case "assettag":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(l => l.AssetTag) : query.OrderBy(l => l.AssetTag);
                        break;
                    case "brand":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(l => l.Brand) : query.OrderBy(l => l.Brand);
                        break;
                    case "model":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(l => l.Model) : query.OrderBy(l => l.Model);
                        break;
                    case "serialnumber":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(l => l.SerialNumber) : query.OrderBy(l => l.SerialNumber);
                        break;
                    case "status":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(l => l.Status) : query.OrderBy(l => l.Status);
                        break;
                    case "assignedtoemployeename":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(l => l.AssignedToEmployee!.FullName) : query.OrderBy(l => l.AssignedToEmployee!.FullName);
                        break;
                    case "createdat":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(l => l.CreatedAt) : query.OrderBy(l => l.CreatedAt);
                        break;
                    default:
                        query = query.OrderBy(l => l.AssetTag); // Default sort
                        break;
                }
            }
            else
            {
                query = query.OrderBy(l => l.AssetTag); // Default sort
            }

            var totalCount = await query.CountAsync();

            var items = await query.Skip((filter.PageNumber - 1) * filter.PageSize)
                                   .Take(filter.PageSize)
                                   .ToListAsync();

            return new PaginatedResultDto<Laptop>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        // New method to get any assigned laptop for an employee
        public async Task<Laptop?> GetAnyAssignedLaptopByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.Laptops
                                 .FirstOrDefaultAsync(l => l.AssignedToEmployeeId == employeeId && l.Status == LaptopStatus.Assigned);
        }
    }
}