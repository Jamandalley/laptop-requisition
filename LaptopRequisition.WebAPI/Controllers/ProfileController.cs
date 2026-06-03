using LaptopRequisition.Application.DTOs.Employee;
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
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProfileController(IProfileService profileService, IHttpContextAccessor httpContextAccessor)
        {
            _profileService = profileService;
            _httpContextAccessor = httpContextAccessor;
        }

        private Guid GetCurrentEmployeeId()
        {
            var claims = _httpContextAccessor.HttpContext?.User.Claims;

            foreach (var claim in claims)
            {
                Console.WriteLine($"CLAIM: {claim.Type} = {claim.Value}");
            }

            var employeeId = _httpContextAccessor.HttpContext?.User
                .FindFirst("SourceId")?.Value;

            Console.WriteLine($"SourceId found: {employeeId}");

            if (string.IsNullOrEmpty(employeeId))
            {
                throw new UnauthorizedAccessException(
                    "User not authenticated or employee ID not found in token.");
            }

            return Guid.Parse(employeeId);
        }
        
        [HttpGet("claims")]
        public IActionResult Claims()
        {
            return Ok(User.Claims.Select(c => new
            {
                c.Type,
                c.Value
            }));
        }
        
        [HttpGet("debug-user")]
        public IActionResult DebugUser()
        {
            return Ok(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated,
                Claims = User.Claims.Select(x => new
                {
                    x.Type,
                    x.Value
                }),
                SourceId = User.FindFirst("SourceId")?.Value
            });
        }
        

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProfileDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                var profile = await _profileService.GetProfileAsync(employeeId);
                return Ok(profile);
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching profile.", details = ex.Message });
            }
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                await _profileService.UpdateProfileAsync(employeeId, dto);
                return NoContent();
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
                return StatusCode(500, new
                {
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpPost("picture")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))] // Returns URL of the uploaded picture
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadProfilePicture([FromForm] ProfilePictureUploadDto dto)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                var imageUrl = await _profileService.UploadProfilePictureAsync(employeeId, dto.File);
                return Ok(new { imageUrl });
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while uploading profile picture.", details = ex.Message });
            }
        }

        [HttpDelete("picture")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveProfilePicture()
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                await _profileService.RemoveProfilePictureAsync(employeeId);
                return NoContent();
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while removing profile picture.", details = ex.Message });
            }
        }
    }
}