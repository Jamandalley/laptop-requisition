using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Application.DTOs.Request; // Added
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page; // Added for AdminRequestFilterDto
using LaptopRequisition.Domain.Common; // NEW: Added for Response<T>

namespace LaptopRequisition.Application.Interfaces
{
    public interface IRequestService
    {
        Task<Response<RequestResponseDto>> CreateRequestAsync(CreateRequestDto dto);

        Task<Response<RequestResponseDto>> GetRequestByIdAsync(Guid id);

        // FIX: Modified GetEmployeeRequestsAsync to include pagination and filtering
        Task<Response<PaginatedResultDto<RequestResponseDto>>> GetEmployeeRequestsAsync(Guid employeeId, RequestFilterDto filter);

        // NEW: Modified to include pagination and filtering for general requests
        Task<Response<PaginatedResultDto<RequestResponseDto>>> GetAllRequestsAsync(RequestFilterDto filter);

        Task<Response> ApproveRequestAsync(Guid requestId);

        Task<Response> RejectRequestAsync(Guid requestId, string reason);

        Task<Response> AssignLaptopAsync(Guid requestId, Guid laptopId);

        // New methods for Request Management
        Task<Response<RequestStatusDetailDto>> GetEmployeeRequestStatusDetailAsync(Guid employeeId);
        Task<Response> DismissRejectedRequestAsync(Guid requestId, Guid employeeId);
        Task<Response> ConfirmReceiptAsync(Guid requestId, Guid employeeId);

        // New method for History
        Task<Response<PaginatedResultDto<RequestHistoryDto>>> GetEmployeeHistoryAsync(Guid employeeId, HistoryFilterDto filter);
        Task<Response<RequestHistoryDto>> GetHistoryItemByIdAsync(Guid id, Guid employeeId); // Added

        // New method for Export
        Task<Response<byte[]>> ExportEmployeeHistoryAsync(Guid employeeId, HistoryFilterDto filter);

        // New method for Reporting Issue
        Task<Response> ReportIssueAsync(Guid employeeId, ReportIssueDto dto);

        // New method for Admin Request Management
        Task<Response<PaginatedResultDto<RequestResponseDto>>> GetFilteredAndPaginatedRequestsForAdminAsync(AdminRequestFilterDto filter);
        Task<Response<byte[]>> ExportFilteredRequestsForAdminAsync(AdminRequestFilterDto filter); // New method
    }
}