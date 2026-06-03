using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Application.DTOs.Notification;
using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Application.Interfaces.External; 
using LaptopRequisition.Domain;
using LaptopRequisition.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options; 
using LaptopRequisition.Application.Configurations; 
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; 
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs.Admin;
using ClosedXML.Excel;

namespace LaptopRequisition.Application.Services
{
    public class ReturnRequestService : IReturnRequestService
    {
        private readonly IReturnRequestRepository _returnRequestRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILaptopRepository _laptopRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;
        private readonly INotificationApi _notificationApi;
        private readonly NotificationApiSettings _notificationApiSettings; 

        public ReturnRequestService(
            IReturnRequestRepository returnRequestRepository,
            IEmployeeRepository employeeRepository,
            ILaptopRepository laptopRepository,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService,
            INotificationApi notificationApi,
            IOptions<NotificationApiSettings> notificationApiSettingsOptions)
        {
            _returnRequestRepository = returnRequestRepository;
            _employeeRepository = employeeRepository;
            _laptopRepository = laptopRepository;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
            _notificationApi = notificationApi;
            _notificationApiSettings = notificationApiSettingsOptions.Value;
        }

        private Guid GetCurrentEmployeeId()
        {
            var employeeId = _httpContextAccessor.HttpContext?.User
                .FindFirst("SourceId")?.Value;

            if (string.IsNullOrEmpty(employeeId))
            {
                throw new UnauthorizedAccessException(
                    "User not authenticated or employee ID not found in token.");
            }

            return Guid.Parse(employeeId);
        }

        private async Task<ReturnRequestResponseDto> MapToDto(ReturnRequest returnRequest)
        {
            Employee? employee = null;
            if (returnRequest.EmployeeId.HasValue) // Handle nullable EmployeeId
            {
                employee = await _employeeRepository.GetByIdAsync(returnRequest.EmployeeId.Value);
            }
            var laptop = await _laptopRepository.GetByIdAsync(returnRequest.LaptopId);

            return new ReturnRequestResponseDto
            {
                Id = returnRequest.Id,
                EmployeeId = returnRequest.EmployeeId,
                EmployeeName = employee?.FullName,
                LaptopId = returnRequest.LaptopId,
                LaptopSerialNumber = laptop?.SerialNumber,
                Reason = returnRequest.Reason,
                Status = Enum.Parse<ReturnRequestStatus>(returnRequest.Status), 
                CreatedAt = returnRequest.CreatedAt,
                ReturnedAt = returnRequest.ReturnedAt,
                UpdatedAt = returnRequest.UpdatedAt
            };
        }

        public async Task<ReturnRequestResponseDto> CreateReturnRequestAsync(CreateReturnRequestDto dto)
        {
            var employeeId = GetCurrentEmployeeId();
            
            var laptop = await _laptopRepository.GetByIdAsync(dto.LaptopId);
            if (laptop == null || laptop.Status != LaptopStatus.Assigned || laptop.AssignedToEmployeeId != employeeId) // Updated check
            {
                throw new InvalidOperationException("Laptop not found or not assigned to the current employee.");
            }
            
            var existingPendingReturn = await _returnRequestRepository.GetPendingReturnRequestByLaptopIdAsync(dto.LaptopId); 
            if (existingPendingReturn != null)
            {
                throw new InvalidOperationException("A pending return request already exists for this laptop.");
            }

            var returnRequest = new ReturnRequest
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                LaptopId = dto.LaptopId,
                Reason = dto.Reason,
                Status = ReturnRequestStatus.Pending.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _returnRequestRepository.AddAsync(returnRequest);
            
            await _notificationService.CreateNotificationAsync(employeeId, $"Your return request for laptop {laptop.SerialNumber} has been submitted and is pending review.");
            
            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee != null)
            {
                // Replaced direct email service with Notification API
                var emailBody = await BuildReturnRequestSubmittedEmailBodyAsync(employee.FullName, laptop.SerialNumber);
                var notificationRequest = new NotificationRequest
                {
                    Channels = new List<string> { "Email" },
                    From = _notificationApiSettings.FromEmail,
                    To = employee.Email,
                    Subject = "Laptop Return Request Submitted",
                    Message = emailBody
                };
                var notificationResponse = await _notificationApi.SendNotificationAsync(notificationRequest);

                if (!notificationResponse.IsSuccessStatusCode || notificationResponse.Content is null || !notificationResponse.Content.IsSuccessful)
                {
                    throw new InvalidOperationException($"Failed to send return request submitted email: {notificationResponse.Error?.Content}");
                }
            }

