using LaptopRequisition.Application.DTOs.Laptop; // Updated namespace for Laptop DTOs
using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain;
using LaptopRequisition.Domain.Enums; // Added for OperatingSystemEnum and LaptopStatus
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs; // Added for PaginatedResultDto
using Microsoft.AspNetCore.Http; // Added for IFormFile
using LaptopRequisition.Application.DTOs.Admin; // Added for BulkUploadResultDto
using CsvHelper; // Added for CsvHelper
using System.Globalization; // Added for CultureInfo
using CsvHelper.Configuration; // Added for CsvConfiguration
using System.IO; // Added for MemoryStream
using System.ComponentModel.DataAnnotations; // Added for ValidationContext
using System.Text.Json; // Added for JsonSerializer
using System.Security.Claims; // Added for Claims
using ClosedXML.Excel;
using LaptopRequisition.Application.DTOs.Page; // Added for ClosedXML

namespace LaptopRequisition.Application.Services;

public class LaptopService : ILaptopService
{
    private readonly ILaptopRepository _laptopRepository;
    private readonly IEmployeeRepository _employeeRepository; // Added
    private readonly IAuditLogRepository _auditLogRepository; // Added
    private readonly IHttpContextAccessor _httpContextAccessor; // Added
    private readonly INotificationService _notificationService; // Added

    public LaptopService(ILaptopRepository laptopRepository, 
                         IEmployeeRepository employeeRepository,
                         IAuditLogRepository auditLogRepository, // Added
                         IHttpContextAccessor httpContextAccessor, // Added
                         INotificationService notificationService) // Added
    {
        _laptopRepository = laptopRepository;
        _employeeRepository = employeeRepository;
        _auditLogRepository = auditLogRepository; // Initialized
        _httpContextAccessor = httpContextAccessor; // Initialized
        _notificationService = notificationService; // Initialized
    }

    public async Task<LaptopResponseDto> CreateLaptopAsync(CreateLaptopDto dto)
    {
        var existingLaptopBySerialNumber = await _laptopRepository
            .GetBySerialNumberAsync(dto.SerialNumber);
    
        if (existingLaptopBySerialNumber != null)
        {
            throw new InvalidOperationException(
                $"Laptop with serial number '{dto.SerialNumber}' already exists.");
        }

        // New: Check for unique AssetTag
        var existingLaptopByAssetTag = await _laptopRepository
            .GetByAssetTagAsync(dto.AssetTag);

        if (existingLaptopByAssetTag != null)
        {
            throw new InvalidOperationException(
                $"Laptop with asset tag '{dto.AssetTag}' already exists.");
        }
    
        var laptop = new Laptop
        {
            Id = Guid.NewGuid(),
            AssetTag = dto.AssetTag,
            Brand = dto.Brand,
            Model = dto.Model,
            SerialNumber = dto.SerialNumber,
            Processor = dto.Processor,
            RAM = dto.RAM,
            Storage = dto.Storage,
            OperatingSystem = dto.OperatingSystem,
            ScreenSize = dto.ScreenSize,
            Status = dto.Status, // Use the new Status property from DTO
            AssignedToEmployeeId = null, // New laptops are not assigned
            AssignedAt = null,           // New laptops are not assigned
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            PurchaseDate = dto.PurchaseDate, // New
            WarrantyExpiryDate = dto.WarrantyExpiryDate // New
        };
    
        await _laptopRepository.AddAsync(laptop);
    
        return new LaptopResponseDto
        {
            Id = laptop.Id,
            AssetTag = laptop.AssetTag,
            Brand = laptop.Brand,
            Model = laptop.Model,
            SerialNumber = laptop.SerialNumber,
            Processor = laptop.Processor,
            RAM = laptop.RAM,
            Storage = laptop.Storage,
            OperatingSystem = laptop.OperatingSystem,
            ScreenSize = laptop.ScreenSize,
            Status = laptop.Status, // Map the new Status property
            AssignedToEmployeeId = laptop.AssignedToEmployeeId,
            AssignedAt = laptop.AssignedAt,
            PurchaseDate = laptop.PurchaseDate, // New
            WarrantyExpiryDate = laptop.WarrantyExpiryDate // New
        };
    }

