using LaptopRequisition.Application.DTOs;
using System; // Added for Guid
using System.Collections.Generic; // Added for IEnumerable
using System.Threading.Tasks; // Added for Task
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page; // Added for AdminReturnRequestFilterDto and ApproveReturnRequestDto
using LaptopRequisition.Domain.Common; // NEW: Added for Response<T>
using LaptopRequisition.Application.DTOs.Request; // NEW: Added for HistoryFilterDto

namespace LaptopRequisition.Application.Interfaces
{
    public interface IReturnRequestService
    {
        Task<Response<ReturnRequestResponseDto>> CreateReturnRequestAsync(CreateReturnRequestDto dto);
        Task<Response<ReturnRequestResponseDto>> GetReturnRequestByIdAsync(Guid id);
        
        // FIX: Corrected signature for GetEmployeeReturnRequestsAsync
        Task<Response<PaginatedResultDto<ReturnRequestResponseDto>>> GetEmployeeReturnRequestsAsync(Guid employeeId, HistoryFilterDto filter);
        
        // FIX: Modified to include pagination and filtering for general return requests
        Task<Response<PaginatedResultDto<ReturnRequestResponseDto>>> GetAllReturnRequestsAsync(AdminReturnRequestFilterDto filter);

        Task<Response> ApproveReturnRequestAsync(ApproveReturnRequestDto dto); // Changed signature
        Task<Response> RejectReturnRequestAsync(Guid returnRequestId, string reason);
        Task<Response> DeleteReturnRequestAsync(Guid returnRequestId);

        // New method for Admin Request Management
        Task<Response<PaginatedResultDto<ReturnRequestResponseDto>>> GetFilteredAndPaginatedReturnRequestsForAdminAsync(AdminReturnRequestFilterDto filter);
        Task<Response<byte[]>> ExportFilteredReturnRequestsForAdminAsync(AdminReturnRequestFilterDto filter); // New method
    }
}