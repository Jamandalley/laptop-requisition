using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Added for StatusCodes
using System; // Added for Exception

namespace LaptopRequisition.WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "REQUISITION_PORTAL_ADMIN,Super Admin")] // FIX: Updated to match SSO admin roles
    public class AdminController : ControllerBase
    {
        private readonly IAdminDashboardService _adminDashboardService;

        public AdminController(IAdminDashboardService adminDashboardService)
        {
            _adminDashboardService = adminDashboardService;
        }

        [HttpGet("dashboard-summary")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LaptopRequisition.Application.DTOs.Admin.AdminDashboardSummaryDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // For unauthorized roles
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAdminDashboardSummary()
        {
            try
            {
                var summary = await _adminDashboardService.GetDashboardSummaryAsync();
                return Ok(summary);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching admin dashboard summary.", details = ex.Message });
            }
        }
    }
}