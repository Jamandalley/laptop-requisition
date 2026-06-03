using System.ComponentModel.DataAnnotations;

namespace LaptopRequisition.Application.DTOs.SSO
{
    public class SsoLoginRequestDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}