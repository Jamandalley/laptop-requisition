using LaptopRequisition.Application.DTOs.Employee;
using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LaptopRequisition.Application.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IDepartmentRepository _departmentRepository; // To get department name
        
        public ProfileService(IEmployeeRepository employeeRepository, IDepartmentRepository departmentRepository)
        {
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
           }

        public async Task<ProfileDto> GetProfileAsync(Guid employeeId)
        {
            Console.WriteLine($"EmployeeId: {employeeId}");

            var employee = await _employeeRepository
                .GetByIdWithDepartmentAndRoleAsync(employeeId);

            Console.WriteLine($"Employee found: {employee != null}");

            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }

            Console.WriteLine($"Department null: {employee.Department == null}");
            Console.WriteLine($"Role null: {employee.Role == null}");

            var department = await _departmentRepository
                .GetByIdAsync(employee.DepartmentId);

            Console.WriteLine($"Department repository result null: {department == null}");

            return new ProfileDto
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
                IsFirstLogin = employee.IsFirstLogin
            };
        }

        public async Task UpdateProfileAsync(Guid employeeId, UpdateProfileDto dto)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }

            employee.FullName = dto.FullName;
            employee.PhoneNumber = dto.PhoneNumber;
            employee.UpdatedAt = DateTime.UtcNow;

            await _employeeRepository.UpdateAsync(employee);
        }

        public async Task<string> UploadProfilePictureAsync(Guid employeeId, IFormFile file)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }
            
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ProfilePictures");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            
            var relativePath = $"/ProfilePictures/{uniqueFileName}";
            employee.ProfilePictureUrl = relativePath;
            employee.UpdatedAt = DateTime.UtcNow;
            await _employeeRepository.UpdateAsync(employee);

            return relativePath;
            // --- End Temporary local file storage implementation ---
        }

        public async Task RemoveProfilePictureAsync(Guid employeeId)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }

            if (!string.IsNullOrEmpty(employee.ProfilePictureUrl))
            {
                // --- Temporary local file deletion implementation ---
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = Path.Combine(uploadsFolder, employee.ProfilePictureUrl.TrimStart('/'));

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                // --- End Temporary local file deletion implementation ---

                employee.ProfilePictureUrl = null;
                employee.UpdatedAt = DateTime.UtcNow;
                await _employeeRepository.UpdateAsync(employee);
            }
        }
    }
}