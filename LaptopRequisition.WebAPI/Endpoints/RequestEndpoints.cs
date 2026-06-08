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
using LaptopRequisition.Application.DTOs.Page;
using LaptopRequisition.Application.DTOs.Request;
using LaptopRequisition.Application.Extensions; // For GetEmployeeId()
using Microsoft.AspNetCore.Builder; // Added for WithOpenApi
using LaptopRequisition.Domain.Common; // Added for Response<T>
using LaptopRequisition.Domain.Enums; // Added for ResponseCode

namespace LaptopRequisition.WebAPI.Endpoints
{
    public static class RequestEndpoints
    {
        public static WebApplication MapRequestEndpoints(this WebApplication app)
        {
            var requestsGroup = app.MapGroup("/api/requests")
                .RequireAuthorization() // Apply authorization to all endpoints in this group
                .WithTags("Requests");

            // POST /api/requests
            requestsGroup.MapPost("/", async (
                HttpContext context,
                [FromServices] IRequestService requestService,
                [FromBody] CreateRequestDto dto) =>
            {
                var employeeId = context.User.GetEmployeeId(); // Get non-nullable employeeId
                var response = await requestService.CreateRequestAsync(dto);
                return response.IsSuccessful
                    ? Results.CreatedAtRoute("GetRequestByIdRoute", new { id = response.Data?.Id }, response.Data)
                    : MapResponseToIResult(response);
            }).WithOpenApi();

            // GET /api/requests/my-requests
            // FIX: Added RequestFilterDto for pagination/filtering
            requestsGroup.MapGet("/my-requests", async (
                HttpContext context,
                [FromServices] IRequestService requestService,
                [AsParameters] RequestFilterDto filter) => // FIX: Added filter
            {
                var employeeId = context.User.GetEmployeeId(); // Get non-nullable employeeId
                var response = await requestService.GetEmployeeRequestsAsync(employeeId, filter); // FIX: Pass filter
                return response.IsSuccessful
                    ? Results.Ok(response.Data)
                    : MapResponseToIResult(response);
            }).WithOpenApi();

            // GET /api/requests/{id}
            requestsGroup.MapGet("/{id}", async (
                Guid id,
                [FromServices] IRequestService requestService) =>
            {
                var response = await requestService.GetRequestByIdAsync(id);
                return response.IsSuccessful
                    ? Results.Ok(response.Data)
                    : MapResponseToIResult(response);
            }).WithName("GetRequestByIdRoute").WithOpenApi(); // FIX: Added .WithName("GetRequestByIdRoute")

            // GET /api/requests/status
            requestsGroup.MapGet("/status", async (
                HttpContext context,
                [FromServices] IRequestService requestService) =>
            {
                var employeeId = context.User.GetEmployeeId(); // Get non-nullable employeeId
                var response = await requestService.GetEmployeeRequestStatusDetailAsync(employeeId);
                return response.IsSuccessful
                    ? Results.Ok(response.Data)
                    : MapResponseToIResult(response);
            }).WithOpenApi();

            // PUT /api/requests/{id}/dismiss
            requestsGroup.MapPut("/{id}/dismiss", async (
                Guid id,
                HttpContext context,
                [FromServices] IRequestService requestService) =>
            {
                var employeeId = context.User.GetEmployeeId(); // Get non-nullable employeeId
                var response = await requestService.DismissRejectedRequestAsync(id, employeeId);
                return response.IsSuccessful
                    ? Results.NoContent()
                    : MapResponseToIResult(response);
            }).WithOpenApi();

            // PUT /api/requests/{id}/confirm-receipt
            requestsGroup.MapPut("/{id}/confirm-receipt", async (
                Guid id,
                HttpContext context,
                [FromServices] IRequestService requestService) =>
            {
                var employeeId = context.User.GetEmployeeId(); // Get non-nullable employeeId
                var response = await requestService.ConfirmReceiptAsync(id, employeeId);
                return response.IsSuccessful
                    ? Results.NoContent()
                    : MapResponseToIResult(response);
            }).WithOpenApi();

            // GET /api/requests/history
            requestsGroup.MapGet("/history", async (
                HttpContext context,
                [FromServices] IRequestService requestService,
                [AsParameters] HistoryFilterDto filter) =>
            {
                var employeeId = context.User.GetEmployeeId(); // Get non-nullable employeeId
                var response = await requestService.GetEmployeeHistoryAsync(employeeId, filter);
                return response.IsSuccessful
                    ? Results.Ok(response.Data)
                    : MapResponseToIResult(response);
            }).WithOpenApi();

            // GET /api/requests/history/{id}
            requestsGroup.MapGet("/history/{id}", async (
                Guid id,
                HttpContext context,
                [FromServices] IRequestService requestService) =>
            {
                var employeeId = context.User.GetEmployeeId(); // Get non-nullable employeeId
                var response = await requestService.GetHistoryItemByIdAsync(id, employeeId);
                return response.IsSuccessful
                    ? Results.Ok(response.Data)
                    : MapResponseToIResult(response);
            }).WithOpenApi();

            // GET /api/requests/history/export
            requestsGroup.MapGet("/history/export", async (
                HttpContext context,
                [FromServices] IRequestService requestService,
                [AsParameters] HistoryFilterDto filter) =>
            {
                var employeeId = context.User.GetEmployeeId(); // Get non-nullable employeeId
                var response =
                    await requestService.ExportEmployeeHistoryAsync(employeeId, filter); // FIX: Get Response<byte[]>
                if (response.IsSuccessful && response.Data != null)
                {
                    var fileName = $"LaptopRequisitionHistory_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
                    return Results.File(response.Data,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName); // FIX: Pass byte[] directly
                }

                return MapResponseToIResult(response); // FIX: Handle unsuccessful response
            }).WithOpenApi();

            // POST /api/requests/report-issue
            requestsGroup.MapPost("/report-issue", async (
                HttpContext context,
                [FromServices] IRequestService requestService,
                [FromBody] ReportIssueDto dto) =>
            {
                var employeeId = context.User.GetEmployeeId(); // Get non-nullable employeeId
                var response = await requestService.ReportIssueAsync(employeeId, dto);
                return response.IsSuccessful
                    ? Results.Ok(new { message = "Issue reported successfully to IT." })
                    : MapResponseToIResult(response);
            }).WithOpenApi();

            // REMOVED: Conflicting Minimal API endpoint for GET /api/admin/requests
            // app.MapGet("/api/admin/requests", async (
            //     [FromServices] IRequestService requestService,
            //     [AsParameters] RequestFilterDto filter) =>
            // {
            //     var response = await requestService.GetAllRequestsAsync(filter);
            //     return response.IsSuccessful
            //         ? Results.Ok(response.Data)
            //         : MapResponseToIResult(response);
            // })
            // .RequireAuthorization(policy => policy.RequireRole("REQUISITION_PORTAL_ADMIN", "Super Admin")) // Admin roles
            // .WithTags("Requests")
            // .WithOpenApi();

            // REMOVED: Conflicting Minimal API endpoint for GET /api/admin/requests/export
            // app.MapGet("/api/admin/requests/export", async (
            //         [FromServices] IRequestService requestService,
            //         [AsParameters] AdminRequestFilterDto filter) =>
            //     {
            //         var response = await requestService.ExportFilteredRequestsForAdminAsync(filter);
            //         if (response.IsSuccessful && response.Data != null)
            //         {
            //             var fileName = $"AdminLaptopRequests_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            //             return Results.File(response.Data,
            //                 "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            //         }
            //
            //         return MapResponseToIResult(response);
            //     })
            //     .RequireAuthorization(policy =>
            //         policy.RequireRole("REQUISITION_PORTAL_ADMIN", "Super Admin")) // Admin roles
            //     .WithTags("Requests")
            //     .WithOpenApi();


            return app;
        }

        // Helper to map Response<T> to IResult
        private static IResult MapResponseToIResult<T>(Response<T> response)
        {
            return response.Code switch
            {
                LaptopRequisition.Domain.Enums.ResponseCode.NotFound => Results.NotFound(new
                    { message = response.Message }),
                LaptopRequisition.Domain.Enums.ResponseCode.BadRequest => Results.BadRequest(new
                    { message = response.Message }),
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
                LaptopRequisition.Domain.Enums.ResponseCode.NotFound => Results.NotFound(new
                    { message = response.Message }),
                LaptopRequisition.Domain.Enums.ResponseCode.BadRequest => Results.BadRequest(new
                    { message = response.Message }),
                LaptopRequisition.Domain.Enums.ResponseCode.Unauthorized => Results.Unauthorized(),
                LaptopRequisition.Domain.Enums.ResponseCode.Forbidden => Results.Forbid(),
            };
        }
    }
}