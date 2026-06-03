using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Request; // Keep this for RequestResponseDto etc.
using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LaptopRequisition.WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/requests")] // Dedicated route for admin request management
    [Authorize(Roles = "REQUISITION_PORTAL_ADMIN,Super Admin")] // FIX: Updated to match SSO admin roles
    public class AdminRequestController : ControllerBase
    {
        private readonly IRequestService _requestService;
        private readonly IReturnRequestService _returnRequestService;

        public AdminRequestController(IRequestService requestService, IReturnRequestService returnRequestService)
        {
            _requestService = requestService;
            _returnRequestService = returnRequestService;
        }

        // --- Laptop Requests (Admin) ---

        [HttpGet] // GET /api/admin/requests
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResultDto<RequestResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFilteredAndPaginatedRequests([FromQuery] AdminRequestFilterDto filter)
        {
            try
            {
                var requests = await _requestService.GetFilteredAndPaginatedRequestsForAdminAsync(filter);
                return Ok(requests);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching laptop requests.", details = ex.Message });
            }
        }

        [HttpGet("{id}")] // GET /api/admin/requests/{id}
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RequestResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRequestById(Guid id)
        {
            try
            {
                var request = await _requestService.GetRequestByIdAsync(id);
                return Ok(request);
            }
            catch (InvalidOperationException ex) // For "Request not found"
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching the laptop request.", details = ex.Message });
            }
        }

        [HttpPut("{id}/approve")] // PUT /api/admin/requests/{id}/approve
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApproveRequest(Guid id)
        {
            try
            {
                await _requestService.ApproveRequestAsync(id);
                return Ok(new { message = "Laptop request approved successfully." });
            }
            catch (InvalidOperationException ex) // For "Request not found" or "Only pending requests can be approved"
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while approving the laptop request.", details = ex.Message });
            }
        }

        [HttpPut("{id}/reject")] // PUT /api/admin/requests/{id}/reject
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RejectRequest(Guid id, [FromBody] RejectRequestDto dto) // Corrected to RejectRequestDto
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                await _requestService.RejectRequestAsync(id, dto.Reason);
                return Ok(new { message = "Laptop request rejected successfully." });
            }
            catch (InvalidOperationException ex) // For "Request not found" or "Only pending requests can be rejected"
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while rejecting the laptop request.", details = ex.Message });
            }
        }

        [HttpPut("{requestId}/assign/{laptopId}")] // PUT /api/admin/requests/{requestId}/assign/{laptopId}
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Corrected typo
        public async Task<IActionResult> AssignLaptopToRequest(Guid requestId, Guid laptopId)
        {
            try
            {
                await _requestService.AssignLaptopAsync(requestId, laptopId);
                return Ok(new { message = $"Laptop {laptopId} assigned to request {requestId} successfully." });
            }
            catch (InvalidOperationException ex) // For "Request not found", "Laptop not found", "Only approved requests can be assigned"
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while assigning laptop to request.", details = ex.Message });
            }
        }

        // --- Return Requests (Admin) ---
        [HttpGet("return-requests/all")] // GET /api/admin/requests/return-requests/all
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResultDto<ReturnRequestResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllReturnRequestsForAdmin([FromQuery] AdminReturnRequestFilterDto filter)
        {
            try
            {
                var returnRequests = await _returnRequestService.GetFilteredAndPaginatedReturnRequestsForAdminAsync(filter);
                return Ok(returnRequests);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching all return requests for admin.", details = ex.Message });
            }
        }

        [HttpPut("return-requests/{id}/approve")] // PUT /api/admin/requests/return-requests/{id}/approve
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AdminApproveReturnRequest(Guid id, [FromBody] ApproveReturnRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                dto.ReturnRequestId = id;
                await _returnRequestService.ApproveReturnRequestAsync(dto);
                return Ok(new { message = "Return request approved successfully by admin." });
            }
            catch (InvalidOperationException ex)
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while approving the return request.", details = ex.Message });
            }
        }

        [HttpPut("return-requests/{id}/reject")] // PUT /api/admin/requests/return-requests/{id}/reject
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AdminRejectReturnRequest(Guid id, [FromBody] RejectRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                await _returnRequestService.RejectReturnRequestAsync(id, dto.Reason);
                return Ok(new { message = "Return request rejected successfully by admin." });
            }
            catch (InvalidOperationException ex)
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while rejecting the return request.", details = ex.Message });
            }
        }

        [HttpDelete("return-requests/{id}")] // DELETE /api/admin/requests/return-requests/{id}
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AdminDeleteReturnRequest(Guid id)
        {
            try
            {
                await _returnRequestService.DeleteReturnRequestAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while deleting the return request.", details = ex.Message });
            }
        }

        // --- Export Endpoints ---
        [HttpGet("export")] // GET /api/admin/requests/export
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExportFilteredRequestsForAdmin([FromQuery] AdminRequestFilterDto filter)
        {
            try
            {
                var fileContents = await _requestService.ExportFilteredRequestsForAdminAsync(filter);
                return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AdminLaptopRequests.xlsx");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while exporting laptop requests.", details = ex.Message });
            }
        }

        [HttpGet("return-requests/export")] // GET /api/admin/requests/return-requests/export
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExportFilteredReturnRequestsForAdmin([FromQuery] AdminReturnRequestFilterDto filter)
        {
            try
            {
                var fileContents = await _returnRequestService.ExportFilteredReturnRequestsForAdminAsync(filter);
                return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AdminReturnRequests.xlsx");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while exporting return requests.", details = ex.Message });
            }
        }
    }
}