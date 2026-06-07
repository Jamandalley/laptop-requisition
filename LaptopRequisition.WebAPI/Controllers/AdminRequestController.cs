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
using LaptopRequisition.Application.DTOs.Page;
using LaptopRequisition.Domain.Common; // NEW: Added for Response<T>
using LaptopRequisition.Domain.Enums; // NEW: Added for ResponseCode

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

        // Helper to map Response<T> to IActionResult
        private IActionResult MapResponseToIActionResult<T>(Response<T> response)
        {
            return response.Code switch
            {
                ResponseCode.NotFound => NotFound(new { message = response.Message }),
                ResponseCode.BadRequest => BadRequest(new { message = response.Message }),
                ResponseCode.Unauthorized => Unauthorized(new { message = response.Message }),
                ResponseCode.Forbidden => Forbid(),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = response.Message })
            };
        }

        // Helper to map Response to IActionResult (for non-generic Response)
        private IActionResult MapResponseToIActionResult(Response response)
        {
            return response.Code switch
            {
                ResponseCode.NotFound => NotFound(new { message = response.Message }),
                ResponseCode.BadRequest => BadRequest(new { message = response.Message }),
                ResponseCode.Unauthorized => Unauthorized(new { message = response.Message }),
                ResponseCode.Forbidden => Forbid(),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = response.Message })
            };
        }

        // --- Laptop Requests (Admin) ---

        [HttpGet] // GET /api/admin/requests
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResultDto<RequestResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFilteredAndPaginatedRequests([FromQuery] AdminRequestFilterDto filter)
        {
            var response = await _requestService.GetFilteredAndPaginatedRequestsForAdminAsync(filter);
            return response.IsSuccessful
                ? Ok(response.Data)
                : MapResponseToIActionResult(response);
        }

        [HttpGet("{id}")] // GET /api/admin/requests/{id}
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RequestResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRequestById(Guid id)
        {
            var response = await _requestService.GetRequestByIdAsync(id);
            return response.IsSuccessful
                ? Ok(response.Data)
                : MapResponseToIActionResult(response);
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
            var response = await _requestService.ApproveRequestAsync(id);
            return response.IsSuccessful
                ? Ok(new { message = "Laptop request approved successfully." })
                : MapResponseToIActionResult(response);
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
            var response = await _requestService.RejectRequestAsync(id, dto.Reason);
            return response.IsSuccessful
                ? Ok(new { message = "Laptop request rejected successfully." })
                : MapResponseToIActionResult(response);
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
            var response = await _requestService.AssignLaptopAsync(requestId, laptopId);
            return response.IsSuccessful
                ? Ok(new { message = $"Laptop {laptopId} assigned to request {requestId} successfully." })
                : MapResponseToIActionResult(response);
        }

        // --- Return Requests (Admin) ---
        [HttpGet("return-requests/all")] // GET /api/admin/requests/return-requests/all
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResultDto<ReturnRequestResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllReturnRequestsForAdmin([FromQuery] AdminReturnRequestFilterDto filter)
        {
            var response = await _returnRequestService.GetAllReturnRequestsAsync(filter);
            return response.IsSuccessful
                ? Ok(response.Data)
                : MapResponseToIActionResult(response);
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
            dto.ReturnRequestId = id;
            var response = await _returnRequestService.ApproveReturnRequestAsync(dto);
            return response.IsSuccessful
                ? Ok(new { message = "Return request approved successfully by admin." })
                : MapResponseToIActionResult(response);
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
            var response = await _returnRequestService.RejectReturnRequestAsync(id, dto.Reason);
            return response.IsSuccessful
                ? Ok(new { message = "Return request rejected successfully by admin." })
                : MapResponseToIActionResult(response);
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
            var response = await _returnRequestService.DeleteReturnRequestAsync(id);
            return response.IsSuccessful
                ? NoContent()
                : MapResponseToIActionResult(response);
        }

        // --- Export Endpoints ---
        [HttpGet("export")] // GET /api/admin/requests/export
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExportFilteredRequestsForAdmin([FromQuery] AdminRequestFilterDto filter)
        {
            var response = await _requestService.ExportFilteredRequestsForAdminAsync(filter);
            if (response.IsSuccessful && response.Data != null)
            {
                var fileName = $"AdminLaptopRequests_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
                return File(response.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            return MapResponseToIActionResult(response);
        }

        [HttpGet("return-requests/export")] // GET /api/admin/requests/return-requests/export
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExportFilteredReturnRequestsForAdmin([FromQuery] AdminReturnRequestFilterDto filter)
        {
            var response = await _returnRequestService.ExportFilteredReturnRequestsForAdminAsync(filter);
            if (response.IsSuccessful && response.Data != null)
            {
                var fileName = $"AdminReturnRequests_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
                return File(response.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            return MapResponseToIActionResult(response);
        }
    }
}