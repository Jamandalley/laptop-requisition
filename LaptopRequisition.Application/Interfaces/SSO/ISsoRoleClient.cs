using Refit;
using LaptopRequisition.Application.DTOs.SSO;
using System.Threading.Tasks;

namespace LaptopRequisition.Application.Interfaces.SSO
{
    [Headers("accept: application/json")]
    public interface ISsoRoleClient
    {
        [Post("/api/roles/{roleName}/create")]
        Task<SsoBaseResponseDto> CreateRole([AliasAs("roleName")] string roleName);

        [Delete("/api/roles/{roleName}/delete")]
        Task<SsoBaseResponseDto> DeleteRole([AliasAs("roleName")] string roleName);

        [Get("/api/roles/get")]
        Task<SsoRoleListResponseDto> GetRoles();

        [Get("/api/roles/{roleId}")]
        Task<SsoRoleResponseDto> GetRoleById([AliasAs("roleId")] string roleId);

        [Post("/api/roles/{roleName}/assign{userId}/user")]
        Task<SsoBaseResponseDto> AssignUserRole([AliasAs("roleName")] string roleName, [AliasAs("userId")] string userId);

        [Delete("/api/roles/{roleName}/remove{userId}/user")]
        Task<SsoBaseResponseDto> RemoveUserRole([AliasAs("roleName")] string roleName, [AliasAs("userId")] string userId);
    }
}
