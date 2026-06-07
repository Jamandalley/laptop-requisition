using LaptopRequisition.Application.DTOs.Notification;
using LaptopRequisition.Application.DTOs.OTP;
using LaptopRequisition.Application.Helpers;
using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Application.Interfaces.External;
using LaptopRequisition.Domain.Enums;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using LaptopRequisition.Application.Configurations;
using System.IO;
using System;
using LaptopRequisition.Domain.Common; // Ensure this is present for ResponseModel

namespace LaptopRequisition.Application.Services
{
    public class OtpHelperService : IOtpHelperService
    {
        private readonly IOtpApi _otpService;
        private readonly INotificationApi _notificationApi; 
        private readonly NotificationApiSettings _notificationApiSettings;

        public OtpHelperService(
            IOtpApi otpService,
            INotificationApi notificationApi,
            IOptions<NotificationApiSettings> notificationApiSettingsOptions) 
        {
            _otpService = otpService;
            _notificationApi = notificationApi; 
            _notificationApiSettings = notificationApiSettingsOptions.Value;
        }

        public async Task<ResponseModel<ResponseCode, OtpResponse>> GenerateOtpAsync(string userRef)
        {
            var otpResult = await _otpService.GenerateOtpAsync(new GenerateOtpRequest
            {
                UserReference = userRef,
                Time = 5, 
                OtpDigit = "SixDigits" 
            });

            if (!otpResult.IsSuccessStatusCode || otpResult.Content is null || !otpResult.Content.IsSuccessful)
            {
                var errorContent = otpResult.Error?.Content;
                var errorMessage = "Error occurred while generating OTP";
                if (!string.IsNullOrEmpty(errorContent))
                {
                    try
                    {
                        var otpBaseError = JsonConvert.DeserializeObject<OtpBase>(errorContent);
                        errorMessage = otpBaseError?.Message ?? errorMessage;
                    }
                    catch (JsonException) { /* Log or handle deserialization error */ }
                }
                return ResponseModel<ResponseCode, OtpResponse>.Failure(ResponseCode.UnknownError, errorMessage);
            }
            
            var otpValue = otpResult.Content.Data!.Otp!;
            var purpose = "Secure Authentication"; 

            var sendResult = await SendOtpEmailAsync(userRef, otpValue, purpose);

            if (!sendResult.IsSuccessful)
            {
                return ResponseModel<ResponseCode, OtpResponse>.Failure(sendResult.Code, sendResult.Message);
            }
           
            return ResponseModel<ResponseCode, OtpResponse>.Success(otpResult.Content, ResponseCode.Success);
        }

        public async Task<ResponseModel<ResponseCode, OtpResponse>> ValidateOtpAsync(string retrievalCode, string otp)
        {
            var validationResult = await _otpService.ValidateOtpAsync(new ValidateOtpRequest
            {
                RetrievalCode = retrievalCode,
                Otp = otp
            });

            if (!validationResult.IsSuccessStatusCode || validationResult.Content is null || !validationResult.Content.IsSuccessful)
            {
                var errorContent = validationResult.Error?.Content;
                var errorMessage = "OTP validation failed.";
                if (!string.IsNullOrEmpty(errorContent))
                {
                    try
                    {
                        var otpBaseError = JsonConvert.DeserializeObject<OtpBase>(errorContent);
                        errorMessage = otpBaseError?.Message ?? errorMessage;
                    }
                    catch (JsonException) { /* Log or handle deserialization error */ }
                }
                return ResponseModel<ResponseCode, OtpResponse>.Failure(ResponseCode.OtpValidationFailed, errorMessage);
            }

            return ResponseModel<ResponseCode, OtpResponse>.Success(validationResult.Content, ResponseCode.Success);
        }

        public async Task<ResponseModel<ResponseCode, OtpBase>> CheckOtpValidityAsync(string retrievalCode, string userRef)
        {
            var validityResult = await _otpService.CheckOtpValidityAsync(retrievalCode, userRef);

            if (!validityResult.IsSuccessStatusCode || validityResult.Content is null || !validityResult.Content.IsSuccessful)
            {
                var errorContent = validityResult.Error?.Content;
                var errorMessage = "OTP validity check failed.";
                if (!string.IsNullOrEmpty(errorContent))
                {
                    try
                    {
                        var otpBaseError = JsonConvert.DeserializeObject<OtpBase>(errorContent);
                        errorMessage = otpBaseError?.Message ?? errorMessage;
                    }
                    catch (JsonException) { /* Log or handle deserialization error */ }
                }
                return ResponseModel<ResponseCode, OtpBase>.Failure(ResponseCode.OtpValidationFailed, errorMessage);
            }

            return ResponseModel<ResponseCode, OtpBase>.Success(validityResult.Content, ResponseCode.Success);
        }
        

        private async Task<ResponseModel<ResponseCode, OtpResponse>> SendOtpEmailAsync(
            string email,
            string otp,
            string purpose)
        {
            Console.WriteLine($"Sending OTP notification to {email} via Email"); 

            var notificationRequest = new NotificationRequest
            {
                Channels = new List<string> { "Email" },
                From = _notificationApiSettings.FromEmail,
                To = email,
                Subject = "Secure Authentication",
                Message = await BuildOtpEmailBodyAsync(otp, purpose)
            };

            Console.WriteLine($"[Notification Request] Sending to: {notificationRequest.To}, From: {notificationRequest.From}, Subject: {notificationRequest.Subject}, Channels: {string.Join(", ", notificationRequest.Channels)}");
            Console.WriteLine($"[Notification Request] Message (first 100 chars): {notificationRequest.Message?.Substring(0, Math.Min(notificationRequest.Message.Length, 100))}");


            var otpResp = await _notificationApi.SendNotificationAsync(notificationRequest);

            Console.WriteLine($"[Notification API Response] IsSuccessStatusCode: {otpResp.IsSuccessStatusCode}, Content: {otpResp.Content}, Error: {otpResp.Error?.Content}");


            if (!otpResp.IsSuccessStatusCode || otpResp.Content is null || !otpResp.Content.IsSuccessful)
            {
                Console.Error.WriteLine($"[Otp-Email-Failed] Failed to send OTP email to {email}. Error: {otpResp.Error?.Content}");
                var error = JsonConvert.DeserializeObject<OtpBase>(
                    otpResp.Error?.Content ?? string.Empty);

                return ResponseModel<ResponseCode, OtpResponse>.Failure(ResponseCode.UnknownError,
                    error?.Message ?? "Failed to send OTP email.");
            }

            Console.WriteLine($"[Otp-Email-Sent] OTP email successfully sent to {email}");
            return ResponseModel<ResponseCode, OtpResponse>.Success(null!, ResponseCode.Success);
        }

        private async Task<string> BuildOtpEmailBodyAsync(string otp, string purpose)
        {
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "otp.html");
            
            if (!File.Exists(templatePath))
            {
                return $"Dear User,\n\nYour One-Time Password (OTP) for {purpose} is: {otp}\n\nThis code is valid for 5 minutes. Please do not share it with anyone.\n\nBest regards,\nThe LRS Team";
            }

            var body = await File.ReadAllTextAsync(templatePath);
            
            return body
                .Replace("{{otp}}", otp)
                .Replace("{{purpose}}", purpose)
                .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString());
        }
    }
}