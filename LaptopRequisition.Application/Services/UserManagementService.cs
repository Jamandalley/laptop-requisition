using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.Interfaces;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs; // Added for PaginatedResultDto
using System.Linq;
using LaptopRequisition.Domain; // Added for Employee
using System; // Added for Guid
using System.Collections.Generic; // Added for List
using LaptopRequisition.Application.DTOs.Request; // Added to resolve RequestHistoryDto
using LaptopRequisition.Application.DTOs.Laptop; // Added to resolve LaptopResponseDto
using Microsoft.AspNetCore.Http; // Added for IFormFile
using CsvHelper; // Added for CsvHelper
using System.Globalization; // Added for CultureInfo
using CsvHelper.Configuration; // Added for CsvConfiguration
using System.IO; // Added for MemoryStream
using System.ComponentModel.DataAnnotations; // Added for ValidationContext

namespace LaptopRequisition.Application.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILaptopRepository _laptopRepository; // Added to check assigned laptops
        private readonly IDepartmentRepository _departmentRepository; // Added
        private readonly IRoleRepository _roleRepository; // Added
        private readonly IRequestRepository _requestRepository; // Added
        private readonly IAuthService _authService; // Added for password reset

        public UserManagementService(IEmployeeRepository employeeRepository,
                                     ILaptopRepository laptopRepository,
                                     IDepartmentRepository departmentRepository,
                                     IRoleRepository roleRepository,
                                     IRequestRepository requestRepository,
                                     IAuthService authService) // Updated constructor
        {
            _employeeRepository = employeeRepository;
            _laptopRepository = laptopRepository;
            _departmentRepository = departmentRepository;
            _roleRepository = roleRepository;
            _requestRepository = requestRepository;
            _authService = authService;
        }

        public async Task<UserManagementSummaryDto> GetUserManagementSummaryAsync()
        {
            var totalStaff = await _employeeRepository.CountAllAsync();
            var activeUsers = await _employeeRepository.CountActiveUsersAsync();
            var pendingOnboarding = await _employeeRepository.CountPendingOnboardingAsync();
            var usersWithAssignedLaptops = await _employeeRepository.CountUsersWithAssignedLaptopsAsync();
            var usersWithoutLaptops = await _employeeRepository.CountUsersWithoutLaptopsAsync();

            return new UserManagementSummaryDto
            {
                TotalStaff = totalStaff,
                ActiveUsers = activeUsers,
                PendingOnboarding = pendingOnboarding,
                UsersWithAssignedLaptops = usersWithAssignedLaptops,
                UsersWithoutLaptops = usersWithoutLaptops
            };
        }

        public async Task<PaginatedResultDto<AdminEmployeeResponseDto>> GetFilteredAndPaginatedEmployeesAsync(EmployeeFilterDto filter)
        {
            var paginatedEmployees = await _employeeRepository.GetFilteredAndPaginatedEmployeesAsync(filter);

            var mappedItems = new List<AdminEmployeeResponseDto>();
            foreach (var employee in paginatedEmployees.Items)
            {
                mappedItems.Add(new AdminEmployeeResponseDto
                {
                    Id = employee.Id,
                    StaffId = employee.StaffId,
                    FullName = employee.FullName,
                    Email = employee.Email,
                    PhoneNumber = employee.PhoneNumber,
                    DepartmentId = employee.DepartmentId,
                    DepartmentName = employee.Department?.Name ?? "Unknown",
                    RoleId = employee.RoleId,
                    RoleName = employee.Role?.Name ?? "Unknown",
                    IsLocked = employee.IsLocked,
                    IsVerified = employee.IsVerified,
                    IsFirstLogin = employee.IsFirstLogin,
                    HasAssignedLaptop = await _laptopRepository.GetAssignedLaptopByEmployeeIdAsync(employee.Id) != null, // Check if employee has an assigned laptop
                    CreatedAt = employee.CreatedAt,
                    UpdatedAt = employee.UpdatedAt
                });
            }

            return new PaginatedResultDto<AdminEmployeeResponseDto>
            {
                Items = mappedItems,
                TotalCount = paginatedEmployees.TotalCount,
                PageNumber = paginatedEmployees.PageNumber,
                PageSize = paginatedEmployees.PageSize
            };
        }

        public async Task<AdminEmployeeProfileDto> GetEmployeeProfileForAdminAsync(Guid employeeId)
        {
            // Use GetByIdIncludingDeletedAsync to allow viewing profile even if soft-deleted
            var employee = await _employeeRepository.GetByIdIncludingDeletedAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }

            var assignedLaptop = await _laptopRepository.GetAssignedLaptopByEmployeeIdAsync(employeeId);
            var requestHistory = await _requestRepository.GetByEmployeeIdAsync(employeeId); // Get all requests for history

            var mappedHistory = requestHistory.Select(r => new RequestHistoryDto
            {
                Id = r.Id,
                Date = r.CreatedAt,
                RequestType = "Laptop Request", // Assuming all are laptop requests for now
                Status = r.Status,
                LaptopDetails = r.Laptop != null ? $"{r.Laptop.Brand} {r.Laptop.Model} (SN: {r.Laptop.SerialNumber})" : null,
                Purpose = r.Purpose,
                Notes = r.RejectionReason
            }).OrderByDescending(h => h.Date).Take(5); // Take recent 5 for summary

            return new AdminEmployeeProfileDto
            {
                Id = employee.Id,
                StaffId = employee.StaffId,
                FullName = employee.FullName,
                Email = employee.Email,
                PhoneNumber = employee.PhoneNumber,
                DepartmentId = employee.DepartmentId,
                DepartmentName = employee.Department?.Name ?? "Unknown",
                RoleId = employee.RoleId,
                RoleName = employee.Role?.Name ?? "Unknown",
                IsLocked = employee.IsLocked,
                IsVerified = employee.IsVerified,
                IsFirstLogin = employee.IsFirstLogin,
                ProfilePictureUrl = employee.ProfilePictureUrl,
                CreatedAt = employee.CreatedAt,
                UpdatedAt = employee.UpdatedAt,
                AssignedLaptop = assignedLaptop != null ? new LaptopResponseDto // Use LaptopResponseDto
                {
                    Id = assignedLaptop.Id,
                    AssetTag = assignedLaptop.AssetTag,
                    Brand = assignedLaptop.Brand,
                    Model = assignedLaptop.Model,
                    SerialNumber = assignedLaptop.SerialNumber,
                    Processor = assignedLaptop.Processor,
                    RAM = assignedLaptop.RAM,
                    Storage = assignedLaptop.Storage,
                    OperatingSystem = assignedLaptop.OperatingSystem,
                    ScreenSize = assignedLaptop.ScreenSize,
                    Status = assignedLaptop.Status,
                    AssignedToEmployeeId = assignedLaptop.AssignedToEmployeeId,
                    AssignedAt = assignedLaptop.AssignedAt
                } : null,
                RequestHistory = mappedHistory
            };
        }

        public async Task<AdminEmployeeProfileDto> UpdateEmployeeDetailsAsync(Guid employeeId, UpdateEmployeeDto dto)
        {
            // Use GetByIdIncludingDeletedAsync to allow updating even if soft-deleted
            var employee = await _employeeRepository.GetByIdIncludingDeletedAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }

            // Check if email is being changed and if new email already exists
            if (employee.Email != dto.Email)
            {
                var existingEmployeeWithEmail = await _employeeRepository.GetByEmailAsync(dto.Email);
                if (existingEmployeeWithEmail != null && existingEmployeeWithEmail.Id != employeeId)
                {
                    throw new InvalidOperationException($"Employee with email '{dto.Email}' already exists.");
                }
            }

            // Check if department exists
            var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId);
            if (department == null)
            {
                throw new InvalidOperationException($"Department with ID '{dto.DepartmentId}' not found.");
            }

            // Check if role exists
            var role = await _roleRepository.GetByIdAsync(dto.RoleId);
            if (role == null)
            {
                throw new InvalidOperationException($"Role with ID '{dto.RoleId}' not found.");
            }

            employee.FullName = dto.FullName;
            employee.Email = dto.Email;
            employee.PhoneNumber = dto.PhoneNumber;
            employee.DepartmentId = dto.DepartmentId;
            employee.RoleId = dto.RoleId;
            employee.IsLocked = dto.IsLocked;
            employee.IsVerified = dto.IsVerified;
            employee.IsFirstLogin = dto.IsFirstLogin;
            employee.ProfilePictureUrl = dto.ProfilePictureUrl;
            employee.UpdatedAt = DateTime.UtcNow;

            await _employeeRepository.UpdateAsync(employee);

            // Re-fetch to ensure navigation properties are loaded for mapping
            var updatedEmployee = await _employeeRepository.GetByIdIncludingDeletedAsync(employeeId);
            if (updatedEmployee == null)
            {
                throw new InvalidOperationException("Updated employee not found after update operation.");
            }

            var assignedLaptop = await _laptopRepository.GetAssignedLaptopByEmployeeIdAsync(employeeId);
            var requestHistory = await _requestRepository.GetByEmployeeIdAsync(employeeId);

            var mappedHistory = requestHistory.Select(r => new RequestHistoryDto
            {
                Id = r.Id,
                Date = r.CreatedAt,
                RequestType = "Laptop Request",
                Status = r.Status,
                LaptopDetails = r.Laptop != null ? $"{r.Laptop.Brand} {r.Laptop.Model} (SN: {r.Laptop.SerialNumber})" : null,
                Purpose = r.Purpose,
                Notes = r.RejectionReason
            }).OrderByDescending(h => h.Date).Take(5);

            return new AdminEmployeeProfileDto
            {
                Id = updatedEmployee.Id,
                StaffId = updatedEmployee.StaffId,
                FullName = updatedEmployee.FullName,
                Email = updatedEmployee.Email,
                PhoneNumber = updatedEmployee.PhoneNumber,
                DepartmentId = updatedEmployee.DepartmentId,
                DepartmentName = updatedEmployee.Department?.Name ?? "Unknown",
                RoleId = updatedEmployee.RoleId,
                RoleName = updatedEmployee.Role?.Name ?? "Unknown",
                IsLocked = updatedEmployee.IsLocked,
                IsVerified = updatedEmployee.IsVerified,
                IsFirstLogin = updatedEmployee.IsFirstLogin,
                ProfilePictureUrl = updatedEmployee.ProfilePictureUrl,
                CreatedAt = updatedEmployee.CreatedAt,
                UpdatedAt = updatedEmployee.UpdatedAt,
                AssignedLaptop = assignedLaptop != null ? new LaptopResponseDto // Use LaptopResponseDto
                {
                    Id = assignedLaptop.Id,
                    AssetTag = assignedLaptop.AssetTag,
                    Brand = assignedLaptop.Brand,
                    Model = assignedLaptop.Model,
                    SerialNumber = assignedLaptop.SerialNumber,
                    Processor = assignedLaptop.Processor,
                    RAM = assignedLaptop.RAM,
                    Storage = assignedLaptop.Storage,
                    OperatingSystem = assignedLaptop.OperatingSystem,
                    ScreenSize = assignedLaptop.ScreenSize,
                    Status = assignedLaptop.Status,
                    AssignedToEmployeeId = assignedLaptop.AssignedToEmployeeId,
                    AssignedAt = assignedLaptop.AssignedAt
                } : null,
                RequestHistory = mappedHistory
            };
        }

        public async Task DeactivateEmployeeAsync(Guid employeeId)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }

            if (employee.IsLocked)
            {
                throw new InvalidOperationException("Employee account is already deactivated.");
            }

            employee.IsLocked = true;
            employee.UpdatedAt = DateTime.UtcNow;
            await _employeeRepository.UpdateAsync(employee);
        }

        public async Task ReactivateEmployeeAsync(Guid employeeId)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }

            if (!employee.IsLocked)
            {
                throw new InvalidOperationException("Employee account is not deactivated.");
            }

            employee.IsLocked = false;
            employee.UpdatedAt = DateTime.UtcNow;
            await _employeeRepository.UpdateAsync(employee);
        }

        public async Task AdminInitiatePasswordResetAsync(Guid employeeId)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }

            // Call the existing AuthService method to send a password reset email
            await _authService.RequestPasswordResetAsync(employee.Email);
        }

        public async Task SoftDeleteEmployeeAsync(Guid employeeId)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }

            if (employee.IsDeleted)
            {
                throw new InvalidOperationException("Employee account is already soft-deleted.");
            }

            // Check if the employee has any assigned laptops
            var assignedLaptop = await _laptopRepository.GetAssignedLaptopByEmployeeIdAsync(employeeId);
            if (assignedLaptop != null)
            {
                throw new InvalidOperationException("Cannot soft-delete an employee with an assigned laptop. Please unassign the laptop first.");
            }

            // Check if the employee has any pending requests
            var pendingRequest = await _requestRepository.GetPendingOrApprovedRequestByEmployeeIdAsync(employeeId);
            if (pendingRequest != null)
            {
                throw new InvalidOperationException("Cannot soft-delete an employee with pending or approved requests.");
            }

            await _employeeRepository.SoftDeleteAsync(employeeId);
        }

        public async Task RestoreEmployeeAsync(Guid employeeId)
        {
            // Use GetByIdIncludingDeletedAsync to find the employee even if soft-deleted
            var employee = await _employeeRepository.GetByIdIncludingDeletedAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found (or not soft-deleted).");
            }

            if (!employee.IsDeleted)
            {
                throw new InvalidOperationException("Employee account is not soft-deleted.");
            }

            await _employeeRepository.RestoreAsync(employeeId);
        }

        public async Task<IEnumerable<AdminEmployeeResponseDto>> GetDeletedEmployeesAsync()
        {
            var deletedEmployees = await _employeeRepository.GetDeletedEmployeesAsync();

            var mappedItems = new List<AdminEmployeeResponseDto>();
            foreach (var employee in deletedEmployees)
            {
                mappedItems.Add(new AdminEmployeeResponseDto
                {
                    Id = employee.Id,
                    StaffId = employee.StaffId,
                    FullName = employee.FullName,
                    Email = employee.Email,
                    PhoneNumber = employee.PhoneNumber,
                    DepartmentId = employee.DepartmentId,
                    DepartmentName = employee.Department?.Name ?? "Unknown",
                    RoleId = employee.RoleId,
                    RoleName = employee.Role?.Name ?? "Unknown",
                    IsLocked = employee.IsLocked,
                    IsVerified = employee.IsVerified,
                    IsFirstLogin = employee.IsFirstLogin,
                    HasAssignedLaptop = await _laptopRepository.GetAssignedLaptopByEmployeeIdAsync(employee.Id) != null, // Check if employee has an assigned laptop
                    CreatedAt = employee.CreatedAt,
                    UpdatedAt = employee.UpdatedAt
                });
            }
            return mappedItems;
        }

        public async Task<List<BulkUploadResultDto>> BulkRegisterEmployeesAsync(IFormFile csvFile)
        {
            var results = new List<BulkUploadResultDto>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
            };

            using (var reader = new StreamReader(csvFile.OpenReadStream()))
            using (var csv = new CsvReader(reader, config))
            {
                // Map CSV headers to DTO properties
                csv.Context.RegisterClassMap<BulkRegisterEmployeeDtoMap>();

                var records = csv.GetRecords<BulkRegisterEmployeeDto>().ToList();

                foreach (var record in records)
                {
                    var result = new BulkUploadResultDto
                    {
                        StaffId = record.StaffId,
                        Email = record.Email,
                        IsSuccess = false
                    };

                    try
                    {
                        // Validate DTO properties
                        var validationContext = new ValidationContext(record, serviceProvider: null, items: null);
                        var validationResults = new List<ValidationResult>();
                        if (!Validator.TryValidateObject(record, validationContext, validationResults, validateAllProperties: true))
                        {
                            result.ErrorMessage = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
                            results.Add(result);
                            continue;
                        }

                        // Check if Department exists
                        var department = await _departmentRepository.GetByNameAsync(record.DepartmentName);
                        if (department == null)
                        {
                            result.ErrorMessage = $"Department '{record.DepartmentName}' not found.";
                            results.Add(result);
                            continue;
                        }

                        // Check if Role exists and is either "Admin" or "Employee"
                        var role = await _roleRepository.GetByNameAsync(record.RoleName);
                        if (role == null || (role.Name != "Admin" && role.Name != "Employee"))
                        {
                            result.ErrorMessage = $"Invalid Role '{record.RoleName}'. Only 'Admin' or 'Employee' roles are allowed for bulk upload.";
                            results.Add(result);
                            continue;
                        }

                        // Create RegisterEmployeeDto for AuthService
                        var registerDto = new RegisterEmployeeDto
                        {
                            StaffId = record.StaffId,
                            FullName = record.FullName,
                            Email = record.Email,
                            PhoneNumber = record.PhoneNumber,
                            DepartmentId = department.Id,
                            // RoleId is no longer part of RegisterEmployeeDto, AuthService assigns default "Employee"
                            Password = record.Password,
                            ValidationReference = Guid.NewGuid().ToString() // Dummy reference for bulk creation
                        };

                        // Register employee using AuthService
                        var newEmployee = await _authService.RegisterEmployeeAsync(registerDto);
                        
                        // If the bulk upload specified "Admin" role, update it after initial creation
                        if (role.Name == "Admin")
                        {
                            newEmployee.RoleId = role.Id;
                            await _employeeRepository.UpdateAsync(newEmployee);
                        }

                        result.IsSuccess = true;
                    }
                    catch (InvalidOperationException ex)
                    {
                        result.ErrorMessage = ex.Message;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
                    }
                    results.Add(result);
                }
            }
            return results;
        }

        public async Task UpdateEmployeeRoleAsync(Guid employeeId, Guid newRoleId)
        {
            // Use GetByIdIncludingDeletedAsync to allow updating role even if soft-deleted
            var employee = await _employeeRepository.GetByIdIncludingDeletedAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }

            // Check if the new role exists and is either "Admin" or "Employee"
            var newRole = await _roleRepository.GetByIdAsync(newRoleId);
            if (newRole == null || (newRole.Name != "Admin" && newRole.Name != "Employee"))
            {
                throw new InvalidOperationException($"Invalid Role. Only 'Admin' or 'Employee' roles can be assigned.");
            }
            
            employee.RoleId = newRoleId;
            employee.UpdatedAt = DateTime.UtcNow;
            await _employeeRepository.UpdateAsync(employee);
        }

        public async Task<AdminEmployeeProfileDto> AdminCreateEmployeeAsync(AdminCreateEmployeeDto dto)
        {
            // 1. Validate input DTO (already handled by [Required] attributes and model binding)

            // 2. Check for existing StaffId and Email
            var existingEmployeeByStaffId = await _employeeRepository.GetByStaffIdAsync(dto.StaffId);
            if (existingEmployeeByStaffId != null)
            {
                throw new InvalidOperationException($"Employee with Staff ID '{dto.StaffId}' already exists.");
            }

            var existingEmployeeByEmail = await _employeeRepository.GetByEmailAsync(dto.Email);
            if (existingEmployeeByEmail != null)
            {
                throw new InvalidOperationException($"Employee with email '{dto.Email}' already exists.");
            }

            // 3. Check if Department and Role exist and are valid
            var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId);
            if (department == null)
            {
                throw new InvalidOperationException($"Department with ID '{dto.DepartmentId}' not found.");
            }

            var role = await _roleRepository.GetByIdAsync(dto.RoleId);
            if (role == null || (role.Name != "Admin" && role.Name != "Employee"))
            {
                throw new InvalidOperationException($"Invalid Role. Only 'Admin' or 'Employee' roles can be assigned during admin creation.");
            }

            // 4. Create RegisterEmployeeDto for AuthService
            var registerDto = new RegisterEmployeeDto
            {
                StaffId = dto.StaffId,
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                DepartmentId = dto.DepartmentId,
                // RoleId is no longer part of RegisterEmployeeDto, AuthService assigns default "Employee"
                Password = dto.Password,
                ValidationReference = Guid.NewGuid().ToString() // Dummy reference for admin creation, as OTP is not involved
            };

            // 5. Register employee using AuthService
            var newEmployee = await _authService.RegisterEmployeeAsync(registerDto);

            // If the admin creation specified "Admin" role, update it after initial creation
            if (role.Name == "Admin")
            {
                newEmployee.RoleId = role.Id;
            }

            // Update IsVerified and IsLocked based on admin's input
            newEmployee.IsVerified = dto.IsVerified;
            newEmployee.IsLocked = dto.IsLocked;
            newEmployee.IsFirstLogin = false; // Admin created, so not first login
            newEmployee.UpdatedAt = DateTime.UtcNow;

            await _employeeRepository.UpdateAsync(newEmployee); // Save these admin-specific settings

            // 6. Map the created employee to AdminEmployeeProfileDto and return
            // Re-fetch to ensure navigation properties are loaded for mapping
            var createdEmployeeWithDetails = await _employeeRepository.GetByIdWithDepartmentAndRoleAsync(newEmployee.Id);
            if (createdEmployeeWithDetails == null)
            {
                throw new InvalidOperationException("Created employee not found after registration.");
            }

            return new AdminEmployeeProfileDto
            {
                Id = createdEmployeeWithDetails.Id,
                StaffId = createdEmployeeWithDetails.StaffId,
                FullName = createdEmployeeWithDetails.FullName,
                Email = createdEmployeeWithDetails.Email,
                PhoneNumber = createdEmployeeWithDetails.PhoneNumber,
                DepartmentId = createdEmployeeWithDetails.DepartmentId,
                DepartmentName = createdEmployeeWithDetails.Department?.Name ?? "Unknown",
                RoleId = createdEmployeeWithDetails.RoleId,
                RoleName = createdEmployeeWithDetails.Role?.Name ?? "Unknown",
                IsLocked = createdEmployeeWithDetails.IsLocked,
                IsVerified = createdEmployeeWithDetails.IsVerified,
                IsFirstLogin = createdEmployeeWithDetails.IsFirstLogin,
                ProfilePictureUrl = createdEmployeeWithDetails.ProfilePictureUrl,
                CreatedAt = createdEmployeeWithDetails.CreatedAt,
                UpdatedAt = createdEmployeeWithDetails.UpdatedAt,
                AssignedLaptop = null, // Newly created employee won't have an assigned laptop
                RequestHistory = new List<RequestHistoryDto>() // Newly created employee won't have request history
            };
        }

        public async Task PurgeEmployeeAsync(Guid employeeId)
        {
            // Use GetByIdIncludingDeletedAsync to find the employee even if soft-deleted
            var employee = await _employeeRepository.GetByIdIncludingDeletedAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }

            if (!employee.IsDeleted)
            {
                throw new InvalidOperationException("Employee account is not soft-deleted and cannot be purged. Please soft-delete it first.");
            }
            
            await _employeeRepository.HardDeleteAsync(employeeId);
        }
    }

    // CsvHelper ClassMap for BulkRegisterEmployeeDto
    public sealed class BulkRegisterEmployeeDtoMap : ClassMap<BulkRegisterEmployeeDto>
    {
        public BulkRegisterEmployeeDtoMap()
        {
            Map(m => m.StaffId).Name("StaffId");
            Map(m => m.FullName).Name("FullName");
            Map(m => m.Email).Name("Email");
            Map(m => m.PhoneNumber).Name("PhoneNumber");
            Map(m => m.DepartmentName).Name("DepartmentName");
            Map(m => m.RoleName).Name("RoleName");
            Map(m => m.Password).Name("Password");
        }
    }
}