    public async Task<IEnumerable<LaptopResponseDto>> GetAllLaptopsAsync()
    {
        var laptops = await _laptopRepository.GetAllAsync();

        return laptops.Select(l => new LaptopResponseDto
        {
            Id = l.Id,
            AssetTag = l.AssetTag,
            Brand = l.Brand,
            Model = l.Model,
            SerialNumber = l.SerialNumber,
            Processor = l.Processor,
            RAM = l.RAM,
            Storage = l.Storage,
            OperatingSystem = l.OperatingSystem,
            ScreenSize = l.ScreenSize,
            Status = l.Status, // Map the new Status property
            AssignedToEmployeeId = l.AssignedToEmployeeId,
            AssignedAt = l.AssignedAt,
            PurchaseDate = l.PurchaseDate, // New
            WarrantyExpiryDate = l.WarrantyExpiryDate // New
        });
    }

    public async Task<LaptopResponseDto> GetLaptopByIdAsync(Guid id)
    {
        var laptop = await _laptopRepository.GetByIdAsync(id);

        if (laptop == null)
        {
            throw new InvalidOperationException("Laptop not found.");
        }

        return new LaptopResponseDto
        {
            Id = laptop.Id,
            AssetTag = laptop.AssetTag,
            Brand = laptop.Brand,
            Model = laptop.Model,
            SerialNumber = laptop.SerialNumber,
            Processor = laptop.Processor,
            RAM = laptop.RAM,
            Storage = laptop.Storage,
            OperatingSystem = laptop.OperatingSystem,
            ScreenSize = laptop.ScreenSize,
            Status = laptop.Status, // Map the new Status property
            AssignedToEmployeeId = laptop.AssignedToEmployeeId,
            AssignedAt = laptop.AssignedAt,
            PurchaseDate = laptop.PurchaseDate, // New
            WarrantyExpiryDate = laptop.WarrantyExpiryDate // New
        };
    }

