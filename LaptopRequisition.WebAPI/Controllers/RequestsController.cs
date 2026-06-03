using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System; // Added for Guid
using System.Collections.Generic; // Added for IEnumerable
using System.Threading.Tasks; // Added for Task
using Microsoft.AspNetCore.Http; // Added for StatusCodes
using LaptopRequisition.Application.DTOs.Request; // Added for RequestStatusDetailDto

namespace LaptopRequisition.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RequestsController : ControllerBase
    {
        private readonly IRequestService _requestService;
        private readonly IHttpContextAccessor _httpContextAccessor; // Added

        public RequestsController(IRequestService requestService, IHttpContextAccessor httpContextAccessor) // Updated constructor
        {
            _requestService = requestService;
            _httpContextAccessor = httpContextAccessor; // Initialized
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
        
        [HttpPost]
        // [Authorize] // Already authorized by controller attribute
        public async Task<IActionResult> CreateRequest(CreateRequestDto dto)
        {
            try
            {
                var result = await _requestService.CreateRequestAsync(dto);

                return Ok(new
                {
                    message = "Request submitted successfully",
                    data = result
                });
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
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while creating the request.", details = ex.Message });
            }
        }

        
        [HttpGet("my-requests")]
        // [Authorize] // Already authorized by controller attribute
        public async Task<IActionResult> GetMyRequests()
        {
            try
            {
                var employeeId = GetCurrentEmployeeId(); // Using helper method

                var requests = await _requestService.GetEmployeeRequestsAsync(employeeId);

                return Ok(requests);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching my requests.", details = ex.Message });
            }
        }

       
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var request = await _requestService.GetRequestByIdAsync(id);

                return Ok(request);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching the request.", details = ex.Message });
            }
        }
        
        // Removed: [HttpPut("{id}/approve")] - This is an admin action
        // Removed: [HttpPut("{id}/reject")] - This is an admin action
        // Removed: [HttpPut("{id}/assign")] - This is an admin action
        // Removed: [HttpGet] GetAllRequests() - This is an admin action

        // New methods for Request Management
        [HttpGet("status")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RequestStatusDetailDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRequestStatusDetail()
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                var detail = await _requestService.GetEmployeeRequestStatusDetailAsync(employeeId);
                return Ok(detail);
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching request status.", details = ex.Message });
            }
        }

        [HttpPut("{id}/dismiss")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DismissRejectedRequest(Guid id)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                await _requestService.DismissRejectedRequestAsync(id, employeeId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message }); // Use BadRequest for business rule violations
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while dismissing the request.", details = ex.Message });
            }
        }

        [HttpPut("{id}/confirm-receipt")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfirmReceipt(Guid id)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                await _requestService.ConfirmReceiptAsync(id, employeeId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message }); // Use BadRequest for business rule violations
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while confirming receipt.", details = ex.Message });
            }
        }

        // New methods for History
        [HttpGet("history")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResultDto<RequestHistoryDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployeeHistory([FromQuery] HistoryFilterDto filter)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                var history = await _requestService.GetEmployeeHistoryAsync(employeeId, filter);
                return Ok(history);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching employee history.", details = ex.Message });
            }
        }

        [HttpGet("history/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RequestHistoryDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetHistoryItemById(Guid id)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                var historyItem = await _requestService.GetHistoryItemByIdAsync(id, employeeId);
                return Ok(historyItem);
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching history item details.", details = ex.Message });
            }
        }

        [HttpGet("history/export")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileContentResult))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExportEmployeeHistory([FromQuery] HistoryFilterDto filter)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                var fileContents = await _requestService.ExportEmployeeHistoryAsync(employeeId, filter);
                var fileName = $"LaptopRequisitionHistory_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
                return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while exporting history.", details = ex.Message });
            }
        }

        [HttpPost("report-issue")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReportIssue([FromBody] ReportIssueDto dto)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                await _requestService.ReportIssueAsync(employeeId, dto);
                return Ok(new { message = "Issue reported successfully to IT." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while reporting the issue.", details = ex.Message });
            }
        }
    }
}