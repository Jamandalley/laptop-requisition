using System.Text.Json.Serialization; // For [JsonPropertyName]

namespace LaptopRequisition.Application.DTOs.SSO
{
    public class SsoTokenResponseDto
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonPropertyName("scope")] // Added Scope property
        public string? Scope { get; set; } // Made nullable as it might not always be present
    }
}