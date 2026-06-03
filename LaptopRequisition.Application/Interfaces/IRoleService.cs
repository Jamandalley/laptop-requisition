using LaptopRequisition.Application.DTOs.Admin;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LaptopRequisition.Application.Interfaces
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleResponseDto>> GetAllRolesAsync();
        Task<RoleResponseDto> GetRoleByIdAsync(Guid id);
        Task<RoleResponseDto> UpdateRoleAsync(Guid id, UpdateRoleDto dto);
        Task DeleteRoleAsync(Guid id);
    }
}