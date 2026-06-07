using LaptopRequisition.Application.DTOs.OTP;
using System.Threading.Tasks;
using LaptopRequisition.Domain.Enums;
using LaptopRequisition.Domain.Common; // NEW: Added for ResponseModel
using LaptopRequisition.Application.Helpers; // Keep this if ResponseModel is not in Common

namespace LaptopRequisition.Application.Interfaces
{
    public interface IOtpHelperService
    {
        Task<ResponseModel<ResponseCode, OtpResponse>> GenerateOtpAsync(string userRef); // FIX: Changed ResponseCodeEnum to ResponseCode
        Task<ResponseModel<ResponseCode, OtpResponse>> ValidateOtpAsync(string retrievalCode, string otp); // FIX: Changed ResponseCodeEnum to ResponseCode
        Task<ResponseModel<ResponseCode, OtpBase>> CheckOtpValidityAsync(string retrievalCode, string userRef); // FIX: Changed ResponseCodeEnum to ResponseCode
    }
}