using LaptopRequisition.Application.DTOs.Admin.Reports;
using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LaptopRequisition.WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/reports")] // Dedicated route for admin reports
    [Authorize(Roles = "REQUISITION_PORTAL_ADMIN,Super Admin")] // FIX: Updated to match SSO admin roles
    public class AdminReportingController : ControllerBase
    {
        private readonly IAdminReportingService _adminReportingService;

        public AdminReportingController(IAdminReportingService adminReportingService)
        {
            _adminReportingService = adminReportingService;
        }

        [HttpGet("laptop-utilization")] // GET /api/admin/reports/laptop-utilization
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LaptopUtilizationReportDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetLaptopUtilizationReport()
        {
            try
            {
                var report = await _adminReportingService.GetLaptopUtilizationReportAsync();
                return Ok(report);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while generating the laptop utilization report.", details = ex.Message });
            }
        }

        [HttpGet("request-trend")] // GET /api/admin/reports/request-trend
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<RequestTrendReportDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRequestTrendReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate == default || endDate == default || startDate > endDate)
            {
                return BadRequest(new { message = "Invalid date range provided." });
            }

            try
            {
                var report = await _adminReportingService.GetRequestTrendReportAsync(startDate, endDate);
                return Ok(report);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while generating the request trend report.", details = ex.Message });
            }
        }

        [HttpGet("employee-activity")] // GET /api/admin/reports/employee-activity
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<EmployeeActivityReportDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployeeActivityReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate == default || endDate == default || startDate > endDate)
            {
                return BadRequest(new { message = "Invalid date range provided." });
            }

            try
            {
                var report = await _adminReportingService.GetEmployeeActivityReportAsync(startDate, endDate);
                return Ok(report);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while generating the employee activity report.", details = ex.Message });
            }
        }
    }
}