using System.Collections.Generic; // Added for IEnumerable
using System.Threading.Tasks; // Added for Task
using System; // Added for Guid
using LaptopRequisition.Application.DTOs.Laptop; // Added for Laptop DTOs
using LaptopRequisition.Application.DTOs; // Added for PaginatedResultDto
using Microsoft.AspNetCore.Http; // Added for IFormFile
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page; // Added for BulkUploadResultDto

namespace LaptopRequisition.Application.Interfaces;

public interface ILaptopService
{
    Task<LaptopResponseDto> CreateLaptopAsync(CreateLaptopDto dto);
    Task<IEnumerable<LaptopResponseDto>> GetAllLaptopsAsync();
    Task<LaptopResponseDto> GetLaptopByIdAsync(Guid id);
    Task<LaptopResponseDto> UpdateLaptopAsync(Guid id, UpdateLaptopDto dto, Guid userId, string userName); // Changed signature
    Task DeleteLaptopAsync(Guid id);

    // New methods for admin laptop assignment
    Task AdminAssignLaptopAsync(Guid laptopId, Guid employeeId);
    Task AdminUnassignLaptopAsync(Guid laptopId);

    // New methods for filtered and paginated laptops
    Task<PaginatedResultDto<LaptopResponseDto>> GetFilteredAndPaginatedLaptopsAsync(LaptopFilterDto filter);

    // New method for bulk laptop upload
    Task<List<BulkUploadResultDto>> BulkUploadLaptopsAsync(IFormFile csvFile);

    // New method for exporting filtered laptops
    Task<byte[]> ExportFilteredLaptopsAsync(LaptopFilterDto filter);
}