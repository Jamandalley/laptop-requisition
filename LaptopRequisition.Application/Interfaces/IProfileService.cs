using LaptopRequisition.Application.DTOs.Employee;
using System;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page;
using LaptopRequisition.Domain.Common;
using Microsoft.AspNetCore.Http; // Added for IFormFile

namespace LaptopRequisition.Application.Interfaces
{
    public interface IProfileService
    {
        Task<Response<ProfileDto>> GetProfileAsync(Guid employeeId);
        Task<Response> UpdateProfileAsync(Guid employeeId, UpdateProfileDto dto);
        Task<Response<string>> UploadProfilePictureAsync(Guid employeeId, IFormFile file); // Returns URL or path
        Task<Response> RemoveProfilePictureAsync(Guid employeeId);
        Task<Response<PaginatedResultDto<ProfileDto>>> GetProfilesAsync(EmployeeFilterDto filter);
    }
}