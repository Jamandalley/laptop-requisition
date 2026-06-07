using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain;
using Microsoft.EntityFrameworkCore;
using System; // Added for Guid
using System.Collections.Generic; // Added for IEnumerable
using System.Linq; // Added for LINQ
using System.Threading.Tasks; // Added for Task

namespace LaptopRequisition.Infrastructure.Repositories
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly ApplicationDbContext _context;

        public DepartmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Department?> GetByIdAsync(Guid id) // Changed to nullable
        {
            return await _context.Departments.FindAsync(id);
        }

        public async Task<Department?> GetByNameAsync(string name) // Changed to nullable
        {
            return await _context.Departments.FirstOrDefaultAsync(d => d.Name == name);
        }

        public async Task<IEnumerable<Department>> GetAllAsync()
        {
            return await _context.Departments.ToListAsync();
        }

        public async Task AddAsync(Department department)
        {
            await _context.Departments.AddAsync(department);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Department department)
        {
            _context.Departments.Update(department);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> AnyEmployeesInDepartmentAsync(Guid departmentId) // NEW: Implementation
        {
            return await _context.Employees.AnyAsync(e => e.DepartmentId == departmentId);
        }
    }
}