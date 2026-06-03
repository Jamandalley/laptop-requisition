using System.Text.Json.Serialization;

namespace LaptopRequisition.Application.DTOs.SSO
{
    public class SsoLoginResponseRootDto
    {
        [JsonPropertyName("data")]
        public SsoLoginResponseDataDto? Data { get; set; }

        [JsonPropertyName("isSuccessful")]
        public bool IsSuccessful { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
    }
}