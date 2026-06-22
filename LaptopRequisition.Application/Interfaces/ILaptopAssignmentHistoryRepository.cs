using LaptopRequisition.Domain;
using System;
using System.Threading.Tasks;

namespace LaptopRequisition.Application.Interfaces
{
    public interface ILaptopAssignmentHistoryRepository
    {
        Task AddAsync(LaptopAssignmentHistory history);
        Task MarkReturnedAsync(Guid laptopId, DateTime returnedAt);
    }
}
