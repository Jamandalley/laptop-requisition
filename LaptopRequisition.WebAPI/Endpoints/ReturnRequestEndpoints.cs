using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc; // Keep this for [FromBody] and [AsParameters]
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.Extensions; // For GetEmployeeId()
using Microsoft.AspNetCore.Builder; // Added for WithOpenApi
using LaptopRequisition.Domain.Common; // Added for Response<T>
using LaptopRequisition.Domain.Enums; // Added for ResponseCode
using LaptopRequisition.Application.DTOs.Request; // Added for HistoryFilterDto

namespace LaptopRequisition.WebAPI.Endpoints
{
    public static class ReturnRequestEndpoints
    {
        public static WebApplication MapReturnRequestEndpoints(this WebApplication app)
        {
            var returnRequestsGroup = app.MapGroup("/api/returnrequests")
                                         .RequireAuthorization() // Apply authorization to all endpoints in this group
                                         .WithTags("Return Requests");

            // POST /api/returnrequests
            returnRequestsGroup.MapPost("/", async (
                HttpContext context,
                [FromServices] IReturnRequestService returnRequestService,
                [FromBody] CreateReturnRequestDto dto) =>
            {
                var employeeId = context.User.GetEmployeeId(); // Get non-nullable employeeId
                var response = await returnRequestService.CreateReturnRequestAsync(dto);
                
                return response.IsSuccessful
                    ? Results.CreatedAtRoute("GetReturnRequestByIdRoute", new { id = response.Data?.Id }, response.Data)
                    : MapResponseToIResult(response);
            }).WithOpenApi();

            // GET /api/returnrequests/{id}
            returnRequestsGroup.MapGet("/{id}", async (
                Guid id,
                HttpContext context,
                [FromServices] IReturnRequestService returnRequestService) =>
            {
                var employeeId = context.User.GetEmployeeId(); // Get non-nullable employeeId
                var response = await returnRequestService.GetReturnRequestByIdAsync(id);
                
                // Optional: Add check to ensure employee can only view their own requests unless admin
                // This logic should ideally be in the service layer, but for direct porting:
                if (response.IsSuccessful && response.Data != null && response.Data.EmployeeId != employeeId)
                {
                    return Results.Forbid(); 
                }
                return response.IsSuccessful
                    ? Results.Ok(response.Data)
                    : MapResponseToIResult(response);
            }).WithName("GetReturnRequestByIdRoute").WithOpenApi(); // Added Name for CreatedAtRoute

            // GET /api/returnrequests/my-requests
            returnRequestsGroup.MapGet("/my-requests", async (
                HttpContext context,
                [FromServices] IReturnRequestService returnRequestService,
                [AsParameters] HistoryFilterDto filter) => // FIX: Added filter
            {
                var employeeId = context.User.GetEmployeeId(); // Get non-nullable employeeId
                var response = await returnRequestService.GetEmployeeReturnRequestsAsync(employeeId, filter); // FIX: Pass filter
                return response.IsSuccessful
                    ? Results.Ok(response.Data)
                    : MapResponseToIResult(response);
            }).WithOpenApi();

            // NEW: Admin endpoints for Return Requests
            var adminReturnRequestsGroup = app.MapGroup("/api/admin/returnrequests")
                                             .RequireAuthorization(policy => policy.RequireRole("REQUISITION_PORTAL_ADMIN", "Super Admin"))
                                             .WithTags("Admin Return Requests");

            // GET /api/admin/returnrequests
            adminReturnRequestsGroup.MapGet("/", async (
                [FromServices] IReturnRequestService returnRequestService,
                [AsParameters] AdminReturnRequestFilterDto filter) =>
            {
                var response = await returnRequestService.GetAllReturnRequestsAsync(filter);
                return response.IsSuccessful
                    ? Results.Ok(response.Data)
                    : MapResponseToIResult(response);
            }).WithOpenApi();

            // POST /api/admin/returnrequests/{returnRequestId}/approve
            adminReturnRequestsGroup.MapPost("/{returnRequestId}/approve", async (
                Guid returnRequestId,
                [FromServices] IReturnRequestService returnRequestService,
                [FromBody] ApproveReturnRequestDto dto) =>
            {
                dto.ReturnRequestId = returnRequestId; // Ensure ID from route matches DTO
                var response = await returnRequestService.ApproveReturnRequestAsync(dto);
                return response.IsSuccessful
                    ? Results.NoContent()
                    : MapResponseToIResult(response);
            }).WithOpenApi();

            // POST /api/admin/returnrequests/{returnRequestId}/reject
            adminReturnRequestsGroup.MapPost("/{returnRequestId}/reject", async (
                Guid returnRequestId,
                [FromServices] IReturnRequestService returnRequestService,
                [FromBody] RejectReturnRequestDto dto) => // Assuming RejectReturnRequestDto exists with a Reason property
            {
                var response = await returnRequestService.RejectReturnRequestAsync(returnRequestId, dto.Reason);
                return response.IsSuccessful
                    ? Results.NoContent()
                    : MapResponseToIResult(response);
            }).WithOpenApi();

            // DELETE /api/admin/returnrequests/{returnRequestId}
            adminReturnRequestsGroup.MapDelete("/{returnRequestId}", async (
                Guid returnRequestId,
                [FromServices] IReturnRequestService returnRequestService) =>
            {
                var response = await returnRequestService.DeleteReturnRequestAsync(returnRequestId);
                return response.IsSuccessful
                    ? Results.NoContent()
                    : MapResponseToIResult(response);
            }).WithOpenApi();

            // GET /api/admin/returnrequests/export
            adminReturnRequestsGroup.MapGet("/export", async (
                [FromServices] IReturnRequestService returnRequestService,
                [AsParameters] AdminReturnRequestFilterDto filter) =>
            {
                var response = await returnRequestService.ExportFilteredReturnRequestsForAdminAsync(filter);
                if (response.IsSuccessful && response.Data != null)
                {
                    var fileName = $"AdminReturnRequests_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
                    return Results.File(response.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                return MapResponseToIResult(response);
            }).WithOpenApi();


            return app;
        }

        // Helper to map Response<T> to IResult
        private static IResult MapResponseToIResult<T>(Response<T> response)
        {
            return response.Code switch
            {
                LaptopRequisition.Domain.Enums.ResponseCode.NotFound => Results.NotFound(new { message = response.Message }),
                LaptopRequisition.Domain.Enums.ResponseCode.BadRequest => Results.BadRequest(new { message = response.Message }),
                LaptopRequisition.Domain.Enums.ResponseCode.Unauthorized => Results.Unauthorized(),
                LaptopRequisition.Domain.Enums.ResponseCode.Forbidden => Results.Forbid(),
                _ => Results.Problem(response.Message)
            };
        }

        // Helper to map Response to IResult (for non-generic Response)
        private static IResult MapResponseToIResult(Response response)
        {
            return response.Code switch
            {
                LaptopRequisition.Domain.Enums.ResponseCode.NotFound => Results.NotFound(new { message = response.Message }),
                LaptopRequisition.Domain.Enums.ResponseCode.BadRequest => Results.BadRequest(new { message = response.Message }),
                LaptopRequisition.Domain.Enums.ResponseCode.Unauthorized => Results.Unauthorized(),
                LaptopRequisition.Domain.Enums.ResponseCode.Forbidden => Results.Forbid(),
                _ => Results.Problem(response.Message)
            };
        }
    }
}