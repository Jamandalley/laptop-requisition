using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc; // Keep this for [FromBody] and [AsParameters]
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaptopRequisition.Application.Extensions; // For GetEmployeeId()
using Microsoft.AspNetCore.Builder; // Added for WithOpenApi
using LaptopRequisition.Domain.Common; // Added for Response<T>
using LaptopRequisition.Domain.Enums; // Added for ResponseCode

namespace LaptopRequisition.WebAPI.Endpoints
{
    public static class DepartmentEndpoints
    {
        public static WebApplication MapDepartmentEndpoints(this WebApplication app)
        {
            // Admin-only endpoints
            app.MapPost("/api/admin/departments", async (
                [FromServices] IDepartmentService departmentService,
                [FromBody] CreateDepartmentDto dto) =>
            {
                var response = await departmentService.CreateDepartmentAsync(dto);
                return response.IsSuccessful
                    ? Results.CreatedAtRoute("GetDepartmentByIdRoute", new { id = response.Data?.Id }, response.Data)
                    : MapResponseToIResult(response);
            })
            .RequireAuthorization(policy => policy.RequireRole("REQUISITION_PORTAL_ADMIN", "Super Admin"))
            .WithTags("Departments")
            .WithOpenApi();

            app.MapPut("/api/admin/departments/{id}", async (
                Guid id,
                [FromServices] IDepartmentService departmentService,
                [FromBody] UpdateDepartmentDto dto) =>
            {
                var response = await departmentService.UpdateDepartmentAsync(id, dto);
                return response.IsSuccessful
                    ? Results.Ok(response.Data)
                    : MapResponseToIResult(response);
            })
            .RequireAuthorization(policy => policy.RequireRole("REQUISITION_PORTAL_ADMIN", "Super Admin"))
            .WithTags("Departments")
            .WithOpenApi();

            app.MapDelete("/api/admin/departments/{id}", async (
                Guid id,
                [FromServices] IDepartmentService departmentService) =>
            {
                var response = await departmentService.DeleteDepartmentAsync(id);
                return response.IsSuccessful
                    ? Results.NoContent()
                    : MapResponseToIResult(response);
            })
            .RequireAuthorization(policy => policy.RequireRole("REQUISITION_PORTAL_ADMIN", "Super Admin"))
            .WithTags("Departments")
            .WithOpenApi();

            // Authenticated-only endpoints (open to all authenticated users)
            app.MapGet("/api/departments", async (
                [FromServices] IDepartmentService departmentService) =>
            {
                var response = await departmentService.GetAllDepartmentsAsync();
                return response.IsSuccessful
                    ? Results.Ok(response.Data)
                    : MapResponseToIResult(response);
            })
            .RequireAuthorization() // Any authenticated user
            .WithTags("Departments")
            .WithOpenApi();

            app.MapGet("/api/departments/{id}", async (
                Guid id,
                [FromServices] IDepartmentService departmentService) =>
            {
                var response = await departmentService.GetDepartmentByIdAsync(id);
                return response.IsSuccessful
                    ? Results.Ok(response.Data)
                    : MapResponseToIResult(response);
            })
            .WithName("GetDepartmentByIdRoute") // Named for CreatedAtRoute
            .RequireAuthorization() // Any authenticated user
            .WithTags("Departments")
            .WithOpenApi();

            return app;
        }

        // Helper to map Response<T> to IResult
        private static IResult MapResponseToIResult<T>(Response<T> response)
        {
            return response.Code switch // FIX: Changed from response.ResponseCode to response.Code
            {
                LaptopRequisition.Domain.Enums.ResponseCode.NotFound => Results.NotFound(new { message = response.Message }), // FIX: Fully qualified
                LaptopRequisition.Domain.Enums.ResponseCode.BadRequest => Results.BadRequest(new { message = response.Message }), // FIX: Fully qualified
                LaptopRequisition.Domain.Enums.ResponseCode.Unauthorized => Results.Unauthorized(), // FIX: Fully qualified
                LaptopRequisition.Domain.Enums.ResponseCode.Forbidden => Results.Forbid(), // FIX: Fully qualified
                _ => Results.Problem(response.Message)
            };
        }

        // Helper to map Response to IResult (for non-generic Response)
        private static IResult MapResponseToIResult(Response response)
        {
            return response.Code switch // FIX: Changed from response.ResponseCode to response.Code
            {
                LaptopRequisition.Domain.Enums.ResponseCode.NotFound => Results.NotFound(new { message = response.Message }), // FIX: Fully qualified
                LaptopRequisition.Domain.Enums.ResponseCode.BadRequest => Results.BadRequest(new { message = response.Message }), // FIX: Fully qualified
                LaptopRequisition.Domain.Enums.ResponseCode.Unauthorized => Results.Unauthorized(), // FIX: Fully qualified
                LaptopRequisition.Domain.Enums.ResponseCode.Forbidden => Results.Forbid(), // FIX: Fully qualified
                _ => Results.Problem(response.Message)
            };
        }
    }
}