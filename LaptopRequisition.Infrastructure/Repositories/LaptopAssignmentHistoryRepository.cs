using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LaptopRequisition.Infrastructure.Repositories
{
    public class LaptopAssignmentHistoryRepository : ILaptopAssignmentHistoryRepository
    {
        private readonly ApplicationDbContext _context;

        public LaptopAssignmentHistoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(LaptopAssignmentHistory history)
        {
            await _context.LaptopAssignmentHistories.AddAsync(history);
            await _context.SaveChangesAsync();
        }

        public async Task MarkReturnedAsync(Guid laptopId, DateTime returnedAt)
        {
            var activeHistory = await _context.LaptopAssignmentHistories
                .Where(h => h.LaptopId == laptopId && h.ReturnedAt == null)
                .OrderByDescending(h => h.AssignedAt)
                .FirstOrDefaultAsync();

            if (activeHistory != null)
            {
                activeHistory.ReturnedAt = returnedAt;
                _context.LaptopAssignmentHistories.Update(activeHistory);
                await _context.SaveChangesAsync();
            }
        }
    }
}
