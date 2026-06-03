using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LaptopRequisition.WebAPI.Controllers
{
    [Authorize] // Secure the entire controller
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DashboardController(IDashboardService dashboardService, IHttpContextAccessor httpContextAccessor)
        {
            _dashboardService = dashboardService;
            _httpContextAccessor = httpContextAccessor;
        }

        private Guid GetCurrentEmployeeId()
        {
            var employeeId = _httpContextAccessor.HttpContext?.User
                .FindFirst("SourceId")?.Value;

            if (string.IsNullOrEmpty(employeeId))
            {
                throw new UnauthorizedAccessException(
                    "User not authenticated or employee ID not found in token.");
            }

            return Guid.Parse(employeeId);
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardSummary()
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                var summary = await _dashboardService.GetEmployeeDashboardSummaryAsync(employeeId);
                return Ok(summary);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching dashboard summary.", details = ex.Message });
            }
        }
    }
}