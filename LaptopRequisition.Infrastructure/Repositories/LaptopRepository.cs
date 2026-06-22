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
                // Also delete any associated LaptopAssignments
                var assignments = await _context.LaptopAssignments.Where(la => la.LaptopId == id).ToListAsync();
                _context.LaptopAssignments.RemoveRange(assignments);

                _context.Laptops.Remove(laptop);
                await _context.SaveChangesAsync();
            }
        }

        // FIX: Updated to query LaptopAssignments
        public async Task<Laptop?> GetAssignedLaptopByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.LaptopAssignments
                                 .Where(la => la.EmployeeId == employeeId)
                                 .OrderByDescending(la => la.AssignedDate) // Get the most recent assignment
                                 .Select(la => la.Laptop)
                                 .FirstOrDefaultAsync();
        }

        // New methods for Admin Dashboard
        public async Task<int> CountAllAsync()
        {
            return await _context.Laptops.CountAsync();
        }

        public async Task<int> CountUpToDateAsync(DateTime date)
        {
            return await _context.Laptops.CountAsync(l => l.CreatedAt <= date);
        }

        public async Task<int> CountAvailableAsync()
        {
            // FIX: Check if laptop has no current assignment
            return await _context.Laptops
                                 .Where(l => !_context.LaptopAssignments.Any(la => la.LaptopId == l.Id))
                                 .CountAsync();
        }

        public async Task<int> CountAvailableUpToDateAsync(DateTime date)
        {
            // Laptops created before the date AND NOT currently assigned at that date
            return await _context.Laptops
                .Where(l => l.CreatedAt <= date)
                .Where(l => !_context.LaptopAssignmentHistories
                    .Any(lah => lah.LaptopId == l.Id && lah.AssignedAt <= date && (lah.ReturnedAt == null || lah.ReturnedAt > date)))
                .CountAsync();
        }

        public async Task<int> CountByStatusAsync(LaptopStatus status) // New method
        {
            // This method needs careful re-evaluation as LaptopStatus is now managed by LaptopAssignments
            // For now, assuming status refers to the Laptop's own status property, not assignment status
            return await _context.Laptops.CountAsync(l => l.Status == status);
        }

        // FIX: Updated to query LaptopAssignments
        public async Task<List<Guid>> GetAllAssignedToEmployeeIdsAsync()
        {
            return await _context.LaptopAssignments
                                 .Select(la => la.EmployeeId)
                                 .Distinct()
                                 .ToListAsync();
        }

        // New method for filtered and paginated laptops
        public async Task<PaginatedResultDto<Laptop>> GetFilteredAndPaginatedLaptopsAsync(LaptopFilterDto filter)
        {
            IQueryable<Laptop> query = _context.Laptops
                                                 .AsQueryable();

            // Include LaptopAssignments and Employee for filtering/sorting
            query = query.Include(l => l.LaptopAssignments)
                         .ThenInclude(la => la.Employee);

            // Apply search term
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(l => l.AssetTag.Contains(filter.SearchTerm) ||
                                         l.Brand.Contains(filter.SearchTerm) ||
                                         l.Model.Contains(filter.SearchTerm) ||
                                         l.SerialNumber.Contains(filter.SearchTerm) ||
                                         l.LaptopAssignments.Any(la => la.Employee!.FullName.Contains(filter.SearchTerm))); // FIX: Use LaptopAssignments for employee search
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
                    query = query.Where(l => l.LaptopAssignments.Any()); // FIX: Check LaptopAssignments
                }
                else
                {
                    query = query.Where(l => !l.LaptopAssignments.Any()); // FIX: Check LaptopAssignments
                }
            }

            // Apply AssignedToEmployeeId filter
            if (filter.AssignedToEmployeeId.HasValue)
            {
                query = query.Where(l => l.LaptopAssignments.Any(la => la.EmployeeId == filter.AssignedToEmployeeId.Value)); // FIX: Check LaptopAssignments
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
                        // FIX: Sort by the FullName of the employee in the most recent assignment
                        query = filter.SortOrder?.ToLower() == "desc" 
                            ? query.OrderByDescending(l => l.LaptopAssignments.OrderByDescending(la => la.AssignedDate).FirstOrDefault()!.Employee!.FullName) 
                            : query.OrderBy(l => l.LaptopAssignments.OrderByDescending(la => la.AssignedDate).FirstOrDefault()!.Employee!.FullName);
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

        // FIX: Updated to query LaptopAssignments
        public async Task<Laptop?> GetAnyAssignedLaptopByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.LaptopAssignments
                                 .Where(la => la.EmployeeId == employeeId)
                                 .OrderByDescending(la => la.AssignedDate) // Get the most recent assignment
                                 .Select(la => la.Laptop)
                                 .FirstOrDefaultAsync();
        }
    }
}