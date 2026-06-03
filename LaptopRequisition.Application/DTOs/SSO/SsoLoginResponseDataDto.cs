using System.Text.Json.Serialization;

namespace LaptopRequisition.Application.DTOs.SSO
{
    public class SsoLoginResponseDataDto
    {
        [JsonPropertyName("tokenDetails")]
        public SsoTokenDetailsDto TokenDetails { get; set; } = new SsoTokenDetailsDto();

        [JsonPropertyName("profile")]
        public SsoProfileDto Profile { get; set; } = new SsoProfileDto();
    }
}