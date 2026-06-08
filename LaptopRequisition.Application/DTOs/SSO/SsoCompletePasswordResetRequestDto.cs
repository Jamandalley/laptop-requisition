namespace LaptopRequisition.Application.DTOs.SSO
{
    public class SsoCompletePasswordResetRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordResetToken { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}