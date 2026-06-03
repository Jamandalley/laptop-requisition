using LaptopRequisition.Application.DTOs.Employee;
using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LaptopRequisition.WebAPI.Controllers;

using Microsoft.AspNetCore.Mvc;

public static class ProfileEndpoint
{
    public static WebApplication MapProfileEndpoint(this WebApplication app)
    {
        // -------------------------
        // GET PROFILE
        // -------------------------
        app.MapGet("/api/v1/profile",
            async (HttpContext context,
                [FromServices] IProfileService service) =>
            {
                try
                {
                    // var employeeId = GetCurrentEmployeeId(context);
                    var profile = await service.GetProfilesAsync();
                    return Results.Ok(profile);
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Unauthorized();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .RequireAuthorization(policy =>
                policy.RequireRole("Super Admin"))
            .WithTags("ProfileService");


        // -------------------------
        // UPDATE PROFILE
        // -------------------------
        app.MapPut("/api/v1/profile",
            async (HttpContext context,
                [FromServices] IProfileService service,
                [FromBody] UpdateProfileDto request) =>
            {
                try
                {
                    var employeeId = GetCurrentEmployeeId(context);
                    await service.UpdateProfileAsync(employeeId, request);
                    return Results.NoContent();
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            // .RequireAuthorization()
            .WithTags("ProfileService");


        // -------------------------
        // UPLOAD PROFILE PICTURE
        // -------------------------
        app.MapPost("/api/v1/profile/picture",
            async (HttpContext context,
                [FromServices] IProfileService service,
                IFormFile file) =>
            {
                try
                {
                    var employeeId = GetCurrentEmployeeId(context);
                    var imageUrl = await service.UploadProfilePictureAsync(employeeId, file);
                    return Results.Ok(new { imageUrl });
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .RequireAuthorization()
            .WithTags("ProfileService");


        // -------------------------
        // REMOVE PROFILE PICTURE
        // -------------------------
        app.MapDelete("/api/v1/profile/picture",
            async (HttpContext context,
                [FromServices] IProfileService service) =>
            {
                try
                {
                    var employeeId = GetCurrentEmployeeId(context);
                    await service.RemoveProfilePictureAsync(employeeId);
                    return Results.NoContent();
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .RequireAuthorization()
            .WithTags("ProfileService");


        // -------------------------
        // DEBUG: CLAIMS
        // -------------------------
        app.MapGet("/api/v1/profile/claims",
            (HttpContext context) =>
            {
                return Results.Ok(context.User.Claims.Select(c => new
                {
                    c.Type,
                    c.Value
                }));
            })
            .WithTags("ProfileService");


        // -------------------------
        // DEBUG: USER
        // -------------------------
        app.MapGet("/api/v1/profile/debug-user",
            (HttpContext context) =>
            {
                return Results.Ok(new
                {
                    IsAuthenticated = context.User.Identity?.IsAuthenticated,
                    Claims = context.User.Claims.Select(x => new
                    {
                        x.Type,
                        x.Value
                    }),
                    SourceId = context.User.FindFirst("SourceId")?.Value
                });
            })
            .WithTags("ProfileService");

        return app;
    }

    // -------------------------
    // Helper
    // -------------------------
    private static Guid GetCurrentEmployeeId(HttpContext context)
    {
        var employeeId = context.User.FindFirst("SourceId")?.Value;

        if (string.IsNullOrEmpty(employeeId))
            throw new UnauthorizedAccessException(
                "User not authenticated or employee ID not found in token.");

        return Guid.Parse(employeeId);
    }
}