    public async Task<LaptopResponseDto> UpdateLaptopAsync(Guid id, UpdateLaptopDto dto, Guid userId, string userName) // Updated signature
    {
        var laptop = await _laptopRepository.GetByIdAsync(id);

        if (laptop == null)
        {
            throw new InvalidOperationException("Laptop not found.");
        }

        // --- New Validation Logic for Status Change ---
        if (laptop.Status == LaptopStatus.Assigned && 
            (dto.Status == LaptopStatus.UnderRepair || dto.Status == LaptopStatus.Decommissioned))
        {
            throw new InvalidOperationException("Cannot change status of an assigned laptop to Under Repair or Decommissioned. Please unassign it first.");
        }
        // --- End New Validation Logic ---

        // Capture original values for auditing
        var originalLaptop = new Laptop
        {
            AssetTag = laptop.AssetTag,
            Brand = laptop.Brand,
            Model = laptop.Model,
            SerialNumber = laptop.SerialNumber,
            Processor = laptop.Processor,
            RAM = laptop.RAM,
            Storage = laptop.Storage,
            OperatingSystem = laptop.OperatingSystem,
            ScreenSize = laptop.ScreenSize,
            Status = laptop.Status,
            PurchaseDate = laptop.PurchaseDate,
            WarrantyExpiryDate = laptop.WarrantyExpiryDate
        };

        // Apply updates
        laptop.AssetTag = dto.AssetTag;
        laptop.Brand = dto.Brand;
        laptop.Model = dto.Model;
        laptop.SerialNumber = dto.SerialNumber;
        laptop.Processor = dto.Processor;
        laptop.RAM = dto.RAM;
        laptop.Storage = dto.Storage;
        laptop.OperatingSystem = dto.OperatingSystem;
        laptop.ScreenSize = dto.ScreenSize;
        laptop.Status = dto.Status; // Use the new Status property from DTO
        laptop.UpdatedAt = DateTime.UtcNow;

        // New: Capture changes for audit log
        var changes = new Dictionary<string, object>();
        if (originalLaptop.AssetTag != laptop.AssetTag) changes.Add("AssetTag", new { Original = originalLaptop.AssetTag, New = laptop.AssetTag });
        if (originalLaptop.Brand != laptop.Brand) changes.Add("Brand", new { Original = originalLaptop.Brand, New = laptop.Brand });
        if (originalLaptop.Model != laptop.Model) changes.Add("Model", new { Original = originalLaptop.Model, New = laptop.Model });
        if (originalLaptop.SerialNumber != laptop.SerialNumber) changes.Add("SerialNumber", new { Original = originalLaptop.SerialNumber, New = laptop.SerialNumber });
        if (originalLaptop.Processor != laptop.Processor) changes.Add("Processor", new { Original = originalLaptop.Processor, New = laptop.Processor });
        if (originalLaptop.RAM != laptop.RAM) changes.Add("RAM", new { Original = originalLaptop.RAM, New = laptop.RAM });
        if (originalLaptop.Storage != laptop.Storage) changes.Add("Storage", new { Original = originalLaptop.Storage, New = laptop.Storage });
        if (originalLaptop.OperatingSystem != laptop.OperatingSystem) changes.Add("OperatingSystem", new { Original = originalLaptop.OperatingSystem, New = laptop.OperatingSystem });
        if (originalLaptop.ScreenSize != laptop.ScreenSize) changes.Add("ScreenSize", new { Original = originalLaptop.ScreenSize, New = laptop.ScreenSize });
        if (originalLaptop.Status != laptop.Status) changes.Add("Status", new { Original = originalLaptop.Status, New = laptop.Status });
        if (originalLaptop.PurchaseDate != laptop.PurchaseDate) changes.Add("PurchaseDate", new { Original = originalLaptop.PurchaseDate, New = laptop.PurchaseDate });
        if (originalLaptop.WarrantyExpiryDate != laptop.WarrantyExpiryDate) changes.Add("WarrantyExpiryDate", new { Original = originalLaptop.WarrantyExpiryDate, New = laptop.WarrantyExpiryDate });

        if (changes.Any())
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityId = laptop.Id,
                EntityType = "Laptop",
                Action = "Update",
                Changes = JsonSerializer.Serialize(changes),
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow
            };
            await _auditLogRepository.AddAsync(auditLog);
        }

        await _laptopRepository.UpdateAsync(laptop);

        // --- New Workflow for Repaired Laptop Becoming Available ---
        if (originalLaptop.Status == LaptopStatus.UnderRepair && laptop.Status == LaptopStatus.Available)
        {
            // Check if this laptop was previously assigned to an employee
            if (originalLaptop.AssignedToEmployeeId.HasValue)
            {
                var employeeId = originalLaptop.AssignedToEmployeeId.Value;

                // Check if this employee currently has *any* assigned laptop (a replacement)
                var currentAssignedLaptop = await _laptopRepository.GetAnyAssignedLaptopByEmployeeIdAsync(employeeId);

                // If the employee has a laptop assigned, and it's NOT the one that just got repaired
                if (currentAssignedLaptop != null && currentAssignedLaptop.Id != laptop.Id)
                {
                    // Notify the employee to return the replacement laptop
                    await _notificationService.CreateNotificationAsync(
                        employeeId,
                        $"Your repaired laptop ({laptop.SerialNumber}) is now available! Please return your replacement laptop ({currentAssignedLaptop.SerialNumber})."
                    );
                    // Optionally, you might want to update a flag on the employee or create a specific return request.
                }
            }
        }
        // --- End New Workflow ---

        return new LaptopResponseDto
        {
            Id = laptop.Id,
            AssetTag = laptop.AssetTag,
            Brand = laptop.Brand,
            Model = laptop.Model,
            SerialNumber = laptop.SerialNumber,
            Processor = laptop.Processor,
            RAM = laptop.RAM,
            Storage = laptop.Storage,
            OperatingSystem = laptop.OperatingSystem,
            ScreenSize = laptop.ScreenSize,
            Status = laptop.Status, // Map the new Status property
            AssignedToEmployeeId = laptop.AssignedToEmployeeId,
            AssignedAt = laptop.AssignedAt,
            PurchaseDate = laptop.PurchaseDate, // New
            WarrantyExpiryDate = laptop.WarrantyExpiryDate // New
        };
    }

    public async Task DeleteLaptopAsync(Guid id)
    {
        var laptop = await _laptopRepository.GetByIdAsync(id);
    
        if (laptop == null)
        {
            throw new InvalidOperationException("Laptop not found.");
        }
    
        if (laptop.Status == LaptopStatus.Assigned) // Check against new Status property
        {
            throw new InvalidOperationException(
                "Cannot delete an assigned laptop.");
        }
    
        await _laptopRepository.DeleteAsync(id);
    }

    // New methods for admin laptop assignment
    public async Task AdminAssignLaptopAsync(Guid laptopId, Guid employeeId)
    {
        var laptop = await _laptopRepository.GetByIdAsync(laptopId);
        if (laptop == null)
        {
            throw new InvalidOperationException("Laptop not found.");
        }

        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        if (employee == null)
        {
            throw new InvalidOperationException("Employee not found.");
        }

        if (laptop.Status == LaptopStatus.Assigned)
        {
            throw new InvalidOperationException($"Laptop '{laptop.SerialNumber}' is already assigned.");
        }

        // Check if the employee already has an assigned laptop
        var existingAssignedLaptop = await _laptopRepository.GetAssignedLaptopByEmployeeIdAsync(employeeId);
        if (existingAssignedLaptop != null)
        {
            throw new InvalidOperationException($"Employee '{employee.FullName}' already has laptop '{existingAssignedLaptop.SerialNumber}' assigned. Please unassign it first.");
        }

        laptop.Status = LaptopStatus.Assigned;
        laptop.AssignedToEmployeeId = employeeId;
        laptop.AssignedAt = DateTime.UtcNow;
        laptop.UpdatedAt = DateTime.UtcNow;

        await _laptopRepository.UpdateAsync(laptop);
    }

    public async Task AdminUnassignLaptopAsync(Guid laptopId)
    {
        var laptop = await _laptopRepository.GetByIdAsync(laptopId);
        if (laptop == null)
        {
            throw new InvalidOperationException("Laptop not found.");
        }

        if (laptop.Status != LaptopStatus.Assigned)
        {
            throw new InvalidOperationException($"Laptop '{laptop.SerialNumber}' is not currently assigned.");
        }

        laptop.Status = LaptopStatus.Available;
        laptop.AssignedToEmployeeId = null;
        laptop.AssignedAt = null;
        laptop.UpdatedAt = DateTime.UtcNow;

        await _laptopRepository.UpdateAsync(laptop);
    }

    public async Task<PaginatedResultDto<LaptopResponseDto>> GetFilteredAndPaginatedLaptopsAsync(LaptopFilterDto filter)
    {
        var paginatedLaptops = await _laptopRepository.GetFilteredAndPaginatedLaptopsAsync(filter);

        var mappedItems = paginatedLaptops.Items.Select(l => new LaptopResponseDto
        {
            Id = l.Id,
            AssetTag = l.AssetTag,
            Brand = l.Brand,
            Model = l.Model,
            SerialNumber = l.SerialNumber,
            Processor = l.Processor,
            RAM = l.RAM,
            Storage = l.Storage,
            OperatingSystem = l.OperatingSystem,
            ScreenSize = l.ScreenSize,
            Status = l.Status,
            AssignedToEmployeeId = l.AssignedToEmployeeId,
            AssignedAt = l.AssignedAt,
            // Include employee details if assigned
            AssignedToEmployeeName = l.AssignedToEmployee?.FullName,
            PurchaseDate = l.PurchaseDate, // New
            WarrantyExpiryDate = l.WarrantyExpiryDate // New
        }).ToList();

        return new PaginatedResultDto<LaptopResponseDto>
        {
            Items = mappedItems,
            TotalCount = paginatedLaptops.TotalCount,
            PageNumber = paginatedLaptops.PageNumber,
            PageSize = paginatedLaptops.PageSize
        };
    }

    public async Task<List<BulkUploadResultDto>> BulkUploadLaptopsAsync(IFormFile csvFile)
    {
        var results = new List<BulkUploadResultDto>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
        };

        using (var reader = new StreamReader(csvFile.OpenReadStream()))
        using (var csv = new CsvReader(reader, config))
        {
            csv.Context.RegisterClassMap<BulkUploadLaptopDtoMap>();
            var records = csv.GetRecords<BulkUploadLaptopDto>().ToList();

            foreach (var record in records)
            {
                var result = new BulkUploadResultDto
                {
                    StaffId = record.SerialNumber, // Using SerialNumber as identifier for laptops
                    Email = record.AssetTag, // Using AssetTag as secondary identifier
                    IsSuccess = false
                };

                try
                {
                    // Validate DTO properties
                    var validationContext = new ValidationContext(record, serviceProvider: null, items: null);
                    var validationResults = new List<ValidationResult>();
                    if (!Validator.TryValidateObject(record, validationContext, validationResults, validateAllProperties: true))
                    {
                        result.ErrorMessage = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
                        results.Add(result);
                        continue;
                    }

                    // Check if laptop with same serial number already exists
                    var existingLaptop = await _laptopRepository.GetBySerialNumberAsync(record.SerialNumber);
                    if (existingLaptop != null)
                    {
                        result.ErrorMessage = $"Laptop with serial number '{record.SerialNumber}' already exists.";
                        results.Add(result);
                        continue;
                    }

                    // Create the laptop
                    var createLaptopDto = new CreateLaptopDto
                    {
                        AssetTag = record.AssetTag,
                        Brand = record.Brand,
                        Model = record.Model,
                        SerialNumber = record.SerialNumber,
                        Processor = record.Processor,
                        RAM = record.RAM,
                        Storage = record.Storage,
                        OperatingSystem = record.OperatingSystem,
                        ScreenSize = record.ScreenSize,
                        Status = record.Status // Use status from CSV
                    };

                    await CreateLaptopAsync(createLaptopDto); // Use existing service method
                    result.IsSuccess = true;
                }
                catch (InvalidOperationException ex)
                {
                    result.ErrorMessage = ex.Message;
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
                }
                results.Add(result);
            }
        }
        return results;
    }

    public async Task<byte[]> ExportFilteredLaptopsAsync(LaptopFilterDto filter)
    {
        // Retrieve all filtered laptops (no pagination for export)
        var allFilteredLaptops = (await _laptopRepository.GetFilteredAndPaginatedLaptopsAsync(new LaptopFilterDto
        {
            SearchTerm = filter.SearchTerm,
            Brand = filter.Brand,
            Model = filter.Model,
            Status = filter.Status,
            IsAssigned = filter.IsAssigned,
            AssignedToEmployeeId = filter.AssignedToEmployeeId,
            SortBy = filter.SortBy,
            SortOrder = filter.SortOrder,
            PageNumber = 1, // Get all pages
            PageSize = int.MaxValue // Get all items
        })).Items;

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Laptop Inventory");

            // Add headers
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Asset Tag";
            worksheet.Cell(1, 3).Value = "Brand";
            worksheet.Cell(1, 4).Value = "Model";
            worksheet.Cell(1, 5).Value = "Serial Number";
            worksheet.Cell(1, 6).Value = "Processor";
            worksheet.Cell(1, 7).Value = "RAM";
            worksheet.Cell(1, 8).Value = "Storage";
            worksheet.Cell(1, 9).Value = "OS";
            worksheet.Cell(1, 10).Value = "Screen Size";
            worksheet.Cell(1, 11).Value = "Status";
            worksheet.Cell(1, 12).Value = "Assigned To Employee ID";
            worksheet.Cell(1, 13).Value = "Assigned At";
            worksheet.Cell(1, 14).Value = "Purchase Date";
            worksheet.Cell(1, 15).Value = "Warranty Expiry Date";

            // Add data
            for (int i = 0; i < allFilteredLaptops.Count(); i++)
            {
                var laptop = allFilteredLaptops.ElementAt(i);
                int row = i + 2; // Start from row 2 for data

                worksheet.Cell(row, 1).Value = laptop.Id.ToString();
                worksheet.Cell(row, 2).Value = laptop.AssetTag;
                worksheet.Cell(row, 3).Value = laptop.Brand;
                worksheet.Cell(row, 4).Value = laptop.Model;
                worksheet.Cell(row, 5).Value = laptop.SerialNumber;
                worksheet.Cell(row, 6).Value = laptop.Processor;
                worksheet.Cell(row, 7).Value = laptop.RAM;
                worksheet.Cell(row, 8).Value = laptop.Storage;
                worksheet.Cell(row, 9).Value = laptop.OperatingSystem.ToString();
                worksheet.Cell(row, 10).Value = laptop.ScreenSize;
                worksheet.Cell(row, 11).Value = laptop.Status.ToString();
                worksheet.Cell(row, 12).Value = laptop.AssignedToEmployeeId?.ToString();
                worksheet.Cell(row, 13).Value = laptop.AssignedAt?.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cell(row, 14).Value = laptop.PurchaseDate.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 15).Value = laptop.WarrantyExpiryDate.ToString("yyyy-MM-dd");
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
        }
    }
}

public sealed class BulkUploadLaptopDtoMap : ClassMap<BulkUploadLaptopDto>
{
    public BulkUploadLaptopDtoMap()
    {
        Map(m => m.AssetTag).Name("AssetTag");
        Map(m => m.Brand).Name("Brand");
        Map(m => m.Model).Name("Model");
        Map(m => m.SerialNumber).Name("SerialNumber");
        Map(m => m.Processor).Name("Processor");
        Map(m => m.RAM).Name("RAM");
        Map(m => m.Storage).Name("Storage");
    }
}
    
