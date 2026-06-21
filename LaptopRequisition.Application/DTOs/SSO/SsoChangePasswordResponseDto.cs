namespace LaptopRequisition.Application.DTOs.SSO
{
    public class SsoChangePasswordResponseDto
    {
        public bool IsSuccess { get; set; }
        public bool Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
