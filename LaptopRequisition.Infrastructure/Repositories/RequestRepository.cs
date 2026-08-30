using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain;
using Microsoft.EntityFrameworkCore;
using LaptopRequisition.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs.Request;
using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page;

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

        public async Task<Request?> GetByIdAsync(Guid id, bool includeRelatedEntities = false)
        {
            IQueryable<Request> query = _context.Requests;

            if (includeRelatedEntities)
            {
                query = query.Include(r => r.Employee)
                             .ThenInclude(e => e.Department)
                             .Include(r => r.Employee)
                             .ThenInclude(e => e.Role)
                             .Include(r => r.Laptop);
            }

            return await query.FirstOrDefaultAsync(r => r.Id == id); // FIX: Ensure query is materialized
        }

        public async Task<PaginatedResultDto<Request>> GetByEmployeeIdAsync(Guid employeeId, RequestFilterDto filter)
        {
            IQueryable<Request> query = _context.Requests
                .Where(r => r.EmployeeId == employeeId)
                .Include(r => r.Employee)
                .ThenInclude(e => e.Department)
                .Include(r => r.Employee)
                .ThenInclude(e => e.Role)
                .Include(r => r.Laptop)
                .AsQueryable();

            // Apply filters from RequestFilterDto
            if (filter.Status.HasValue)
            {
                query = query.Where(r => r.Status == filter.Status.Value);
            }
            if (filter.StartDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= filter.StartDate.Value);
            }
            if (filter.EndDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= filter.EndDate.Value);
            }
            if (filter.IsSwapRequest.HasValue)
            {
                query = query.Where(r => r.IsSwapRequest == filter.IsSwapRequest.Value);
            }
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTermLower = filter.SearchTerm.ToLower();
                query = query.Where(r => r.Purpose.ToLower().Contains(searchTermLower) ||
                                         r.PreferredSpecs.ToLower().Contains(searchTermLower) ||
                                         (r.Employee != null &&
                                            (r.Employee.FullName.ToLower().Contains(searchTermLower) ||
                                             r.Employee.StaffId.ToLower().Contains(searchTermLower) ||
                                             r.Employee.Email.ToLower().Contains(searchTermLower))) ||
                                         (r.Laptop != null && r.Laptop.SerialNumber.ToLower().Contains(searchTermLower)));
            }

            // Apply sorting
            query = ApplySorting(query, filter.SortBy, filter.SortOrder);

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

        public async Task<int> CountByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.Requests
                .CountAsync(r => r.EmployeeId == employeeId);
        }

        public async Task<Request?> GetLatestRequestByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.Requests
                .Where(r => r.EmployeeId == employeeId)
                .OrderByDescending(r => r.UpdatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<PaginatedResultDto<Request>> GetEmployeeRequestsAsync(Guid employeeId, HistoryFilterDto filter)
        {
            IQueryable<Request> query = _context.Requests
                .Where(r => r.EmployeeId == employeeId)
                .Include(r => r.Laptop)
                .Include(r => r.Employee)
                .AsQueryable();

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
                if (filter.RequestType.Equals("LaptopRequest", StringComparison.OrdinalIgnoreCase))
                {
                    // This query already only gets Requests, so no additional filter needed here
                }
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
                .FirstOrDefaultAsync(r => r.Id == requestId); // FIX: Ensure query is materialized
        }

        public async Task<int> CountByStatusAsync(RequestStatus status)
        {
            return await _context.Requests.CountAsync(r => r.Status == status);
        }

        public async Task<PaginatedResultDto<Request>> GetFilteredAndPaginatedRequestsAsync(RequestFilterDto filter)
        {
            IQueryable<Request> query = _context.Requests
                .Include(r => r.Employee)
                .ThenInclude(e => e.Department)
                .Include(r => r.Employee)
                .ThenInclude(e => e.Role)
                .Include(r => r.Laptop)
                .AsQueryable();

            // Apply search term
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTermLower = filter.SearchTerm.ToLower();
                query = query.Where(r => r.Purpose.ToLower().Contains(searchTermLower) ||
                                         r.PreferredSpecs.ToLower().Contains(searchTermLower) ||
                                         (r.Employee != null &&
                                            (r.Employee.FullName.ToLower().Contains(searchTermLower) ||
                                             r.Employee.StaffId.ToLower().Contains(searchTermLower) ||
                                             r.Employee.Email.ToLower().Contains(searchTermLower))) ||
                                         (r.Laptop != null && r.Laptop.SerialNumber.ToLower().Contains(searchTermLower)));
            }

            // Apply filters
            if (filter.Status.HasValue)
            {
                query = query.Where(r => r.Status == filter.Status.Value);
            }
            if (filter.EmployeeId.HasValue)
            {
                query = query.Where(r => r.EmployeeId == filter.EmployeeId.Value);
            }
            if (filter.IsSwapRequest.HasValue)
            {
                query = query.Where(r => r.IsSwapRequest == filter.IsSwapRequest.Value);
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
            query = ApplySorting(query, filter.SortBy, filter.SortOrder);

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

        public async Task<PaginatedResultDto<Request>> GetFilteredAndPaginatedRequestsForAdminAsync(AdminRequestFilterDto filter)
        {
            IQueryable<Request> query = _context.Requests
                .Include(r => r.Employee)
                .ThenInclude(e => e.Department)
                .Include(r => r.Employee)
                .ThenInclude(e => e.Role)
                .Include(r => r.Laptop)
                .AsQueryable();

            // Apply search term
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTermLower = filter.SearchTerm.ToLower();
                query = query.Where(r => r.Purpose.ToLower().Contains(searchTermLower) ||
                                         r.PreferredSpecs.ToLower().Contains(searchTermLower) ||
                                         (r.Employee != null &&
                                            (r.Employee.FullName.ToLower().Contains(searchTermLower) ||
                                             r.Employee.StaffId.ToLower().Contains(searchTermLower) ||
                                             r.Employee.Email.ToLower().Contains(searchTermLower))) ||
                                         (r.Laptop != null && r.Laptop.SerialNumber.ToLower().Contains(searchTermLower)));
            }

            // Apply filters
            if (filter.Status.HasValue)
            {
                query = query.Where(r => r.Status == filter.Status.Value);
            }
            else
            {
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
            query = ApplySorting(query, filter.SortBy, filter.SortOrder);

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

        private IQueryable<Request> ApplySorting(
            IQueryable<Request> query,
            string? sortBy,
            string? sortOrder)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderByDescending(r => r.CreatedAt);

            var isDesc = sortOrder?.ToLower() == "desc";

            return sortBy.ToLower() switch
            {
                "createdat" => isDesc ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt),
                "employeename" => isDesc ? query.OrderByDescending(r => r.Employee!.FullName) : query.OrderBy(r => r.Employee!.FullName),
                "status" => isDesc ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
                _ => query.OrderByDescending(r => r.CreatedAt) // Default sort
            };
        }
    }
}