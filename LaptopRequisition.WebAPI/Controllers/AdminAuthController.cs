using LaptopRequisition.Application.DTOs.Login;
using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs;
using Microsoft.AspNetCore.Http; // Added for StatusCodes

namespace LaptopRequisition.WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/auth")] // Dedicated route for admin authentication
    public class AdminAuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AdminAuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AdminLogin([FromBody] LoginDto loginDto)
        {
            try
            {
                var response = await _authService.AdminLoginAsync(loginDto.Email, loginDto.Password);
                return Ok(response);
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred during admin login.", details = ex.Message });
            }
        }
    }
}