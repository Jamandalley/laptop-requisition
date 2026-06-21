using System;
using System.Collections.Generic;

namespace LaptopRequisition.Application.DTOs.SSO
{
    public class SsoBaseResponseDto
    {
        public bool IsSuccess { get; set; }
        public bool Data { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SsoRoleDataDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public string? ConcurrencyStamp { get; set; }
        public int UserCount { get; set; }
        public bool Status { get; set; }
    }

    public class SsoRoleResponseDto
    {
        public bool IsSuccess { get; set; }
        public SsoRoleDataDto? Data { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SsoRoleListResponseDto
    {
        public bool IsSuccess { get; set; }
        public List<SsoRoleDataDto>? Data { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
