using LaptopRequisition.Application.DTOs.Employee;
using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Domain.Common;
using LaptopRequisition.Domain.Enums;
using Microsoft.Extensions.Logging;
using LaptopRequisition.Application.DTOs.Page;
using System.Linq; // Added for LINQ operations
using System.Collections.Generic; // Added for List<string>

namespace LaptopRequisition.Application.Services;

public class ProfileService(
    IEmployeeRepository employeeRepository,
    IDepartmentRepository departmentRepository,
    ILogger<ProfileService> logger)
    : IProfileService
{
    // -------------------------
    // GET SINGLE PROFILE
    // -------------------------
    public async Task<Response<ProfileDto>> GetProfileAsync(Guid employeeId)
    {
        // FIX: Include LaptopAssignments when fetching employee
        var employee = await employeeRepository
            .GetByIdWithDepartmentAndRoleAsync(employeeId, includeLaptopAssignments: true);

        if (employee == null)
        {
            return Response<ProfileDto>.Fail(
                ResponseCode.NotFound, // Changed to NotFound for single entity
                new List<string> { "Employee not found" }); // FIX: Wrapped in List<string>
        }

        // Department is already included in GetByIdWithDepartmentAndRoleAsync
        // var department = await departmentRepository.GetByIdAsync(employee.DepartmentId);

        return Response<ProfileDto>.Ok(Map(employee)); // Use the updated Map method
    }

    // -------------------------
    // GET ALL PROFILES (OPTIONAL USE CASE)
    // -------------------------
    public async Task<Response<PaginatedResultDto<ProfileDto>>> GetProfilesAsync(EmployeeFilterDto filter)
    {
        // FIX: Include LaptopAssignments when fetching employees
        var paginatedEmployees = await employeeRepository.GetFilteredAsync(filter, includeLaptopAssignments: true);

        var dtos = paginatedEmployees.Items.Select(Map).ToList(); // FIX: Access Items property

        return Response<PaginatedResultDto<ProfileDto>>.Ok(new PaginatedResultDto<ProfileDto>
        {
            PageNumber = paginatedEmployees.PageNumber, // FIX: Use paginatedEmployees properties
            PageSize = paginatedEmployees.PageSize,     // FIX: Use paginatedEmployees properties
            TotalCount = paginatedEmployees.TotalCount, // FIX: Use paginatedEmployees properties
            Items = dtos
        });
    }

    // -------------------------
    // UPDATE PROFILE
    // -------------------------
    public async Task<Response> UpdateProfileAsync(Guid employeeId, UpdateProfileDto dto)
    {
        var employee = await employeeRepository.GetByIdAsync(employeeId);

        if (employee == null)
        {
            return Response.Fail(ResponseCode.NotFound, new List<string> { "Employee not found" }); // FIX: Wrapped in List<string>
        }

        employee.FullName = dto.FullName;
        employee.PhoneNumber = dto.PhoneNumber;
        employee.UpdatedAt = DateTime.UtcNow;

        await employeeRepository.UpdateAsync(employee);

        return Response.Ok();
    }

    // -------------------------
    // UPLOAD PROFILE PICTURE
    // -------------------------
    public async Task<Response<string>> UploadProfilePictureAsync(Guid employeeId, IFormFile file)
    {
        var employee = await employeeRepository.GetByIdAsync(employeeId);

        if (employee == null)
        {
            return Response<string>.Fail(ResponseCode.NotFound, new List<string> { "Employee not found" }); // FIX: Wrapped in List<string>
        }

        if (file == null || file.Length == 0)
        {
            return Response<string>.Fail(ResponseCode.BadRequest, new List<string> { "No file uploaded." });
        }

        var uploadsFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "ProfilePictures");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = $"/ProfilePictures/{uniqueFileName}";

        employee.ProfilePictureUrl = relativePath;
        employee.UpdatedAt = DateTime.UtcNow;

        await employeeRepository.UpdateAsync(employee);

        return Response<string>.Ok(relativePath);
    }

    // -------------------------
    // REMOVE PROFILE PICTURE
    // -------------------------
    public async Task<Response> RemoveProfilePictureAsync(Guid employeeId)
    {
        var employee = await employeeRepository.GetByIdAsync(employeeId);

        if (employee == null)
        {
            return Response.Fail(ResponseCode.NotFound, new List<string> { "Employee not found" }); // FIX: Wrapped in List<string>
        }

        if (!string.IsNullOrEmpty(employee.ProfilePictureUrl))
        {
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot");

            var filePath = Path.Combine(
                uploadsFolder,
                employee.ProfilePictureUrl.TrimStart('/'));

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            employee.ProfilePictureUrl = null;
            employee.UpdatedAt = DateTime.UtcNow;

            await employeeRepository.UpdateAsync(employee);
        }

        return Response.Ok();
    }
    
    // NEW: Updated Map method to include assigned laptop details
    private static ProfileDto Map(Employee employee)
    {
        var currentAssignment = employee.LaptopAssignments?
            .OrderByDescending(la => la.AssignedDate)
            .FirstOrDefault();

        return new ProfileDto
        {
            Id = employee.Id,
            StaffId = employee.StaffId,
            FullName = employee.FullName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name ?? "Unknown",
            Role = employee.Role?.Name ?? "Unknown",
            ProfilePictureUrl = employee.ProfilePictureUrl,
            IsFirstLogin = employee.IsFirstLogin,

            AssignedLaptopId = currentAssignment?.LaptopId,
            AssignedLaptopSerialNumber = currentAssignment?.Laptop?.SerialNumber,
            AssignedLaptopAssetTag = currentAssignment?.Laptop?.AssetTag,
            AssignedLaptopBrand = currentAssignment?.Laptop?.Brand,
            AssignedLaptopModel = currentAssignment?.Laptop?.Model,
            AssignedLaptopDate = currentAssignment?.AssignedDate
        };
    }
}