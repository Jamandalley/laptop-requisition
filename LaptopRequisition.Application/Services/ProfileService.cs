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
        var employee = await employeeRepository
            .GetByIdWithDepartmentAndRoleAsync(employeeId);

        if (employee == null)
        {
            return Response<ProfileDto>.Fail(
                ResponseCode.BadRequest,
                ["Employee not found"]);
        }

        var department = await departmentRepository
            .GetByIdAsync(employee.DepartmentId);

        // var currentLaptop = employee.Laptops?
        //     .OrderByDescending(x => x.AssignedDate)
        //     .FirstOrDefault();

        var dto = new ProfileDto
        {
            Id = employee.Id,
            StaffId = employee.StaffId,
            FullName = employee.FullName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            DepartmentId = employee.DepartmentId,
            DepartmentName = department?.Name ?? "Unknown",
            Role = employee.Role?.Name ?? "Unknown",
            ProfilePictureUrl = employee.ProfilePictureUrl,
            IsFirstLogin = employee.IsFirstLogin,

            // optional enrichment (if added to DTO)
            // AssignedLaptopId = currentLaptop?.LaptopId,
            // AssignedLaptopSerialNumber = currentLaptop?.Laptop?.SerialNumber
        };

        return Response<ProfileDto>.Ok(dto);
    }

    // -------------------------
    // GET ALL PROFILES (OPTIONAL USE CASE)
    // -------------------------
    public async Task<Response<PaginatedResultDto<ProfileDto>>> GetProfilesAsync(EmployeeFilterDto filter)
    {
        var result = await employeeRepository.GetFilteredAsync(filter);

        var dtos = result.Items.Select(employee => new ProfileDto
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

            // AssignedLaptopId = currentLaptop?.LaptopId,
            // AssignedLaptopSerialNumber = currentLaptop?.Laptop?.SerialNumber
        }).ToList();

        return Response<PaginatedResultDto<ProfileDto>>.Ok(new PaginatedResultDto<ProfileDto>
        {
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            Data = dtos
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
            return Response.Fail(ResponseCode.BadRequest, ["Employee not found"]);
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
            return Response<string>.Fail(ResponseCode.BadRequest, ["Employee not found"]);
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
            return Response.Fail(ResponseCode.BadRequest, ["Employee not found"]);
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
    
    private static ProfileDto Map(Employee employee)
    {
        // var currentLaptop = employee.Laptops?
        //     .OrderByDescending(x => x.AssignedDate)
        //     .FirstOrDefault();

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

            // AssignedLaptopId = currentLaptop?.LaptopId,
            // AssignedLaptopSerialNumber = currentLaptop?.Laptop?.SerialNumber
        };
    }
}