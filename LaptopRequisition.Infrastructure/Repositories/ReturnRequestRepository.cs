using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain;
using LaptopRequisition.Domain.Enums; 
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs.Request; 
using LaptopRequisition.Application.DTOs; 
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page; // Added for AdminReturnRequestFilterDto

namespace LaptopRequisition.Infrastructure.Repositories
{
    public class ReturnRequestRepository : IReturnRequestRepository
    {
        private readonly ApplicationDbContext _context;

        public ReturnRequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ReturnRequest returnRequest)
        {
            await _context.ReturnRequests.AddAsync(returnRequest);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ReturnRequest returnRequest)
        {
            _context.ReturnRequests.Update(returnRequest);
            await _context.SaveChangesAsync();
        }

        public async Task<ReturnRequest?> GetByIdAsync(Guid id)
        {
            return await _context.ReturnRequests
                                 .Include(rr => rr.Employee) // Include Employee for mapping
                                 .Include(rr => rr.Laptop)   // Include Laptop for mapping
                                 .FirstOrDefaultAsync(rr => rr.Id == id);
        }

        public async Task<IEnumerable<ReturnRequest>> GetByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.ReturnRequests
                                 .Where(rr => rr.EmployeeId == employeeId)
                                 .Include(rr => rr.Employee)
                                 .Include(rr => rr.Laptop)
                                 .OrderByDescending(rr => rr.CreatedAt)
                                 .ToListAsync();
        }

        public async Task<IEnumerable<ReturnRequest>> GetAllAsync()
        {
            return await _context.ReturnRequests
                                 .Include(rr => rr.Employee)
                                 .Include(rr => rr.Laptop)
                                 .OrderByDescending(rr => rr.CreatedAt)
                                 .ToListAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var returnRequest = await _context.ReturnRequests.FindAsync(id);
            if (returnRequest != null)
            {
                _context.ReturnRequests.Remove(returnRequest);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<ReturnRequest?> GetPendingReturnRequestByLaptopIdAsync(Guid laptopId) // Implemented this method
        {
            return await _context.ReturnRequests
                                 .Where(rr => rr.LaptopId == laptopId && rr.Status == ReturnRequestStatus.Pending.ToString())
                                 .FirstOrDefaultAsync();
        }

     
        public async Task<ReturnRequest?> GetPendingReturnRequestByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.ReturnRequests
                                 .Where(rr => rr.EmployeeId == employeeId && rr.Status == ReturnRequestStatus.Pending.ToString())
                                 .FirstOrDefaultAsync();
        }
        
        public async Task<PaginatedResultDto<ReturnRequest>> GetEmployeeReturnRequestsAsync(Guid employeeId, HistoryFilterDto filter)
        {
            IQueryable<ReturnRequest> query = _context.ReturnRequests
                .Where(rr => rr.EmployeeId == employeeId)
                .Include(rr => rr.Laptop)
                .Include(rr => rr.Employee);
            
            if (filter.StartDate.HasValue)
            {
                query = query.Where(rr => rr.CreatedAt >= filter.StartDate.Value);
            }
            if (filter.EndDate.HasValue)
            {
                query = query.Where(rr => rr.CreatedAt <= filter.EndDate.Value);
            }
            if (filter.Status.HasValue) 
            {
                query = query.Where(rr => rr.Status == filter.Status.Value.ToString());
            }
            
            query = query.OrderByDescending(rr => rr.CreatedAt);

             var totalCount = await query.CountAsync();

            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResultDto<ReturnRequest>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<ReturnRequest?> GetReturnRequestWithLaptopAndEmployeeAsync(Guid returnRequestId)
        {
            return await _context.ReturnRequests
                .Include(rr => rr.Laptop)
                .Include(rr => rr.Employee)
                .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);
        }

        // New method for Admin Request Management
        public async Task<PaginatedResultDto<ReturnRequest>> GetFilteredAndPaginatedReturnRequestsAsync(AdminReturnRequestFilterDto filter)
        {
            IQueryable<ReturnRequest> query = _context.ReturnRequests
                .Include(rr => rr.Employee)
                .Include(rr => rr.Laptop);

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(rr => rr.Employee != null &&
                                         (rr.Employee.FullName.Contains(filter.SearchTerm) ||
                                          rr.Employee.StaffId.Contains(filter.SearchTerm) ||
                                          rr.Laptop != null && rr.Laptop.SerialNumber.Contains(filter.SearchTerm)));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(rr => rr.Status == filter.Status.Value.ToString());
            }

            if (filter.EmployeeId.HasValue)
            {
                query = query.Where(rr => rr.EmployeeId == filter.EmployeeId.Value);
            }

            if (filter.DepartmentId.HasValue)
            {
                query = query.Where(rr => rr.Employee != null && rr.Employee.DepartmentId == filter.DepartmentId.Value);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(rr => rr.CreatedAt >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(rr => rr.CreatedAt <= filter.EndDate.Value);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                switch (filter.SortBy.ToLower())
                {
                    case "createdat":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(rr => rr.CreatedAt) : query.OrderBy(rr => rr.CreatedAt);
                        break;
                    case "employeename":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(rr => rr.Employee!.FullName) : query.OrderBy(rr => rr.Employee!.FullName);
                        break;
                    case "status":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(rr => rr.Status) : query.OrderBy(rr => rr.Status);
                        break;
                    default:
                        query = query.OrderByDescending(rr => rr.CreatedAt); // Default sort
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(rr => rr.CreatedAt); // Default sort
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResultDto<ReturnRequest>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }
    }
}