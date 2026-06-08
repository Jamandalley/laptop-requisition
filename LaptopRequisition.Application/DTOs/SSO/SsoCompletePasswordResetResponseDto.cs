namespace LaptopRequisition.Application.DTOs.SSO
{
    public class SsoCompletePasswordResetResponseDto
    {
        public bool IsSuccess { get; set; }
        public bool Data { get; set; } // Data is a boolean for this response
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}