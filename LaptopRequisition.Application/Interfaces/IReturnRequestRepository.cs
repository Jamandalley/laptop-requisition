using LaptopRequisition.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs.Request; // Added for HistoryFilterDto
using LaptopRequisition.Application.DTOs; // Added for PaginatedResultDto
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page; // Added for AdminReturnRequestFilterDto

namespace LaptopRequisition.Application.Interfaces
{
    public interface IReturnRequestRepository
    {
        Task AddAsync(ReturnRequest returnRequest);
        Task UpdateAsync(ReturnRequest returnRequest);
        Task<ReturnRequest?> GetByIdAsync(Guid id, bool includeRelatedEntities = false); // FIX: Added includeRelatedEntities
        // Removed: Task<IEnumerable<ReturnRequest>> GetByEmployeeIdAsync(Guid employeeId); // Replaced by paginated version
        // Removed: Task<IEnumerable<ReturnRequest>> GetAllAsync(); // Replaced by GetFilteredAndPaginatedReturnRequestsAsync
        Task DeleteAsync(Guid id);
        Task<ReturnRequest?> GetPendingReturnRequestByLaptopIdAsync(Guid laptopId); // Existing method

        // New methods for History
        // FIX: Modified GetEmployeeReturnRequestsAsync to include pagination and filtering
        Task<PaginatedResultDto<ReturnRequest>> GetEmployeeReturnRequestsAsync(Guid employeeId, HistoryFilterDto filter);
        Task<ReturnRequest?> GetReturnRequestWithLaptopAndEmployeeAsync(Guid returnRequestId);

        // New method for DashboardService
        Task<ReturnRequest?> GetPendingReturnRequestByEmployeeIdAsync(Guid employeeId); // Added

        // New method for Admin Request Management (used for reporting now)
        Task<PaginatedResultDto<ReturnRequest>> GetFilteredAndPaginatedReturnRequestsAsync(AdminReturnRequestFilterDto filter);
    }
}