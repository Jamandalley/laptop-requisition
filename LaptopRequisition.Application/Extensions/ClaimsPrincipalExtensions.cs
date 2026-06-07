using System;
using System.Security.Claims;

namespace LaptopRequisition.Application.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetEmployeeId(this ClaimsPrincipal user)
        {
            var employeeIdClaim = user.FindFirst("SourceId");

            if (employeeIdClaim == null || string.IsNullOrEmpty(employeeIdClaim.Value))
            {
                throw new UnauthorizedAccessException("Employee ID not found in token claims.");
            }

            if (!Guid.TryParse(employeeIdClaim.Value, out Guid employeeId))
            {
                throw new UnauthorizedAccessException("Invalid employee ID format in token claims.");
            }

            return employeeId;
        }
    }
}