            return await MapToDto(returnRequest);
        }

        public async Task<ReturnRequestResponseDto> GetReturnRequestByIdAsync(Guid id)
        {
            var returnRequest = await _returnRequestRepository.GetByIdAsync(id);
            if (returnRequest == null)
            {
                throw new InvalidOperationException("Return request not found.");
            }
            return await MapToDto(returnRequest);
        }

        public async Task<IEnumerable<ReturnRequestResponseDto>> GetEmployeeReturnRequestsAsync(Guid employeeId)
        {
            var returnRequests = await _returnRequestRepository.GetByEmployeeIdAsync(employeeId);
            var dtos = new List<ReturnRequestResponseDto>();
            foreach (var rr in returnRequests)
            {
                dtos.Add(await MapToDto(rr));
            }
            return dtos;
        }

        public async Task<IEnumerable<ReturnRequestResponseDto>> GetAllReturnRequestsAsync()
        {
            var returnRequests = await _returnRequestRepository.GetAllAsync();
            var dtos = new List<ReturnRequestResponseDto>();
            foreach (var rr in returnRequests)
            {
                dtos.Add(await MapToDto(rr));
            }
            return dtos;
        }

        public async Task ApproveReturnRequestAsync(ApproveReturnRequestDto dto) // Updated signature
        {
            var returnRequest = await _returnRequestRepository.GetByIdAsync(dto.ReturnRequestId); // Use dto.ReturnRequestId
            if (returnRequest == null)
            {
                throw new InvalidOperationException("Return request not found.");
            }
            if (returnRequest.Status != ReturnRequestStatus.Pending.ToString())
            {
                throw new InvalidOperationException("Only pending return requests can be approved.");
            }

            returnRequest.Status = ReturnRequestStatus.Approved.ToString();
            returnRequest.UpdatedAt = DateTime.UtcNow;
            await _returnRequestRepository.UpdateAsync(returnRequest);
            
            var laptop = await _laptopRepository.GetByIdAsync(returnRequest.LaptopId);
            if (laptop != null)
            {
                laptop.AssignedToEmployeeId = null;
                laptop.AssignedAt = null;
                // Set laptop status based on returned condition
                laptop.Status = dto.ReturnedCondition; 
                await _laptopRepository.UpdateAsync(laptop);
            }
            
            if (returnRequest.EmployeeId.HasValue) // Check for nullability
            {
                await _notificationService.CreateNotificationAsync(returnRequest.EmployeeId.Value, $"Your return request for laptop {laptop?.SerialNumber} has been approved. Laptop condition: {dto.ReturnedCondition}.");
            }
            
            Employee? employee = null;
            if (returnRequest.EmployeeId.HasValue) // Check for nullability
            {
                employee = await _employeeRepository.GetByIdAsync(returnRequest.EmployeeId.Value);
            }

            if (employee != null)
            {
                var emailBody = await BuildReturnRequestApprovedEmailBodyAsync(employee.FullName, laptop?.SerialNumber ?? "N/A");
                var notificationRequest = new NotificationRequest
                {
                    Channels = new List<string> { "Email" },
                    From = _notificationApiSettings.FromEmail,
                    To = employee.Email,
                    Subject = "Laptop Return Request Approved",
                    Message = emailBody
                };
                var notificationResponse = await _notificationApi.SendNotificationAsync(notificationRequest);

                if (!notificationResponse.IsSuccessStatusCode || notificationResponse.Content is null || !notificationResponse.Content.IsSuccessful)
                {
                    throw new InvalidOperationException($"Failed to send return request approved email: {notificationResponse.Error?.Content}");
                }
            }
        }

