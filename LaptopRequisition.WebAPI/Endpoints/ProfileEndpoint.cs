using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Employee;
using LaptopRequisition.Application.Extensions;
using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
// Added for LINQ operations

// NEW: Added for ClaimsPrincipalExtensions

namespace LaptopRequisition.WebAPI.Endpoints;

public static class ProfileEndpoint
{
    public static WebApplication MapProfileEndpoint(this WebApplication app)
    {
        // -------------------------
        // GET ALL PROFILES (ADMIN)
        // -------------------------
        app.MapGet("/api/v1/profiles", // Changed route to /profiles
            async (HttpContext context,
                [FromServices] IProfileService service,
                [AsParameters] EmployeeFilterDto filter) =>
            {
                try
                {
                    var profiles = await service.GetProfilesAsync(filter);
                    return Results.Ok(profiles);
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
            .RequireAuthorization(policy =>
                policy.RequireRole("REQUISITION_PORTAL_ADMIN", "Super Admin")) // Updated roles
            .WithTags("ProfileService");

        // -------------------------
        // GET CURRENT EMPLOYEE PROFILE
        // -------------------------
        app.MapGet("/api/v1/profile", // New endpoint for current employee
            async (HttpContext context,
                [FromServices] IProfileService service) =>
            {
                try
                {
                    var employeeId = context.User.GetEmployeeId(); // FIX: Use extension method
                    var profile = await service.GetProfileAsync(employeeId);
                    return Results.Ok(profile);
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
            .RequireAuthorization() // Any authenticated user
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
                    var employeeId = context.User.GetEmployeeId(); // FIX: Use extension method
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
            .RequireAuthorization() // Uncommented authorization
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
                    var employeeId = context.User.GetEmployeeId(); // FIX: Use extension method
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
            .DisableAntiforgery()
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
                    var employeeId = context.User.GetEmployeeId(); // FIX: Use extension method
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
        // DEBUG: CLAIMS (Removed)
        // -------------------------
        // app.MapGet("/api/v1/profile/claims",
        //     (HttpContext context) =>
        //     {
        //         return Results.Ok(context.User.Claims.Select(c => new
        //         {
        //             c.Type,
        //             c.Value
        //         }));
        //     })
        //     .WithTags("ProfileService");


        // -------------------------
        // DEBUG: USER (Removed)
        // -------------------------
        // app.MapGet("/api/v1/profile/debug-user",
        //     (HttpContext context) =>
        //     {
        //         return Results.Ok(new
        //         {
        //             IsAuthenticated = context.User.Identity?.IsAuthenticated,
        //             Claims = context.User.Claims.Select(x => new
        //             {
        //                 x.Type,
        //                 x.Value
        //             }),
        //             SourceId = context.User.FindFirst("SourceId")?.Value
        //         });
        //     })
        //     .WithTags("ProfileService");

        return app;
    }

    // -------------------------
    // Helper (Removed - now using extension method)
    // -------------------------
    // private static Guid GetCurrentEmployeeId(HttpContext context)
    // {
    //     var employeeId = context.User.FindFirst("SourceId")?.Value;

    //     if (string.IsNullOrEmpty(employeeId))
    //         throw new UnauthorizedAccessException(
    //             "User not authenticated or employee ID not found in token.");

    //     return Guid.Parse(employeeId);
    // }
}