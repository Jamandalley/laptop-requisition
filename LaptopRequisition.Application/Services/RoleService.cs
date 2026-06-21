using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LaptopRequisition.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IEmployeeRepository _employeeRepository; // Added
        private readonly LaptopRequisition.Application.Interfaces.SSO.ISsoRoleClient _ssoRoleClient; // Added

        public RoleService(IRoleRepository roleRepository, IEmployeeRepository employeeRepository, LaptopRequisition.Application.Interfaces.SSO.ISsoRoleClient ssoRoleClient) // Updated constructor
        {
            _roleRepository = roleRepository;
            _employeeRepository = employeeRepository; // Initialized
            _ssoRoleClient = ssoRoleClient;
        }

        public async Task<IEnumerable<RoleResponseDto>> GetAllRolesAsync()
        {
            var roles = await _roleRepository.GetAllAsync();
            return roles.Select(r => new RoleResponseDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            });
        }

        public async Task<RoleResponseDto> CreateRoleAsync(string roleName, string description)
        {
            var existingRole = await _roleRepository.GetByNameAsync(roleName);
            if (existingRole != null)
            {
                throw new InvalidOperationException($"Role with name '{roleName}' already exists locally.");
            }

            // Call SSO API
            var ssoResponse = await _ssoRoleClient.CreateRole(roleName);
            if (!ssoResponse.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to create role in SSO: {ssoResponse.Message}");
            }

            var role = new Role
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _roleRepository.AddAsync(role);

            return new RoleResponseDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt
            };
        }

        public async Task<RoleResponseDto> GetRoleByIdAsync(Guid id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
            {
                throw new InvalidOperationException("Role not found.");
            }
            return new RoleResponseDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt
            };
        }

        public async Task<RoleResponseDto> UpdateRoleAsync(Guid id, UpdateRoleDto dto)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
            {
                throw new InvalidOperationException("Role not found.");
            }

            var existingRoleWithSameName = await _roleRepository.GetByNameAsync(dto.Name);
            if (existingRoleWithSameName != null && existingRoleWithSameName.Id != id)
            {
                throw new InvalidOperationException($"Role with name '{dto.Name}' already exists.");
            }

            role.Name = dto.Name;
            role.Description = dto.Description;
            role.UpdatedAt = DateTime.UtcNow;

            await _roleRepository.UpdateAsync(role);
            return new RoleResponseDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt
            };
        }

        public async Task DeleteRoleAsync(Guid id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
            {
                throw new InvalidOperationException("Role not found.");
            }

            // Check if any employees are assigned to this role before deleting
            var employeesWithRole = await _employeeRepository.GetEmployeesByRoleIdAsync(id); // Assuming this method exists
            if (employeesWithRole != null && employeesWithRole.Any())
            {
                throw new InvalidOperationException($"Cannot delete role '{role.Name}' because it is assigned to {employeesWithRole.Count()} employee(s).");
            }

            // Call SSO API
            var ssoResponse = await _ssoRoleClient.DeleteRole(role.Name);
            if (!ssoResponse.IsSuccess)
            {
                if (!ssoResponse.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Failed to delete role in SSO: {ssoResponse.Message}");
                }
            }

            await _roleRepository.DeleteAsync(id);
        }
    }
}