using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Application.DTOs.Request; // Added
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page; // Added for AdminRequestFilterDto

namespace LaptopRequisition.Application.Interfaces
{
    public interface IRequestService
    {
        Task<RequestResponseDto> CreateRequestAsync(CreateRequestDto dto);

        Task<RequestResponseDto> GetRequestByIdAsync(Guid id);

        Task<IEnumerable<RequestResponseDto>> GetEmployeeRequestsAsync(Guid employeeId);

        Task<IEnumerable<RequestResponseDto>> GetAllRequestsAsync();

        Task ApproveRequestAsync(Guid requestId);

        Task RejectRequestAsync(Guid requestId, string reason);

        Task AssignLaptopAsync(Guid requestId, Guid laptopId);

        // New methods for Request Management
        Task<RequestStatusDetailDto> GetEmployeeRequestStatusDetailAsync(Guid employeeId);
        Task DismissRejectedRequestAsync(Guid requestId, Guid employeeId);
        Task ConfirmReceiptAsync(Guid requestId, Guid employeeId);

        // New method for History
        Task<PaginatedResultDto<RequestHistoryDto>> GetEmployeeHistoryAsync(Guid employeeId, HistoryFilterDto filter);
        Task<RequestHistoryDto> GetHistoryItemByIdAsync(Guid id, Guid employeeId); // Added

        // New method for Export
        Task<byte[]> ExportEmployeeHistoryAsync(Guid employeeId, HistoryFilterDto filter);

        // New method for Reporting Issue
        Task ReportIssueAsync(Guid employeeId, ReportIssueDto dto);

        // New method for Admin Request Management
        Task<PaginatedResultDto<RequestResponseDto>> GetFilteredAndPaginatedRequestsForAdminAsync(AdminRequestFilterDto filter);
        Task<byte[]> ExportFilteredRequestsForAdminAsync(AdminRequestFilterDto filter); // New method
    }
}