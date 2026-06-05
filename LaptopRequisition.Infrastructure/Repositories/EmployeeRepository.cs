using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page;
using LaptopRequisition.Domain.Enums; // Added for LaptopStatus

namespace LaptopRequisition.Infrastructure.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly ApplicationDbContext _context;

        public EmployeeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Employee?> GetByIdAsync(Guid id)
        {
            return await _context.Employees
                                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Employee?> GetByStaffIdAsync(string staffId)
        {
            return await _context.Employees
                                 .FirstOrDefaultAsync(e => e.StaffId == staffId);
        }

        public async Task<Employee?> GetByEmailAsync(string email)
        {
            return await _context.Employees
                                .FirstOrDefaultAsync(e => e.Email == email);
        }

        public async Task<IEnumerable<Employee>> GetAllAsync()
        {
            return await _context.Employees
                                 .ToListAsync();
        }

        public async Task AddAsync(Employee employee)
        {
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Employee employee)
        {
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
        }

       public async Task SoftDeleteAsync(Guid employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee != null)
            {
                employee.IsDeleted = true; 
                employee.UpdatedAt = DateTime.UtcNow;
                _context.Employees.Update(employee);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Employee?> GetByEmailWithDepartmentAndRoleAsync(string email)
        {
            return await _context.Employees
                                 .Include(e => e.Department)
                                 .Include(e => e.Role)
                                 .FirstOrDefaultAsync(e => e.Email == email);
        }

        public async Task<Employee?> GetByIdWithDepartmentAndRoleAsync(Guid employeeId)
        {
            return await _context.Employees
                                 .Include(e => e.Department)
                                 .Include(e => e.Role)
                                 .FirstOrDefaultAsync(e => e.Id == employeeId);
        }
        
        public async Task<int> CountAllAsync()
        {
            return await _context.Employees.CountAsync();
        }
        
        public async Task<int> CountActiveUsersAsync()
        {
            return await _context.Employees.CountAsync(e => !e.IsLocked);
        }

        public async Task<int> CountPendingOnboardingAsync()
        {
           return await _context.Employees.CountAsync(e => !e.IsVerified && e.IsFirstLogin);
        }

        public async Task<int> CountUsersWithAssignedLaptopsAsync()
        {
            return await _context.Laptops.Where(l => l.AssignedToEmployeeId != null).Select(l => l.AssignedToEmployeeId).Distinct().CountAsync();
        }

        public async Task<int> CountUsersWithoutLaptopsAsync()
        {
            
            var employeesWithLaptops = await _context.Laptops
                                                     .Where(l => l.AssignedToEmployeeId != null)
                                                     .Select(l => l.AssignedToEmployeeId)
                                                     .Distinct()
                                                     .ToListAsync();
            
            return await _context.Employees
                                 .CountAsync(e => !e.IsLocked && !employeesWithLaptops.Contains(e.Id));
        }
        
        public async Task<IEnumerable<Employee>> GetAllWithDepartmentAndRoleAsync()
        {
            return await _context.Employees
                                 .Include(e => e.Department)
                                 .Include(e => e.Role)
                                 .ToListAsync();
        }

        public async Task<PaginatedResultDto<Employee>> GetFilteredAndPaginatedEmployeesAsync(EmployeeFilterDto filter)
        {
            IQueryable<Employee> query = _context.Employees
                                                 .Include(e => e.Department)
                                                 .Include(e => e.Role);

           
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(e => e.FullName.Contains(filter.SearchTerm) ||
                                         e.StaffId.Contains(filter.SearchTerm) ||
                                         e.Email.Contains(filter.SearchTerm));
            }

           
            if (filter.DepartmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == filter.DepartmentId.Value);
            }

            if (filter.RoleId.HasValue)
            {
                query = query.Where(e => e.RoleId == filter.RoleId.Value);
            }

              if (filter.IsActive.HasValue)
            {
                query = query.Where(e => !e.IsLocked == filter.IsActive.Value);
            }
            
            if (filter.IsVerified.HasValue)
            {
                query = query.Where(e => e.IsVerified == filter.IsVerified.Value);
            }

           
            if (filter.HasAssignedLaptop.HasValue)
            {
                var employeesWithLaptops = await _context.Laptops
                                                         .Where(l => l.AssignedToEmployeeId != null)
                                                         .Select(l => l.AssignedToEmployeeId!.Value)
                                                         .Distinct()
                                                         .ToListAsync();
                if (filter.HasAssignedLaptop.Value)
                {
                    query = query.Where(e => employeesWithLaptops.Contains(e.Id));
                }
                else
                {
                    query = query.Where(e => !employeesWithLaptops.Contains(e.Id));
                }
            }

           
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                 switch (filter.SortBy.ToLower())
                {
                    case "fullname":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(e => e.FullName) : query.OrderBy(e => e.FullName);
                        break;
                    case "staffid":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(e => e.StaffId) : query.OrderBy(e => e.StaffId);
                        break;
                    case "email":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(e => e.Email) : query.OrderBy(e => e.Email);
                        break;
                    case "departmentname":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(e => e.Department!.Name) : query.OrderBy(e => e.Department!.Name);
                        break;
                    case "rolename":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(e => e.Role!.Name) : query.OrderBy(e => e.Role!.Name);
                        break;
                    case "createdat":
                        query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.CreatedAt);
                        break;
                    default:
                        query = query.OrderBy(e => e.FullName); // Default sort
                        break;
                }
            }
            else
            {
                query = query.OrderBy(e => e.FullName); // Default sort
            }

            var totalCount = await query.CountAsync();

            var items = await query.Skip((filter.PageNumber - 1) * filter.PageSize)
                                   .Take(filter.PageSize)
                                   .ToListAsync();

            return new PaginatedResultDto<Employee>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task RestoreAsync(Guid employeeId)
        {
            var employee = await _context.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == employeeId && e.IsDeleted);
            if (employee != null)
            {
                employee.IsDeleted = false; 
                employee.UpdatedAt = DateTime.UtcNow;
                _context.Employees.Update(employee);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Employee>> GetDeletedEmployeesAsync()
        {
           return await _context.Employees.IgnoreQueryFilters()
                                 .Where(e => e.IsDeleted)
                                 .Include(e => e.Department)
                                 .Include(e => e.Role)
                                 .ToListAsync();
        }

        public async Task<Employee?> GetByIdIncludingDeletedAsync(Guid id)
        {
            return await _context.Employees.IgnoreQueryFilters()
                                 .Include(e => e.Department)
                                 .Include(e => e.Role)
                                 .FirstOrDefaultAsync(e => e.Id == id);
        }

       
        public async Task<IEnumerable<Employee>> GetEmployeesByRoleIdAsync(Guid roleId)
        {
            return await _context.Employees
                                 .Where(e => e.RoleId == roleId)
                                 .ToListAsync();
        }

       
        public async Task HardDeleteAsync(Guid employeeId)
        {
            var employee = await _context.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee != null)
            {
                // 1. Unassign any laptops assigned to this employee
                var assignedLaptops = await _context.Laptops
                                                    .Where(l => l.AssignedToEmployeeId == employeeId)
                                                    .ToListAsync();
                foreach (var laptop in assignedLaptops)
                {
                    laptop.AssignedToEmployeeId = null;
                    laptop.AssignedAt = null;
                    laptop.Status = LaptopStatus.Available; // Set status back to Available
                    _context.Laptops.Update(laptop);
                }

                // 2. Disassociate Requests and ReturnRequests (set EmployeeId to null)
                var requests = await _context.Requests
                                             .Where(r => r.EmployeeId == employeeId)
                                             .ToListAsync();
                foreach (var request in requests)
                {
                    request.EmployeeId = null;
                    _context.Requests.Update(request);
                }

                var returnRequests = await _context.ReturnRequests
                                                   .Where(rr => rr.EmployeeId == employeeId)
                                                   .ToListAsync();
                foreach (var returnRequest in returnRequests)
                {
                    returnRequest.EmployeeId = null;
                    _context.ReturnRequests.Update(returnRequest);
                }

                // 3. Delete Notifications associated with this employee
                var notifications = await _context.Notifications
                                                  .Where(n => n.EmployeeId == employeeId)
                                                  .ToListAsync();
                _context.Notifications.RemoveRange(notifications);

                // 4. Delete PasswordResetTokens associated with this employee
                var passwordResetTokens = await _context.PasswordResetTokens
                                                        .Where(prt => prt.EmployeeId == employeeId)
                                                        .ToListAsync();
                _context.PasswordResetTokens.RemoveRange(passwordResetTokens);

                // 5. Finally, remove the employee
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateLoginAttemptsAsync(Employee employee)
        {
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
        }

        public async Task<PaginatedResultDto<Employee>> GetFilteredAsync(EmployeeFilterDto filter)
        {
            var query = _context.Employees
                .Include(x => x.Department)
                .Include(x => x.Role)
                // .Include(x => x.Laptops).ThenInclude(a => a.Laptop)
                .AsQueryable();

            // -------------------------
            // SEARCH
            // -------------------------
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();

                query = query.Where(x =>
                    x.FullName.ToLower().Contains(term) ||
                    x.Email.ToLower().Contains(term) ||
                    x.StaffId.ToLower().Contains(term));
            }

            // -------------------------
            // FILTERS
            // -------------------------
            if (filter.DepartmentId.HasValue)
                query = query.Where(x => x.DepartmentId == filter.DepartmentId);

            if (filter.RoleId.HasValue)
                query = query.Where(x => x.RoleId == filter.RoleId);
            
            if (filter.IsVerified.HasValue)
                query = query.Where(x => x.IsVerified == filter.IsVerified);

            // if (filter.HasAssignedLaptop.HasValue)
            // {
            //     if (filter.HasAssignedLaptop.Value)
            //     {
            //         query = query.Where(e => e.Laptops.Any());
            //     }
            //     else
            //     {
            //         query = query.Where(e => !e.Laptops.Any());
            //     }
            // }

            // -------------------------
            // SORTING
            // -------------------------
            query = ApplySorting(query, filter.SortBy, filter.SortOrder);

            // -------------------------
            // COUNT (before pagination)
            // -------------------------
            var totalCount = await query.CountAsync();

            // -------------------------
            // PAGINATION
            // -------------------------
            var employees = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResultDto<Employee>
            {
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount,
                Items = employees
            };
        }
        
        private IQueryable<Employee> ApplySorting(
            IQueryable<Employee> query,
            string? sortBy,
            string? sortOrder)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderBy(x => x.FullName);

            var isDesc = sortOrder?.ToLower() == "desc";

            return sortBy.ToLower() switch
            {
                "fullname" => isDesc ? query.OrderByDescending(x => x.FullName) : query.OrderBy(x => x.FullName),
                "email" => isDesc ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
                "staffid" => isDesc ? query.OrderByDescending(x => x.StaffId) : query.OrderBy(x => x.StaffId),
                "createdat" => isDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
                _ => query.OrderBy(x => x.FullName)
            };
        }
    }
}