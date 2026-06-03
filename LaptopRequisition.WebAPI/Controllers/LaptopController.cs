using LaptopRequisition.Application.DTOs.Laptop;
using LaptopRequisition.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // Added for Authorize attribute
using System; // Added for Guid
using System.Collections.Generic; // Added for IEnumerable
using System.Threading.Tasks; // Added for Task
using Microsoft.AspNetCore.Http; // Added for StatusCodes
using LaptopRequisition.Application.DTOs; // Added for PaginatedResultDto
using LaptopRequisition.Application.DTOs.Admin; // Added for BulkUploadResultDto
using System.Security.Claims; // Added for Claims

namespace LaptopRequisition.WebAPI.Controllers;

[ApiController]
[Route("api/admin/laptops")] // Changed base route to admin-specific
[Authorize(Roles = "REQUISITION_PORTAL_ADMIN,Super Admin")] // FIX: Updated to match SSO admin roles
public class LaptopsController : ControllerBase
{
    private readonly ILaptopService _laptopService;

    public LaptopsController(ILaptopService laptopService)
    {
        _laptopService = laptopService;
    }

    [HttpPost] // POST /api/admin/laptops (Create Laptop)
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(LaptopResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateLaptopDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        try
        {
            var result = await _laptopService.CreateLaptopAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while creating the laptop.", details = ex.Message });
        }
    }

    [HttpGet] // GET /api/admin/laptops (Get Filtered and Paginated Laptops)
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResultDto<LaptopResponseDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFilteredAndPaginatedLaptops([FromQuery] LaptopFilterDto filter)
    {
        try
        {
            var result = await _laptopService.GetFilteredAndPaginatedLaptopsAsync(filter);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching laptops.", details = ex.Message });
        }
    }

    [HttpGet("{id}")] // GET /api/admin/laptops/{id} (Get Laptop by ID)
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LaptopResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _laptopService.GetLaptopByIdAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex) // For "Laptop not found"
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching the laptop.", details = ex.Message });
        }
    }

    [HttpPut("{id}")] // PUT /api/admin/laptops/{id} (Update Laptop)
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LaptopResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLaptopDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        try
        {
            // Retrieve UserId and UserName from claims
            var userIdClaim = User.FindFirst("SourceId");
            var userNameClaim = User.FindFirst("name");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized(new { message = "User ID not found in claims." });
            }
            var userName = userNameClaim?.Value ?? "Unknown User";

            var result = await _laptopService.UpdateLaptopAsync(id, dto, userId, userName);
            return Ok(result);
        }
        catch (InvalidOperationException ex) // For "Laptop not found"
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while updating the laptop.", details = ex.Message });
        }
    }

    [HttpDelete("{id}")] // DELETE /api/admin/laptops/{id} (Delete Laptop)
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _laptopService.DeleteLaptopAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex) // For "Laptop not found" or "Cannot delete assigned laptop"
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new { message = ex.Message });
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while deleting the laptop.", details = ex.Message });
        }
    }

    [HttpPost("{laptopId}/assign/{employeeId}")] // POST /api/admin/laptops/{laptopId}/assign/{employeeId}
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AdminAssignLaptop(Guid laptopId, Guid employeeId)
    {
        try
        {
            await _laptopService.AdminAssignLaptopAsync(laptopId, employeeId);
            return Ok(new { message = $"Laptop {laptopId} assigned to employee {employeeId} successfully." });
        }
        catch (InvalidOperationException ex) // For "Laptop not found", "Employee not found", "Laptop already assigned", "Employee already has laptop"
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new { message = ex.Message });
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred during laptop assignment.", details = ex.Message });
        }
    }

    [HttpPut("{laptopId}/unassign")] // PUT /api/admin/laptops/{laptopId}/unassign
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AdminUnassignLaptop(Guid laptopId)
    {
        try
        {
            await _laptopService.AdminUnassignLaptopAsync(laptopId);
            return Ok(new { message = $"Laptop {laptopId} unassigned successfully." });
        }
        catch (InvalidOperationException ex) // For "Laptop not found", "Laptop not assigned"
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new { message = ex.Message });
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred during laptop unassignment.", details = ex.Message });
        }
    }

    [HttpPost("bulk-upload")] // POST /api/admin/laptops/bulk-upload
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<BulkUploadResultDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BulkUploadLaptops(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded or file is empty." });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only CSV files are allowed." });
        }

        try
        {
            var results = await _laptopService.BulkUploadLaptopsAsync(file);
            return Ok(results);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred during bulk upload.", details = ex.Message });
        }
    }

    [HttpGet("export")] // GET /api/admin/laptops/export
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportFilteredLaptops([FromQuery] LaptopFilterDto filter)
    {
        try
        {
            var fileContents = await _laptopService.ExportFilteredLaptopsAsync(filter);
            return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LaptopInventory.xlsx");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while exporting laptops.", details = ex.Message });
        }
    }
}