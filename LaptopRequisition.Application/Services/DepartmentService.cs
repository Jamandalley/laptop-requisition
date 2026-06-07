using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaptopRequisition.Domain.Common; // NEW: Added for Response<T>
using LaptopRequisition.Domain.Enums; // NEW: Added for ResponseCode

namespace LaptopRequisition.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;

    public DepartmentService(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<Response<DepartmentResponseDto>> CreateDepartmentAsync(CreateDepartmentDto createDepartmentDto)
    {
        var existingDepartment = await _departmentRepository.GetByNameAsync(createDepartmentDto.Name);
        if (existingDepartment != null)
        {
            return Response<DepartmentResponseDto>.Fail(ResponseCode.BadRequest, new List<string> { $"Department with name '{createDepartmentDto.Name}' already exists." }); // FIX
        }

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = createDepartmentDto.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _departmentRepository.AddAsync(department);
        return Response<DepartmentResponseDto>.Ok(new DepartmentResponseDto
        {
            Id = department.Id,
            Name = department.Name,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt
        });
    }

    public async Task<Response<DepartmentResponseDto>> GetDepartmentByIdAsync(Guid id)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
        {
            return Response<DepartmentResponseDto>.Fail(ResponseCode.NotFound, new List<string> { $"Department with ID '{id}' not found." }); // FIX
        }

        return Response<DepartmentResponseDto>.Ok(new DepartmentResponseDto
        {
            Id = department.Id,
            Name = department.Name,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt
        });
    }

    public async Task<Response<IEnumerable<DepartmentResponseDto>>> GetAllDepartmentsAsync()
    {
        var departments = await _departmentRepository.GetAllAsync();
        var departmentDtos = departments.Select(department => new DepartmentResponseDto
        {
            Id = department.Id,
            Name = department.Name,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt
        }).ToList();

        return Response<IEnumerable<DepartmentResponseDto>>.Ok(departmentDtos);
    }

    public async Task<Response<DepartmentResponseDto>> UpdateDepartmentAsync(Guid id, UpdateDepartmentDto updateDepartmentDto)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
        {
            return Response<DepartmentResponseDto>.Fail(ResponseCode.NotFound, new List<string> { $"Department with ID '{id}' not found." }); // FIX
        }

        var existingDepartmentWithName = await _departmentRepository.GetByNameAsync(updateDepartmentDto.Name);
        if (existingDepartmentWithName != null && existingDepartmentWithName.Id != id)
        {
            return Response<DepartmentResponseDto>.Fail(ResponseCode.BadRequest, new List<string> { $"Department with name '{updateDepartmentDto.Name}' already exists." }); // FIX
        }

        department.Name = updateDepartmentDto.Name;
        department.UpdatedAt = DateTime.UtcNow;

        await _departmentRepository.UpdateAsync(department);
        return Response<DepartmentResponseDto>.Ok(new DepartmentResponseDto
        {
            Id = department.Id,
            Name = department.Name,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt
        });
    }

    public async Task<Response> DeleteDepartmentAsync(Guid id)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
        {
            return Response.Fail(ResponseCode.NotFound, new List<string> { $"Department with ID '{id}' not found." }); // FIX
        }
       
        // FIX: Check if there are any employees associated with this department
        var hasEmployees = await _departmentRepository.AnyEmployeesInDepartmentAsync(id);
        if (hasEmployees)
        {
            return Response.Fail(ResponseCode.BadRequest, new List<string> { $"Cannot delete department with ID '{id}' because it has associated employees." }); // FIX
        }

        await _departmentRepository.DeleteAsync(id);
        return Response.Ok();
    }
}