        public async Task RejectReturnRequestAsync(Guid returnRequestId, string reason)
        {
            var returnRequest = await _returnRequestRepository.GetByIdAsync(returnRequestId);
            if (returnRequest == null)
            {
                throw new InvalidOperationException("Return request not found.");
            }
            if (returnRequest.Status != ReturnRequestStatus.Pending.ToString())
            {
                throw new InvalidOperationException("Only pending return requests can be rejected.");
            }

            returnRequest.Status = ReturnRequestStatus.Rejected.ToString();
            returnRequest.Reason = reason;
            returnRequest.UpdatedAt = DateTime.UtcNow;
            await _returnRequestRepository.UpdateAsync(returnRequest);

            if (returnRequest.EmployeeId.HasValue) // Check for nullability
            {
                await _notificationService.CreateNotificationAsync(returnRequest.EmployeeId.Value, $"Your return request for laptop {returnRequest.LaptopId} has been rejected. Reason: {reason}");
            }
            
            Employee? employee = null;
            if (returnRequest.EmployeeId.HasValue) // Check for nullability
            {
                employee = await _employeeRepository.GetByIdAsync(returnRequest.EmployeeId.Value);
            }
            var laptop = await _laptopRepository.GetByIdAsync(returnRequest.LaptopId); // LaptopId is non-nullable

            if (employee != null)
            {
                var emailBody = await BuildReturnRequestRejectedEmailBodyAsync(employee.FullName, laptop?.SerialNumber ?? "N/A", reason);
                var notificationRequest = new NotificationRequest
                {
                    Channels = new List<string> { "Email" },
                    From = _notificationApiSettings.FromEmail,
                    To = employee.Email,
                    Subject = "Laptop Return Request Rejected",
                    Message = emailBody
                };
                var notificationResponse = await _notificationApi.SendNotificationAsync(notificationRequest);

                if (!notificationResponse.IsSuccessStatusCode || notificationResponse.Content is null || !notificationResponse.Content.IsSuccessful)
                {
                    throw new InvalidOperationException($"Failed to send return request rejected email: {notificationResponse.Error?.Content}");
                }
            }
        }

        public async Task DeleteReturnRequestAsync(Guid returnRequestId)
        {
            var returnRequest = await _returnRequestRepository.GetByIdAsync(returnRequestId);
            if (returnRequest == null)
            {
                throw new InvalidOperationException("Return request not found.");
            }
            await _returnRequestRepository.DeleteAsync(returnRequestId);
        }
        
