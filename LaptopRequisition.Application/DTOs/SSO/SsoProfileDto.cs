using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LaptopRequisition.Application.DTOs.SSO
{
    public class SsoProfileDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("otherName")]
        public string? OtherName { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("defaultRole")]
        public string? DefaultRole { get; set; }

        [JsonPropertyName("userType")]
        public string UserType { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("departmentId")]
        public string? DepartmentId { get; set; } // Changed from Guid? to string?

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; } = new List<string>();

        [JsonPropertyName("apps")]
        public List<SsoAppDto> Apps { get; set; } = new List<SsoAppDto>();
    }

    public class SsoAppDto
    {
        [JsonPropertyName("appId")]
        public string AppId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("logo")]
        public string Logo { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }
    }
}