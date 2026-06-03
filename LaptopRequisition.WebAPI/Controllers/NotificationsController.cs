using LaptopRequisition.Application.DTOs.Notification;
using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Added for StatusCodes

namespace LaptopRequisition.WebAPI.Controllers
{
    [Authorize] 
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NotificationsController(INotificationService notificationService, IHttpContextAccessor httpContextAccessor)
        {
            _notificationService = notificationService;
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<NotificationResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Added
        public async Task<ActionResult<IEnumerable<NotificationResponseDto>>> GetNotifications([FromQuery] NotificationFilterDto filter) // Changed parameter
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                var notifications = await _notificationService.GetNotificationsByEmployeeIdAsync(employeeId, filter.UnreadOnly);
                return Ok(notifications);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching notifications.", details = ex.Message });
            }
        }
        
        [HttpGet("latest/{count}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<NotificationResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Added
        public async Task<ActionResult<IEnumerable<NotificationResponseDto>>> GetLatestNotifications(int count)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                // Renamed in INotificationService to GetRecentNotificationsByEmployeeIdAsync
                var notifications = await _notificationService.GetRecentNotificationsByEmployeeIdAsync(employeeId, count); 
                return Ok(notifications);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching latest notifications.", details = ex.Message });
            }
        }
        
        [HttpPut("{notificationId}/read")] // Kept existing route and parameter name
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Added
        public async Task<IActionResult> MarkNotificationAsRead(Guid notificationId)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                var notification = await _notificationService.GetNotificationByIdAsync(notificationId);
                if (notification == null || notification.EmployeeId != employeeId)
                {
                    return NotFound("Notification not found or does not belong to the current user.");
                }

                await _notificationService.MarkNotificationAsReadAsync(notificationId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) // Kept existing InvalidOperationException handling
            {
                return NotFound(new { message = ex.Message }); // Changed to NotFound for consistency with existing
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while marking notification as read.", details = ex.Message });
            }
        }

       
        [HttpPut("read-all")] // Kept existing route
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Added
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                await _notificationService.MarkAllNotificationsAsReadAsync(employeeId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while marking all notifications as read.", details = ex.Message });
            }
        }
    }
}