using LaptopRequisition.Application.DTOs.Login;
using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using LaptopRequisition.Application.DTOs;
using Microsoft.AspNetCore.Authorization; // NEW: Added for [AllowAnonymous]

namespace LaptopRequisition.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // This will be the unified AuthController
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [AllowAnonymous] // NEW: Allow unauthenticated access for registration
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterEmployeeDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var employee = await _authService.RegisterEmployeeAsync(registerDto);
                return StatusCode(StatusCodes.Status202Accepted, new
                {
                    Message = "Employee registered successfully. Please verify your account with OTP.",
                    EmployeeId = employee.Id,
                    ValidationReference = registerDto.ValidationReference
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred during registration.", Details = ex.Message });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous] // NEW: Allow unauthenticated access for login
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // This now calls the unified LoginAsync in AuthService
                var loginResponse = await _authService.LoginAsync(loginDto.Email, loginDto.Password);
                
                // If loginResponse indicates failure (e.g., due to local lockout/verification)
                if (!loginResponse.IsSuccess)
                {
                    if (loginResponse.IsLocked)
                    {
                        return Unauthorized(new { Message = loginResponse.Message, IsLocked = true, LockoutEndDate = loginResponse.LockoutEndDate });
                    }
                    // For other failures like "Account not verified" or "Invalid credentials"
                    return Unauthorized(new { Message = loginResponse.Message });
                }

                return Ok(loginResponse); // Return the full LoginResponseDto on success
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex) // Catch InvalidOperationException for specific business logic errors
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred during login.", Details = ex.Message });
            }
        }

        [HttpPost("request-password-reset")]
        [AllowAnonymous] // NEW: Allow unauthenticated access
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _authService.RequestPasswordResetAsync(requestDto.Email);
                return Ok(new
                    { Message = "If an account with that email exists, a password reset link has been sent." });
            }
            catch (InvalidOperationException ex) // Catch InvalidOperationException for specific business logic errors
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred during password reset request.", Details = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous] // NEW: Allow unauthenticated access
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _authService.ResetPasswordAsync(resetDto.Token, resetDto.NewPassword);
                return Ok(new { Message = "Password has been reset successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred during password reset.", Details = ex.Message });
            }
        }
    }
}