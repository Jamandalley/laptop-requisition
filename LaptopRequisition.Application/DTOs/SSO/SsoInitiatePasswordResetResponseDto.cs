namespace LaptopRequisition.Application.DTOs.SSO
{
    public class SsoPasswordResetDataDto
    {
        public string PasswordResetToken { get; set; } = string.Empty;
    }

    public class SsoInitiatePasswordResetResponseDto
    {
        public bool IsSuccess { get; set; }
        public SsoPasswordResetDataDto? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}