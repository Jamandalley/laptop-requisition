using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Domain.Common; // NEW: Added for Response<T>
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LaptopRequisition.Application.Interfaces
{
    public interface IDepartmentService
    {
        Task<Response<DepartmentResponseDto>> CreateDepartmentAsync(CreateDepartmentDto createDepartmentDto);
        Task<Response<DepartmentResponseDto>> GetDepartmentByIdAsync(Guid id);
        Task<Response<IEnumerable<DepartmentResponseDto>>> GetAllDepartmentsAsync();
        Task<Response<DepartmentResponseDto>> UpdateDepartmentAsync(Guid id, UpdateDepartmentDto updateDepartmentDto);
        Task<Response> DeleteDepartmentAsync(Guid id);
    }
}