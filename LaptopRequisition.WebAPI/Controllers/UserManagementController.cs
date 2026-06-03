using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Added for IFormFile and StatusCodes
using System; // Added for Exception
using LaptopRequisition.Application.DTOs.Admin; // Added for EmployeeFilterDto, AdminEmployeeResponseDto, BulkUploadResultDto, UpdateUserRoleDto, AdminCreateEmployeeDto
using LaptopRequisition.Application.DTOs; // Added for PaginatedResultDto
using System.Collections.Generic; // Added for IEnumerable

namespace LaptopRequisition.WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/users")] // Dedicated route for admin user management
    [Authorize(Roles = "REQUISITION_PORTAL_ADMIN,Super Admin")] // FIX: Updated to match SSO admin roles
    public class UserManagementController : ControllerBase
    {
        private readonly IUserManagementService _userManagementService;

        public UserManagementController(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        [HttpGet("summary")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserManagementSummaryDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // For unauthorized roles
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserManagementSummary()
        {
            try
            {
                var summary = await _userManagementService.GetUserManagementSummaryAsync();
                return Ok(summary);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching user management summary.", details = ex.Message });
            }
        }

        [HttpGet] // GET /api/admin/users
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResultDto<AdminEmployeeResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFilteredAndPaginatedEmployees([FromQuery] EmployeeFilterDto filter)
        {
            try
            {
                var employees = await _userManagementService.GetFilteredAndPaginatedEmployeesAsync(filter);
                return Ok(employees);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching employee data.", details = ex.Message });
            }
        }

        [HttpGet("{employeeId}")] // GET /api/admin/users/{employeeId}
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AdminEmployeeProfileDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployeeProfileForAdmin(Guid employeeId)
        {
            try
            {
                var profile = await _userManagementService.GetEmployeeProfileForAdminAsync(employeeId);
                return Ok(profile);
            }
            catch (InvalidOperationException ex) // For "Employee not found"
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching employee profile.", details = ex.Message });
            }
        }

        [HttpPost] // POST /api/admin/users (Admin Create Employee)
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AdminEmployeeProfileDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AdminCreateEmployee([FromBody] AdminCreateEmployeeDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var newEmployeeProfile = await _userManagementService.AdminCreateEmployeeAsync(dto);
                return CreatedAtAction(nameof(GetEmployeeProfileForAdmin), new { employeeId = newEmployeeProfile.Id }, newEmployeeProfile);
            }
            catch (InvalidOperationException ex) // For "Staff ID already exists", "Email already exists", "Department not found", "Role not found"
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while creating the employee.", details = ex.Message });
            }
        }

        [HttpPut("{employeeId}")] // PUT /api/admin/users/{employeeId}
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AdminEmployeeProfileDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEmployeeDetails(Guid employeeId, [FromBody] UpdateEmployeeDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedProfile = await _userManagementService.UpdateEmployeeDetailsAsync(employeeId, dto);
                return Ok(updatedProfile);
            }
            catch (InvalidOperationException ex) // For "Employee not found", "Email already exists", "Department not found", "Role not found"
            {
                return BadRequest(new { message = ex.Message }); // Use BadRequest for validation-related errors
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while updating employee details.", details = ex.Message });
            }
        }

        [HttpPut("{employeeId}/deactivate")] // PUT /api/admin/users/{employeeId}/deactivate
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeactivateEmployee(Guid employeeId)
        {
            try
            {
                await _userManagementService.DeactivateEmployeeAsync(employeeId);
                return Ok(new { message = "Employee account deactivated successfully." });
            }
            catch (InvalidOperationException ex) // For "Employee not found" or "already deactivated"
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while deactivating employee account.", details = ex.Message });
            }
        }

        [HttpPut("{employeeId}/reactivate")] // PUT /api/admin/users/{employeeId}/reactivate
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReactivateEmployee(Guid employeeId)
        {
            try
            {
                await _userManagementService.ReactivateEmployeeAsync(employeeId);
                return Ok(new { message = "Employee account reactivated successfully." });
            }
            catch (InvalidOperationException ex) // For "Employee not found" or "not deactivated"
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while reactivating employee account.", details = ex.Message });
            }
        }

        [HttpPost("{employeeId}/initiate-password-reset")] // POST /api/admin/users/{employeeId}/initiate-password-reset
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AdminInitiatePasswordReset(Guid employeeId)
        {
            try
            {
                await _userManagementService.AdminInitiatePasswordResetAsync(employeeId);
                return Ok(new { message = "Password reset email initiated for employee." });
            }
            catch (InvalidOperationException ex) // For "Employee not found" or SSO-managed password
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while initiating password reset.", details = ex.Message });
            }
        }

        [HttpPut("{employeeId}/role")] // PUT /api/admin/users/{employeeId}/role
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEmployeeRole(Guid employeeId, [FromBody] UpdateUserRoleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _userManagementService.UpdateEmployeeRoleAsync(employeeId, dto.NewRoleId);
                return Ok(new { message = "Employee role updated successfully." });
            }
            catch (InvalidOperationException ex) // For "Employee not found" or "Role not found"
            {
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new { message = ex.Message });
                }
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while updating employee role.", details = ex.Message });
            }
        }

        [HttpDelete("{employeeId}")] // DELETE /api/admin/users/{employeeId}
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SoftDeleteEmployee(Guid employeeId)
        {
            try
            {
                await _userManagementService.SoftDeleteEmployeeAsync(employeeId);
                return Ok(new { message = "Employee account soft-deleted successfully." });
            }
            catch (InvalidOperationException ex) // For "Employee not found", "already soft-deleted", "has assigned laptop", "has pending requests"
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while soft-deleting employee account.", details = ex.Message });
            }
        }

        [HttpDelete("{employeeId}/purge")] // DELETE /api/admin/users/{employeeId}/purge
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PurgeEmployee(Guid employeeId)
        {
            try
            {
                await _userManagementService.PurgeEmployeeAsync(employeeId);
                return NoContent(); // 204 No Content
            }
            catch (InvalidOperationException ex) // For "Employee not found" or "not soft-deleted"
            {
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new { message = ex.Message });
                }
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while purging employee account.", details = ex.Message });
            }
        }

        [HttpPut("{employeeId}/restore")] // PUT /api/admin/users/{employeeId}/restore
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RestoreEmployee(Guid employeeId)
        {
            try
            {
                await _userManagementService.RestoreEmployeeAsync(employeeId);
                return Ok(new { message = "Employee account restored successfully." });
            }
            catch (InvalidOperationException ex) // For "Employee not found" or "not soft-deleted"
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while restoring employee account.", details = ex.Message });
            }
        }

        [HttpGet("deleted")] // GET /api/admin/users/deleted
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AdminEmployeeResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDeletedEmployees()
        {
            try
            {
                var deletedEmployees = await _userManagementService.GetDeletedEmployeesAsync();
                return Ok(deletedEmployees);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching deleted employee accounts.", details = ex.Message });
            }
        }

        [HttpPost("bulk-upload")] 
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<BulkUploadResultDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> BulkUploadEmployees(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded or file is empty." });
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Only CSV files are allowed." });
            }

            try
            {
                var results = await _userManagementService.BulkRegisterEmployeesAsync(file);
                return Ok(results);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred during bulk upload.", details = ex.Message });
            }
        }
    }
}