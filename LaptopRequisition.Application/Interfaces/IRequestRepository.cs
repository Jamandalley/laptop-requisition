using LaptopRequisition.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs.Request; // Added for HistoryFilterDto
using LaptopRequisition.Application.DTOs; // Added for PaginatedResultDto
using LaptopRequisition.Domain.Enums; // Added for RequestStatus
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page; // Added for AdminRequestFilterDto

namespace LaptopRequisition.Application.Interfaces
{
    public interface IRequestRepository
    {
        Task AddAsync(Request request);
        Task<Request?> GetByIdAsync(Guid id);
        Task<IEnumerable<Request>> GetAllAsync();
        Task<IEnumerable<Request>> GetByEmployeeIdAsync(Guid employeeId);
        Task<Request?> GetPendingRequestByEmployeeIdAsync(Guid employeeId);
        Task UpdateAsync(Request request);
        Task DeleteAsync(Guid id);
        Task<int> CountByEmployeeIdAsync(Guid employeeId);
        Task<Request?> GetPendingOrApprovedRequestByEmployeeIdAsync(Guid employeeId);

        // New methods for History
        Task<PaginatedResultDto<Request>> GetEmployeeRequestsAsync(Guid employeeId, HistoryFilterDto filter);
        Task<Request?> GetRequestWithLaptopAndEmployeeAsync(Guid requestId);

        // New method for Admin Dashboard
        Task<int> CountByStatusAsync(RequestStatus status);

        // New method for Admin Request Management
        Task<PaginatedResultDto<Request>> GetFilteredAndPaginatedRequestsAsync(AdminRequestFilterDto filter);
    }
}