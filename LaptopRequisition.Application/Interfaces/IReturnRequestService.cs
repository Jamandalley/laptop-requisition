using LaptopRequisition.Application.DTOs;
using System; // Added for Guid
using System.Collections.Generic; // Added for IEnumerable
using System.Threading.Tasks; // Added for Task
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page; // Added for AdminReturnRequestFilterDto and ApproveReturnRequestDto

namespace LaptopRequisition.Application.Interfaces
{
    public interface IReturnRequestService
    {
        Task<ReturnRequestResponseDto> CreateReturnRequestAsync(CreateReturnRequestDto dto);
        Task<ReturnRequestResponseDto> GetReturnRequestByIdAsync(Guid id);
        Task<IEnumerable<ReturnRequestResponseDto>> GetEmployeeReturnRequestsAsync(Guid employeeId);
        Task<IEnumerable<ReturnRequestResponseDto>> GetAllReturnRequestsAsync();
        Task ApproveReturnRequestAsync(ApproveReturnRequestDto dto); // Changed signature
        Task RejectReturnRequestAsync(Guid returnRequestId, string reason);
        Task DeleteReturnRequestAsync(Guid returnRequestId);

        // New method for Admin Request Management
        Task<PaginatedResultDto<ReturnRequestResponseDto>> GetFilteredAndPaginatedReturnRequestsForAdminAsync(AdminReturnRequestFilterDto filter);
        Task<byte[]> ExportFilteredReturnRequestsForAdminAsync(AdminReturnRequestFilterDto filter); // New method
    }
}