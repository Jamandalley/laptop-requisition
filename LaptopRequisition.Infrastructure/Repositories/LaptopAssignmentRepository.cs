using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LaptopRequisition.Infrastructure.Repositories
{
    public class LaptopAssignmentRepository : ILaptopAssignmentRepository
    {
        private readonly ApplicationDbContext _context;

        public LaptopAssignmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(LaptopAssignments assignment)
        {
            await _context.LaptopAssignments.AddAsync(assignment);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(LaptopAssignments assignment)
        {
            _context.LaptopAssignments.Update(assignment);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(LaptopAssignments assignment)
        {
            _context.LaptopAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
        }

        public async Task<LaptopAssignments?> GetByIdAsync(Guid employeeId, Guid laptopId)
        {
            return await _context.LaptopAssignments
                                 .Include(la => la.Employee)
                                 .Include(la => la.Laptop)
                                 .FirstOrDefaultAsync(la => la.EmployeeId == employeeId && la.LaptopId == laptopId);
        }

        public async Task<LaptopAssignments?> GetCurrentAssignmentForEmployeeAsync(Guid employeeId)
        {
            return await _context.LaptopAssignments
                                 .Include(la => la.Laptop)
                                 .Include(la => la.Employee)
                                 .Where(la => la.EmployeeId == employeeId)
                                 .OrderByDescending(la => la.AssignedDate) // Assuming the most recent is the current
                                 .FirstOrDefaultAsync();
        }

        public async Task<LaptopAssignments?> GetCurrentAssignmentForLaptopAsync(Guid laptopId)
        {
            return await _context.LaptopAssignments
                                 .Include(la => la.Employee)
                                 .Include(la => la.Laptop)
                                 .Where(la => la.LaptopId == laptopId)
                                 .OrderByDescending(la => la.AssignedDate) // Assuming the most recent is the current
                                 .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<LaptopAssignments>> GetAllAssignmentsForEmployeeAsync(Guid employeeId)
        {
            return await _context.LaptopAssignments
                                 .Where(la => la.EmployeeId == employeeId)
                                 .Include(la => la.Laptop)
                                 .OrderByDescending(la => la.AssignedDate)
                                 .ToListAsync();
        }

        public async Task<bool> AnyAssignmentForEmployeeAsync(Guid employeeId)
        {
            return await _context.LaptopAssignments.AnyAsync(la => la.EmployeeId == employeeId);
        }

        public async Task<bool> AnyAssignmentForLaptopAsync(Guid laptopId)
        {
            return await _context.LaptopAssignments.AnyAsync(la => la.LaptopId == laptopId);
        }

        public async Task<IEnumerable<LaptopAssignments>> GetAllAsync()
        {
            return await _context.LaptopAssignments
                                 .Include(la => la.Employee)
                                 .Include(la => la.Laptop)
                                 .ToListAsync();
        }
    }
}