        private async Task<string> BuildReturnRequestSubmittedEmailBodyAsync(string employeeName, string laptopSerialNumber)
        {
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "ReturnRequestSubmitted.html");
            if (!File.Exists(templatePath))
            {
                return $"Dear {employeeName},\n\nYour request to return laptop {laptopSerialNumber} has been submitted successfully. We will notify you once it has been processed.\n\nBest regards,\nLRS Team";
            }
            var body = await File.ReadAllTextAsync(templatePath);
            return body
                .Replace("{{employeeName}}", employeeName)
                .Replace("{{laptopSerialNumber}}", laptopSerialNumber);
        }
        
        private async Task<string> BuildReturnRequestApprovedEmailBodyAsync(string employeeName, string laptopSerialNumber)
        {
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "ReturnRequestApproved.html");
            if (!File.Exists(templatePath))
            {
                return $"Dear {employeeName},\n\nYour request to return laptop {laptopSerialNumber} has been approved. Please proceed with the physical return process.\n\nBest regards,\nLRS Team";
            }
            var body = await File.ReadAllTextAsync(templatePath);
            return body
                .Replace("{{employeeName}}", employeeName)
                .Replace("{{laptopSerialNumber}}", laptopSerialNumber);
        }
        
        private async Task<string> BuildReturnRequestRejectedEmailBodyAsync(string employeeName, string laptopSerialNumber, string reason)
        {
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "ReturnRequestRejected.html");
            if (!File.Exists(templatePath))
            {
                return $"Dear {employeeName},\n\nYour request to return laptop {laptopSerialNumber} has been rejected. Reason: {reason}\n\nBest regards,\nLRS Team";
            }
            var body = await File.ReadAllTextAsync(templatePath);
            return body
                .Replace("{{employeeName}}", employeeName)
                .Replace("{{laptopSerialNumber}}", laptopSerialNumber)
                .Replace("{{reason}}", reason);
        }

        public async Task<PaginatedResultDto<ReturnRequestResponseDto>> GetFilteredAndPaginatedReturnRequestsForAdminAsync(AdminReturnRequestFilterDto filter)
        {
            var paginatedReturnRequests = await _returnRequestRepository.GetFilteredAndPaginatedReturnRequestsAsync(filter);

            var mappedItems = new List<ReturnRequestResponseDto>();
            foreach (var rr in paginatedReturnRequests.Items)
            {
                mappedItems.Add(await MapToDto(rr));
            }

            return new PaginatedResultDto<ReturnRequestResponseDto>
            {
                Items = mappedItems,
                TotalCount = paginatedReturnRequests.TotalCount,
                PageNumber = paginatedReturnRequests.PageNumber,
                PageSize = paginatedReturnRequests.PageSize
            };
        }

        public async Task<byte[]> ExportFilteredReturnRequestsForAdminAsync(AdminReturnRequestFilterDto filter)
        {
            // Retrieve all filtered return requests (no pagination for export)
            var allFilteredReturnRequests = (await _returnRequestRepository.GetFilteredAndPaginatedReturnRequestsAsync(new AdminReturnRequestFilterDto
            {
                SearchTerm = filter.SearchTerm,
                Status = filter.Status,
                EmployeeId = filter.EmployeeId,
                LaptopId = filter.LaptopId,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                SortBy = filter.SortBy,
                SortOrder = filter.SortOrder,
                PageNumber = 1, 
                PageSize = int.MaxValue 
            })).Items.ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Admin Return Requests");

              
                worksheet.Cell(1, 1).Value = "Return Request ID";
                worksheet.Cell(1, 2).Value = "Employee Name";
                worksheet.Cell(1, 3).Value = "Employee Email";
                worksheet.Cell(1, 4).Value = "Laptop Serial Number";
                worksheet.Cell(1, 5).Value = "Reason";
                worksheet.Cell(1, 6).Value = "Status";
                worksheet.Cell(1, 7).Value = "Created At";
                worksheet.Cell(1, 8).Value = "Returned At";
                worksheet.Cell(1, 9).Value = "Updated At";

                // Add data
                for (int i = 0; i < allFilteredReturnRequests.Count(); i++)
                {
                    var returnRequest = allFilteredReturnRequests.ElementAt(i);
                    int row = i + 2; 

                    worksheet.Cell(row, 1).Value = returnRequest.Id.ToString();
                    worksheet.Cell(row, 2).Value = returnRequest.Employee?.FullName;
                    worksheet.Cell(row, 3).Value = returnRequest.Employee?.Email;
                    worksheet.Cell(row, 4).Value = returnRequest.Laptop?.SerialNumber;
                    worksheet.Cell(row, 5).Value = returnRequest.Reason;
                    worksheet.Cell(row, 6).Value = returnRequest.Status.ToString();
                    worksheet.Cell(row, 7).Value = returnRequest.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cell(row, 8).Value = returnRequest.ReturnedAt?.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cell(row, 9).Value = returnRequest.UpdatedAt.ToString("yyyy-MM-dd HH:mm");
                }
                
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}