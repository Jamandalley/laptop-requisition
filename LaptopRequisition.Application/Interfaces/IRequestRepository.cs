using LaptopRequisition.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs.Request; // Added for HistoryFilterDto and RequestFilterDto
using LaptopRequisition.Application.DTOs; // Added for PaginatedResultDto
using LaptopRequisition.Domain.Enums; // Added for RequestStatus
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page; // Added for AdminRequestFilterDto

namespace LaptopRequisition.Application.Interfaces
{
    public interface IRequestRepository
    {
        Task AddAsync(Request request);
        Task<Request?> GetByIdAsync(Guid id, bool includeRelatedEntities = false); // Modified to include related entities
        // Removed: Task<IEnumerable<Request>> GetAllAsync(); // Replaced by GetFilteredAndPaginatedRequestsAsync
        // Modified: GetByEmployeeIdAsync to support filtering/pagination
        Task<PaginatedResultDto<Request>> GetByEmployeeIdAsync(Guid employeeId, RequestFilterDto filter); 
        Task<Request?> GetPendingRequestByEmployeeIdAsync(Guid employeeId);
        Task UpdateAsync(Request request);
        Task DeleteAsync(Guid id);
        Task<int> CountByEmployeeIdAsync(Guid employeeId);
        Task<Request?> GetPendingOrApprovedRequestByEmployeeIdAsync(Guid employeeId);

        // New methods for History (already paginated)
        Task<PaginatedResultDto<Request>> GetEmployeeRequestsAsync(Guid employeeId, HistoryFilterDto filter);
        Task<Request?> GetRequestWithLaptopAndEmployeeAsync(Guid requestId); // This method already includes related entities

        // New method for Admin Dashboard
        Task<int> CountByStatusAsync(RequestStatus status);

        // NEW: General purpose filtered and paginated requests
        Task<PaginatedResultDto<Request>> GetFilteredAndPaginatedRequestsAsync(RequestFilterDto filter);

        // New method for Admin Request Management (already exists, but ensure it's consistent)
        Task<PaginatedResultDto<Request>> GetFilteredAndPaginatedRequestsForAdminAsync(AdminRequestFilterDto filter);
    }
}