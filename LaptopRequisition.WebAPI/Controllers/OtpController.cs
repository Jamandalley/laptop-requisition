using LaptopRequisition.Application.DTOs.OTP;
using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; 
using System; 

namespace LaptopRequisition.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OtpController : ControllerBase
    {
        private readonly IOtpHelperService _otpHelperService;
        private readonly IAuthService _authService; // Added

        public OtpController(IOtpHelperService otpHelperService, IAuthService authService) // Updated constructor
        {
            _otpHelperService = otpHelperService;
            _authService = authService; // Initialized
        }

        [HttpPost("initiate")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OtpResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> InitiateOtp([FromBody] InitiateOtpRequestDto request)
        {
            try
            {
                var result = await _otpHelperService.GenerateOtpAsync(request.UserReference);
                if (!result.IsSuccessful)
                {
                    return BadRequest(new { message = result.Message ?? "Failed to initiate OTP." });
                }
                // The OtpResponse contains OtpData which has RetrievalCode
                return Ok(new { validationReference = result.Data?.Data?.RetrievalCode });
            }
            catch (Exception ex)
            {
                // Log the exception details here
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while initiating OTP.", details = ex.Message });
            }
        }

        [HttpPost("verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)] // For employee not found
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
        {
            try
            {
                // Call AuthService to validate OTP and verify the account
                await _authService.VerifyAccountAsync(request.ValidationReference, request.Otp);
                return Ok(new { message = "OTP verified and account activated successfully." });
            }
            catch (InvalidOperationException ex)
            {
                
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred during OTP verification.", details = ex.Message });
            }
        }
        
        // [HttpGet("check-validity/{retrievalCode}/{userRef}")]
        // [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OtpBase))]
        // [ProducesResponseType(StatusCodes.Status400BadRequest)]
        // [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        // public async Task<IActionResult> CheckOtpValidity(string retrievalCode, string userRef)
        // {
        //     try
        //     {
        //         var result = await _otpHelperService.CheckOtpValidityAsync(retrievalCode, userRef);
        //         if (!result.IsSuccessful)
        //         {
        //             return BadRequest(new { message = result.Message ?? "OTP validity check failed." });
        //         }
        //         return Ok(result.Data); 
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred during OTP validity check.", details = ex.Message });
        //     }
        // }
    }
}