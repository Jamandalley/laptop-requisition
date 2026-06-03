using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs.Admin; // Added for AdminReturnRequestFilterDto, ApproveReturnRequestDto

namespace LaptopRequisition.WebAPI.Controllers
{
    [Authorize] 
    [Route("api/[controller]")]
    [ApiController]
    public class ReturnRequestsController : ControllerBase
    {
        private readonly IReturnRequestService _returnRequestService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReturnRequestsController(IReturnRequestService returnRequestService, IHttpContextAccessor httpContextAccessor)
        {
            _returnRequestService = returnRequestService;
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

        /// <summary>
        /// Employee submits a new return request for a laptop.
        /// </summary>
        /// <param name="dto">Details of the return request.</param>
        /// <returns>The created return request.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ReturnRequestResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)] // If laptop not found or not assigned
        public async Task<ActionResult<ReturnRequestResponseDto>> CreateReturnRequest(CreateReturnRequestDto dto)
        {
            try
            {
                var returnRequest = await _returnRequestService.CreateReturnRequestAsync(dto);
                return CreatedAtAction(nameof(GetReturnRequestById), new { id = returnRequest.Id }, returnRequest);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Get a specific return request by its ID.
        /// </summary>
        /// <param name="id">The ID of the return request.</param>
        /// <returns>The return request details.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ReturnRequestResponseDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReturnRequestResponseDto>> GetReturnRequestById(Guid id)
        {
            try
            {
                var returnRequest = await _returnRequestService.GetReturnRequestByIdAsync(id);
                // Optional: Add check to ensure employee can only view their own requests unless admin
                if (returnRequest.EmployeeId != GetCurrentEmployeeId())
                {
                    return Forbid(); // Or NotFound for security by obscurity
                }
                return Ok(returnRequest);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Get all return requests for the current authenticated employee.
        /// </summary>
        /// <returns>A list of return request DTOs.</returns>
        [HttpGet("my-requests")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ReturnRequestResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<ReturnRequestResponseDto>>> GetMyReturnRequests()
        {
            var employeeId = GetCurrentEmployeeId();
            var returnRequests = await _returnRequestService.GetEmployeeReturnRequestsAsync(employeeId);
            return Ok(returnRequests);
        }
    }
}