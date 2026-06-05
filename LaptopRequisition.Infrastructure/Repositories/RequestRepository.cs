using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain;
using Microsoft.EntityFrameworkCore;
using LaptopRequisition.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs.Request; // Added for HistoryFilterDto
using LaptopRequisition.Application.DTOs; // Added for PaginatedResultDto
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page; // Added for AdminRequestFilterDto

namespace LaptopRequisition.Infrastructure.Repositories
{
    public class RequestRepository : IRequestRepository
    {
        private readonly ApplicationDbContext _context;

        public RequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Request request)
        {
            await _context.Requests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task<Request?> GetByIdAsync(Guid id) // Changed to nullable
        {
            return await _context.Requests
                .Include(r => r.Employee)
                .Include(r => r.Laptop)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Request>> GetAllAsync()
        {
            return await _context.Requests
                .Include(r => r.Employee)
                .Include(r => r.Laptop)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Request>> GetByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.Requests
                .Include(r => r.Employee)
                .Include(r => r.Laptop)
                .Where(r => r.EmployeeId == employeeId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Request?> GetPendingRequestByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.Requests
                .FirstOrDefaultAsync(r =>
                    r.EmployeeId == employeeId &&
                    r.Status == RequestStatus.Pending);
        }

        public async Task UpdateAsync(Request request)
        {
            _context.Requests.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request != null)
            {
                _context.Requests.Remove(request);
                await _context.SaveChangesAsync();
            }
        }

        // New methods for DashboardService
        public async Task<int> CountByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.Requests
                .CountAsync(r => r.EmployeeId == employeeId);
        }

        public async Task<Request?> GetPendingOrApprovedRequestByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.Requests
                .Where(r => r.EmployeeId == employeeId &&
                            (r.Status == RequestStatus.Pending || r.Status == RequestStatus.Approved || r.Status == RequestStatus.Assigned))
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
        }

        // New methods for History
        public async Task<PaginatedResultDto<Request>> GetEmployeeRequestsAsync(Guid employeeId, HistoryFilterDto filter)
        {
            IQueryable<Request> query = _context.Requests
                .Where(r => r.EmployeeId == employeeId)
                .Include(r => r.Laptop)
                .Include(r => r.Employee);

            // Apply filters
            if (filter.StartDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= filter.StartDate.Value);
            }
            if (filter.EndDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= filter.EndDate.Value);
            }
            if (filter.Status.HasValue)
            {
                query = query.Where(r => r.Status == filter.Status.Value);
            }
            if (!string.IsNullOrEmpty(filter.RequestType))
            {
                // Assuming "LaptopRequest" for Request and "ReturnRequest" for ReturnRequest (which is not in this repo)
                // For now, only filter by Request type if specified
                if (filter.RequestType.Equals("LaptopRequest", StringComparison.OrdinalIgnoreCase))
                {
                    // This query already only gets Requests, so no additional filter needed here
                }
                // If "ReturnRequest" is specified, this method won't return anything, as it's for Requests only.
                // The combined history logic will handle both types.
            }

            // Order by creation date descending for chronological list
            query = query.OrderByDescending(r => r.CreatedAt);

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResultDto<Request>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<Request?> GetRequestWithLaptopAndEmployeeAsync(Guid requestId)
        {
            return await _context.Requests
                .Include(r => r.Laptop)
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == requestId);
        }

        // New method for Admin Dashboard
        public async Task<int> CountByStatusAsync(RequestStatus status)
        {
            return await _context.Requests.CountAsync(r => r.Status == status);
        }

        // New method for Admin Request Management
        public async Task<PaginatedResultDto<Request>> GetFilteredAndPaginatedRequestsAsync(AdminRequestFilterDto filter)
        {
            IQueryable<Request> query = _context.Requests
                .Include(r => r.Employee)
                .Include(r => r.Laptop);

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(r => r.Employee != null &&
                                         (r.Employee.FullName.Contains(filter.SearchTerm) ||
                                          r.Employee.StaffId.Contains(filter.SearchTerm) ||
                                          r.Laptop != null && r.Laptop.SerialNumber.Contains(filter.SearchTerm)));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(r => r.Status == filter.Status.Value);
            }
            else
            {
                // By default, exclude dismissed requests unless explicitly included
                if (!filter.IncludeDismissed)
                {
                    query = query.Where(r => !r.IsDismissed);
                }
            }

            if (filter.EmployeeId.HasValue)
            {
                query = query.Where(r => r.EmployeeId == filter.EmployeeId.Value);
            }

            if (filter.DepartmentId.HasValue)
            {
                query = query.Where(r => r.Employee != null && r.Employee.DepartmentId == filter.DepartmentId.Value);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= filter.EndDate.Value);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                switch (filter.SortBy.ToLower())
                {
                    case "createdat":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt);
                        break;
                    case "employeename":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(r => r.Employee!.FullName) : query.OrderBy(r => r.Employee!.FullName);
                        break;
                    case "status":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status);
                        break;
                    default:
                        query = query.OrderByDescending(r => r.CreatedAt); // Default sort
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(r => r.CreatedAt); // Default sort
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResultDto<Request>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }
    }
}