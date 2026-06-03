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
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using LaptopRequisition.Application.DTOs.Request; // Added for RequestStatusDetailDto
using ClosedXML.Excel; // Added for ClosedXML
using LaptopRequisition.Application.DTOs.Admin; // Added for AdminRequestFilterDto

namespace LaptopRequisition.Application.Services
{
    public class RequestService : IRequestService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRequestRepository _requestRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILaptopRepository _laptopRepository;
        private readonly INotificationService _notificationService;
        private readonly INotificationApi _notificationApi;
        private readonly NotificationApiSettings _notificationApiSettings;
        private readonly IReturnRequestRepository _returnRequestRepository; // Added

        public RequestService(
            IRequestRepository requestRepository,
            IEmployeeRepository employeeRepository,
            ILaptopRepository laptopRepository,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService,
            INotificationApi notificationApi,
            IOptions<NotificationApiSettings> notificationApiSettingsOptions,
            IReturnRequestRepository returnRequestRepository) // Updated constructor
        {
            _requestRepository = requestRepository;
            _employeeRepository = employeeRepository;
            _laptopRepository = laptopRepository;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
            _notificationApi = notificationApi;
            _notificationApiSettings = notificationApiSettingsOptions.Value;
            _returnRequestRepository = returnRequestRepository; // Initialized
        }

        private Guid GetCurrentEmployeeId()
        {
            var employeeId = _httpContextAccessor.HttpContext?.User
                .FindFirst("SourceId")?.Value;

            if (string.IsNullOrEmpty(employeeId))
                throw new UnauthorizedAccessException(
                    "User not authenticated or employee ID not found in token.");

            return Guid.Parse(employeeId);
        }

        public async Task<RequestResponseDto> CreateRequestAsync(CreateRequestDto dto)
        {
            var employeeId = GetCurrentEmployeeId();

            var existingPending = await _requestRepository.GetPendingRequestByEmployeeIdAsync(employeeId);
            if (existingPending != null)
                throw new InvalidOperationException("You already have a pending request.");

            var request = new Request
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId, // employeeId is Guid, Request.EmployeeId is Guid?, implicit conversion is fine

                Purpose = dto.Purpose,
                PreferredSpecs = dto.PreferredSpecs,
                IsSwapRequest = dto.IsSwapRequest,
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDismissed = false // Default value
            };

            await _requestRepository.AddAsync(request);

            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found for current user.");
            }

            await _notificationService.CreateNotificationAsync(employeeId, $"Your laptop request (ID: {request.Id.ToString().Substring(0, 8)}...) has been submitted successfully.");

            var emailBody = await BuildRequestConfirmationEmailBodyAsync(employee.FullName, "N/A", request.Status.ToString()); // Laptop model is N/A at this stage
            var notificationRequest = new NotificationRequest
            {
                Channels = new List<string> { "Email" },
                From = _notificationApiSettings.FromEmail,
                To = employee.Email,
                Subject = "Laptop Request Submitted Successfully",
                Message = emailBody
            };
            var notificationResponse = await _notificationApi.SendNotificationAsync(notificationRequest);

            if (!notificationResponse.IsSuccessStatusCode || notificationResponse.Content is null || !notificationResponse.Content.IsSuccessful)
            {
                throw new InvalidOperationException($"Failed to send request confirmation email: {notificationResponse.Error?.Content}");
            }

            return Map(request);
        }

        public async Task<RequestResponseDto> GetRequestByIdAsync(Guid id)
        {
            var request = await _requestRepository.GetByIdAsync(id);

            if (request == null)
                throw new InvalidOperationException("Request not found.");

            return Map(request);
        }

        public async Task<IEnumerable<RequestResponseDto>> GetEmployeeRequestsAsync(Guid employeeId)
        {
            var requests = await _requestRepository.GetByEmployeeIdAsync(employeeId);

            return requests.Select(Map);
        }

        public async Task<IEnumerable<RequestResponseDto>> GetAllRequestsAsync()
        {
            var requests = await _requestRepository.GetAllAsync();

            return requests.Select(Map);
        }

        public async Task ApproveRequestAsync(Guid requestId)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);

            if (request == null)
                throw new InvalidOperationException("Request not found.");

            if (request.Status != RequestStatus.Pending)
                throw new InvalidOperationException("Only pending requests can be approved.");

            request.Status = RequestStatus.Approved;
            request.UpdatedAt = DateTime.UtcNow;
            request.ApprovedRejectedAt = DateTime.UtcNow;

            await _requestRepository.UpdateAsync(request);

            if (request.EmployeeId.HasValue) // Check for nullability
            {
                await _notificationService.CreateNotificationAsync(request.EmployeeId.Value, $"Your laptop request (ID: {request.Id.ToString().Substring(0, 8)}...) has been approved!");
            }
        }

        public async Task RejectRequestAsync(Guid requestId, string reason)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);

            if (request == null)
                throw new InvalidOperationException("Request not found.");

            if (request.Status != RequestStatus.Pending)
                throw new InvalidOperationException("Only pending requests can be rejected.");

            request.Status = RequestStatus.Rejected;
            request.RejectionReason = reason;
            request.UpdatedAt = DateTime.UtcNow;
            request.ApprovedRejectedAt = DateTime.UtcNow;

            await _requestRepository.UpdateAsync(request);

            if (request.EmployeeId.HasValue) // Check for nullability
            {
                await _notificationService.CreateNotificationAsync(request.EmployeeId.Value, $"Your laptop request (ID: {request.Id.ToString().Substring(0, 8)}...) has been rejected. Reason: {reason}");
            }
        }

        public async Task AssignLaptopAsync(Guid requestId, Guid laptopId)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);
            var laptop = await _laptopRepository.GetByIdAsync(laptopId);

            if (request == null)
                throw new InvalidOperationException("Request not found.");

            if (laptop == null)
                throw new InvalidOperationException("Laptop not found.");

            if (request.Status != RequestStatus.Approved)
                throw new InvalidOperationException("Only approved requests can be assigned.");

            // --- New Logic for Alternative Device Note ---
            string? alternativeNote = null;
            // Simplified comparison: check if preferred specs are significantly different from assigned laptop
            // In a real app, this would involve more sophisticated parsing and comparison of PreferredSpecs string
            if (!string.IsNullOrEmpty(request.PreferredSpecs))
            {
                var preferredSpecsLower = request.PreferredSpecs.ToLower();
                var assignedLaptopDetails = $"{laptop.Brand} {laptop.Model} {laptop.Processor} {laptop.RAM} {laptop.Storage}".ToLower();

                // Example: if preferred specs mention "MacBook" but assigned is "Dell"
                if ((preferredSpecsLower.Contains("macbook") && !assignedLaptopDetails.Contains("macbook")) ||
                    (preferredSpecsLower.Contains("dell") && !assignedLaptopDetails.Contains("dell")) ||
                    (preferredSpecsLower.Contains("hp") && !assignedLaptopDetails.Contains("hp")) ||
                    (preferredSpecsLower.Contains("lenovo") && !assignedLaptopDetails.Contains("lenovo")) ||
                    (preferredSpecsLower.Contains("chromebook") && !assignedLaptopDetails.Contains("chromebook")) ||
                    (preferredSpecsLower.Contains("windows") && !assignedLaptopDetails.Contains("windows")) ||
                    (preferredSpecsLower.Contains("macos") && !assignedLaptopDetails.Contains("macos")) ||
                    (preferredSpecsLower.Contains("linux") && !assignedLaptopDetails.Contains("linux")) ||
                    (preferredSpecsLower.Contains("chrome os") && !assignedLaptopDetails.Contains("chrome os"))
                    )
                {
                    alternativeNote = $"Assigned laptop ({laptop.Brand} {laptop.Model}) does not fully match preferred specifications: '{request.PreferredSpecs}'.";
                }
                // Add more complex logic here if needed, e.g., comparing RAM, Storage, Processor details
            }
            // --- End New Logic ---

            request.LaptopId = laptopId;
            request.Status = RequestStatus.Assigned;
            request.UpdatedAt = DateTime.UtcNow;
            request.AssignedAt = DateTime.UtcNow;
            request.AlternativeDeviceNote = alternativeNote; // Set the alternative device note

            laptop.Status = LaptopStatus.Assigned; // Updated from laptop.IsAssigned = true;
            laptop.AssignedToEmployeeId = request.EmployeeId; // Ensure laptop is linked to employee
            await _laptopRepository.UpdateAsync(laptop);

            await _requestRepository.UpdateAsync(request);

            if (request.EmployeeId.HasValue) // Check for nullability
            {
                await _notificationService.CreateNotificationAsync(request.EmployeeId.Value, $"A laptop ({laptop.SerialNumber}) has been assigned to your request (ID: {request.Id.ToString().Substring(0, 8)}...). Please check your request status.");
            }
        }

        // New methods for Request Management
        public async Task<RequestStatusDetailDto> GetEmployeeRequestStatusDetailAsync(Guid employeeId)
        {
            var request = await _requestRepository.GetPendingOrApprovedRequestByEmployeeIdAsync(employeeId);

            if (request == null || request.IsDismissed)
            {
                return new RequestStatusDetailDto { HasActiveRequest = false }; // Indicate no active/undismissed request
            }

            var assignedLaptopDto = new AssignedLaptopDetailDto();
            if (request.LaptopId.HasValue)
            {
                var laptop = await _laptopRepository.GetByIdAsync(request.LaptopId.Value);
                if (laptop != null)
                {
                    assignedLaptopDto = new AssignedLaptopDetailDto
                    {
                        Id = laptop.Id,
                        AssetTag = laptop.AssetTag,
                        Brand = laptop.Brand,
                        Model = laptop.Model,
                        SerialNumber = laptop.SerialNumber,
                        Processor = laptop.Processor,
                        RAM = laptop.RAM,
                        Storage = laptop.Storage,
                        OperatingSystem = laptop.OperatingSystem.ToString(),
                        ScreenSize = laptop.ScreenSize,
                        AssignedDate = laptop.AssignedAt ?? DateTime.MinValue
                    };
                }
            }

            var timeline = new List<RequestTimelineEventDto>();
            timeline.Add(new RequestTimelineEventDto { Status = RequestStatus.Pending, Timestamp = request.CreatedAt, Notes = "Request Submitted" });

            if (request.Status >= RequestStatus.Approved && request.ApprovedRejectedAt.HasValue)
            {
                timeline.Add(new RequestTimelineEventDto { Status = RequestStatus.Approved, Timestamp = request.ApprovedRejectedAt, Notes = "Request Approved" });
            }
            else if (request.Status == RequestStatus.Rejected && request.ApprovedRejectedAt.HasValue)
            {
                timeline.Add(new RequestTimelineEventDto { Status = RequestStatus.Rejected, Timestamp = request.ApprovedRejectedAt, Notes = $"Request Rejected: {request.RejectionReason}" });
            }

            if (request.Status >= RequestStatus.Assigned && request.AssignedAt.HasValue)
            {
                timeline.Add(new RequestTimelineEventDto { Status = RequestStatus.Assigned, Timestamp = request.AssignedAt, Notes = $"Laptop Assigned: {assignedLaptopDto.Brand} {assignedLaptopDto.Model}" });
            }

            if (request.IsReceiptConfirmed && request.ReceiptConfirmedAt.HasValue)
            {
                timeline.Add(new RequestTimelineEventDto { Status = RequestStatus.Completed, Timestamp = request.ReceiptConfirmedAt, Notes = "Receipt Confirmed" });
            }


            return new RequestStatusDetailDto
            {
                RequestId = request.Id,
                DateSubmitted = request.CreatedAt,
                RequestedLaptopModel = request.PreferredSpecs, // Or parse from PreferredSpecs if more detailed
                CurrentStatus = request.Status,
                Purpose = request.Purpose,
                PreferredSpecs = request.PreferredSpecs,
                RejectionReason = request.RejectionReason,
                IsReceiptConfirmed = request.IsReceiptConfirmed,
                ReceiptConfirmedAt = request.ReceiptConfirmedAt,
                AssignedLaptop = assignedLaptopDto.Id != Guid.Empty ? assignedLaptopDto : null, // Only include if laptop was assigned
                Timeline = timeline,
                IsDismissed = request.IsDismissed
            };
        }

        public async Task DismissRejectedRequestAsync(Guid requestId, Guid employeeId)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);

            if (request == null)
            {
                throw new InvalidOperationException("Request not found.");
            }
            // Comparison between Guid? and Guid is fine for `request.EmployeeId != employeeId`
            if (request.EmployeeId != employeeId)
            {
                throw new InvalidOperationException("Request does not belong to the current user.");
            }

            if (request.Status != RequestStatus.Rejected)
            {
                throw new InvalidOperationException("Only rejected requests can be dismissed.");
            }

            request.IsDismissed = true;
            request.UpdatedAt = DateTime.UtcNow;
            await _requestRepository.UpdateAsync(request);

            if (request.EmployeeId.HasValue) // Check for nullability
            {
                await _notificationService.CreateNotificationAsync(request.EmployeeId.Value, $"Your rejected request (ID: {request.Id.ToString().Substring(0, 8)}...) has been dismissed.");
            }
        }

        public async Task ConfirmReceiptAsync(Guid requestId, Guid employeeId)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);

            if (request == null)
            {
                throw new InvalidOperationException("Request not found.");
            }
            // Comparison between Guid? and Guid is fine for `request.EmployeeId != employeeId`
            if (request.EmployeeId != employeeId)
            {
                throw new InvalidOperationException("Request does not belong to the current user.");
            }

            if (request.Status != RequestStatus.Assigned)
            {
                throw new InvalidOperationException("Only assigned requests can have receipt confirmed.");
            }

            if (request.IsReceiptConfirmed)
            {
                throw new InvalidOperationException("Receipt already confirmed for this request.");
            }

            request.IsReceiptConfirmed = true;
            request.ReceiptConfirmedAt = DateTime.UtcNow;
            request.Status = RequestStatus.Completed; // Mark as completed after receipt confirmation
            request.UpdatedAt = DateTime.UtcNow;
            await _requestRepository.UpdateAsync(request);

            if (request.EmployeeId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(request.EmployeeId.Value, $"Receipt confirmed for your laptop request (ID: {request.Id.ToString().Substring(0, 8)}...).");
            }
        }

        
        public async Task<PaginatedResultDto<RequestHistoryDto>> GetEmployeeHistoryAsync(Guid employeeId, HistoryFilterDto filter)
        {
            var requestsPaginated = await _requestRepository.GetEmployeeRequestsAsync(employeeId, filter);
            var returnRequestsPaginated = await _returnRequestRepository.GetEmployeeReturnRequestsAsync(employeeId, filter);

            var combinedHistoryItems = new List<RequestHistoryDto>();

            
            foreach (var req in requestsPaginated.Items)
            {
                // Only include if not dismissed, or if filter explicitly asks for dismissed (not implemented yet)
                if (req.IsDismissed && filter.Status != RequestStatus.Rejected) // Assuming dismissed rejected requests are not shown by default
                {
                    continue;
                }

                string? laptopDetails = null;
                if (req.Laptop != null)
                {
                    laptopDetails = $"{req.Laptop.Brand} {req.Laptop.Model} (SN: {req.Laptop.SerialNumber})";
                }

                combinedHistoryItems.Add(new RequestHistoryDto
                {
                    Id = req.Id,
                    Date = req.CreatedAt,
                    RequestType = "Laptop Request",
                    Status = req.Status,
                    LaptopDetails = laptopDetails,
                    Purpose = req.Purpose,
                    Notes = req.RejectionReason // Rejection reason as notes
                });
            }

            // Map ReturnRequests to RequestHistoryDto
            foreach (var retReq in returnRequestsPaginated.Items)
            {
                string? laptopDetails = null;
                if (retReq.Laptop != null)
                {
                    laptopDetails = $"{retReq.Laptop.Brand} {retReq.Laptop.Model} (SN: {retReq.Laptop.SerialNumber})";
                }

                combinedHistoryItems.Add(new RequestHistoryDto
                {
                    Id = retReq.Id,
                    Date = retReq.CreatedAt,
                    RequestType = "Return Request",
                    ReturnStatus = (ReturnRequestStatus)Enum.Parse(typeof(ReturnRequestStatus), retReq.Status), // Assuming retReq.Status is string
                    LaptopDetails = laptopDetails,
                    Reason = retReq.Reason,
                    Notes = retReq.Reason // Return reason as notes
                });
            }

            // Apply RequestType filter to combined list
            if (!string.IsNullOrEmpty(filter.RequestType))
            {
                combinedHistoryItems = combinedHistoryItems
                    .Where(item => item.RequestType.Equals(filter.RequestType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Sort the combined list chronologically (most recent first)
            combinedHistoryItems = combinedHistoryItems.OrderByDescending(item => item.Date).ToList();

            // Calculate total count after all filtering
            var totalCount = combinedHistoryItems.Count;

            // Apply pagination to the combined, filtered, and sorted list
            var paginatedItems = combinedHistoryItems
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            // Calculate duration for each item (example: "3 months")
            foreach (var item in paginatedItems)
            {
                if (item.Status.HasValue && item.Status == RequestStatus.Completed && item.Date != DateTime.MinValue)
                {
                    // For completed requests, duration from creation to completion
                    var request = await _requestRepository.GetByIdAsync(item.Id);
                    if (request != null && request.ReceiptConfirmedAt.HasValue)
                    {
                        item.Duration = (request.ReceiptConfirmedAt.Value - item.Date).Days > 0 ? $"{(request.ReceiptConfirmedAt.Value - item.Date).Days} days" : "Less than a day";
                    }
                }
                else if (item.ReturnStatus.HasValue && item.ReturnStatus == ReturnRequestStatus.Returned && item.Date != DateTime.MinValue) // Changed to Returned
                {
                    // For completed return requests, duration from creation to return
                    var returnRequest = await _returnRequestRepository.GetByIdAsync(item.Id);
                    if (returnRequest != null && returnRequest.ReturnedAt.HasValue)
                    {
                        item.Duration = (returnRequest.ReturnedAt.Value - item.Date).Days > 0 ? $"{(returnRequest.ReturnedAt.Value - item.Date).Days} days" : "Less than a day";
                    }
                }
                else if (item.Date != DateTime.MinValue)
                {
                    // For ongoing requests/returns, duration from creation to now
                    item.Duration = (DateTime.UtcNow - item.Date).Days > 0 ? $"{(DateTime.UtcNow - item.Date).Days} days" : "Less than a day";
                }
            }


            return new PaginatedResultDto<RequestHistoryDto>
            {
                Items = paginatedItems,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<RequestHistoryDto> GetHistoryItemByIdAsync(Guid id, Guid employeeId)
        {
            // Try to find as a Request
            var request = await _requestRepository.GetRequestWithLaptopAndEmployeeAsync(id);
            if (request != null)
            {
                // Safely compare Guid? with Guid
                if (request.EmployeeId.HasValue && request.EmployeeId.Value == employeeId)
                {
                    string? laptopDetails = null;
                    if (request.Laptop != null)
                    {
                        laptopDetails = $"{request.Laptop.Brand} {request.Laptop.Model} (SN: {request.Laptop.SerialNumber})";
                    }

                    string? duration = null;
                    if (request.Status == RequestStatus.Completed && request.ReceiptConfirmedAt.HasValue)
                    {
                        duration = (request.ReceiptConfirmedAt.Value - request.CreatedAt).Days > 0 ? $"{(request.ReceiptConfirmedAt.Value - request.CreatedAt).Days} days" : "Less than a day";
                    }
                    else
                    {
                        duration = (DateTime.UtcNow - request.CreatedAt).Days > 0 ? $"{(DateTime.UtcNow - request.CreatedAt).Days} days" : "Less than a day";
                    }

                    return new RequestHistoryDto
                    {
                        Id = request.Id,
                        Date = request.CreatedAt,
                        RequestType = "Laptop Request",
                        Status = request.Status,
                        LaptopDetails = laptopDetails,
                        Purpose = request.Purpose,
                        Notes = request.RejectionReason,
                        Duration = duration
                    };
                }
            }

            // If not a Request, try to find as a ReturnRequest
            var returnRequest = await _returnRequestRepository.GetReturnRequestWithLaptopAndEmployeeAsync(id);
            if (returnRequest != null)
            {
                // Safely compare Guid? with Guid
                if (returnRequest.EmployeeId.HasValue && returnRequest.EmployeeId.Value == employeeId)
                {
                    string? laptopDetails = null;
                    if (returnRequest.Laptop != null)
                    {
                        laptopDetails = $"{returnRequest.Laptop.Brand} {returnRequest.Laptop.Model} (SN: {returnRequest.Laptop.SerialNumber})";
                    }

                    string? duration = null;
                    if (returnRequest.Status == ReturnRequestStatus.Returned.ToString() && returnRequest.ReturnedAt.HasValue) // Changed to Returned and compare with string
                    {
                        duration = (returnRequest.ReturnedAt.Value - returnRequest.CreatedAt).Days > 0 ? $"{(returnRequest.ReturnedAt.Value - returnRequest.CreatedAt).Days} days" : "Less than a day";
                    }
                    else
                    {
                        duration = (DateTime.UtcNow - returnRequest.CreatedAt).Days > 0 ? $"{(DateTime.UtcNow - returnRequest.CreatedAt).Days} days" : "Less than a day";
                    }

                    return new RequestHistoryDto
                    {
                        Id = returnRequest.Id,
                        Date = returnRequest.CreatedAt,
                        RequestType = "Return Request",
                        ReturnStatus = (ReturnRequestStatus)Enum.Parse(typeof(ReturnRequestStatus), returnRequest.Status),
                        LaptopDetails = laptopDetails,
                        Reason = returnRequest.Reason,
                        Notes = returnRequest.Reason,
                        Duration = duration
                    };
                }
            }

            throw new InvalidOperationException("History item not found or does not belong to the current user.");
        }

        public async Task<byte[]> ExportEmployeeHistoryAsync(Guid employeeId, HistoryFilterDto filter)
        {
            // Retrieve all history items (non-paginated) based on the filter
            var historyItems = (await GetEmployeeHistoryAsync(employeeId, new HistoryFilterDto // Use a new filter to get all items
            {
                PageNumber = 1,
                PageSize = int.MaxValue, // Get all items
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                Status = filter.Status,
                RequestType = filter.RequestType
            })).Items.ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Requisition History");

                // Add headers
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Date";
                worksheet.Cell(1, 3).Value = "Request Type";
                worksheet.Cell(1, 4).Value = "Status";
                worksheet.Cell(1, 5).Value = "Return Status";
                worksheet.Cell(1, 6).Value = "Laptop Details";
                worksheet.Cell(1, 7).Value = "Purpose";
                worksheet.Cell(1, 8).Value = "Reason";
                worksheet.Cell(1, 9).Value = "Duration";
                worksheet.Cell(1, 10).Value = "Notes";

                // Add data
                for (int i = 0; i < historyItems.Count; i++)
                {
                    var item = historyItems[i];
                    int row = i + 2; // Start from row 2 for data

                    worksheet.Cell(row, 1).Value = item.Id.ToString();
                    worksheet.Cell(row, 2).Value = item.Date.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cell(row, 3).Value = item.RequestType;
                    worksheet.Cell(row, 4).Value = item.Status?.ToString();
                    worksheet.Cell(row, 5).Value = item.ReturnStatus?.ToString();
                    worksheet.Cell(row, 6).Value = item.LaptopDetails;
                    worksheet.Cell(row, 7).Value = item.Purpose;
                    worksheet.Cell(row, 8).Value = item.Reason;
                    worksheet.Cell(row, 9).Value = item.Duration;
                    worksheet.Cell(row, 10).Value = item.Notes;
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


        private RequestResponseDto Map(Request request)
        {
            // Employee and Laptop are already included in GetByIdAsync and GetEmployeeRequestsAsync
            var employee = request.Employee; // Access directly
            var laptop = request.Laptop;     // Access directly

            return new RequestResponseDto
            {
                Id = request.Id,
                EmployeeId = request.EmployeeId,
                EmployeeName = employee?.FullName,
                Status = request.Status,
                Purpose = request.Purpose,
                PreferredSpecs = request.PreferredSpecs,
                IsSwapRequest = request.IsSwapRequest,
                RejectionReason = request.RejectionReason,
                LaptopId = request.LaptopId,
                LaptopName = laptop?.SerialNumber,
                IsReceiptConfirmed = request.IsReceiptConfirmed,
                CreatedAt = request.CreatedAt,
                ApprovedRejectedAt = request.ApprovedRejectedAt,
                AssignedAt = request.AssignedAt,
                ReceiptConfirmedAt = request.ReceiptConfirmedAt,
                AlternativeDeviceNote = request.AlternativeDeviceNote // Added
            };
        }

        private async Task<string> BuildRequestConfirmationEmailBodyAsync(string employeeName, string laptopModel, string requestStatus)
        {
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "RequestConfirmation.html");
            if (!File.Exists(templatePath))
            {
                return $"Dear {employeeName},\n\nYour laptop request for {laptopModel} has been submitted successfully. Status: {requestStatus}.\n\nBest regards,\nLRS Team";
            }
            var body = await File.ReadAllTextAsync(templatePath);
            return body
                .Replace("{{employeeName}}", employeeName)
                .Replace("{{laptopModel}}", laptopModel)
                .Replace("{{requestStatus}}", requestStatus);
        }

        public async Task ReportIssueAsync(Guid employeeId, ReportIssueDto dto)
        {
            // 1. Validate that the laptop is assigned to this employee
            var laptop = await _laptopRepository.GetByIdAsync(dto.LaptopId);
            if (laptop == null || laptop.AssignedToEmployeeId != employeeId)
            {
                throw new InvalidOperationException("Laptop not found or not assigned to the current employee.");
            }

            // 2. Create a notification for the admin/IT team
            // In a real system, you'd have an Admin user ID or a dedicated 'Issue' entity and notifying IT.

            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }

            string notificationMessage = $"Issue reported for your laptop ({laptop.SerialNumber}): '{dto.Description}'. IT has been notified. Contact preference: {dto.ContactPreference ?? "Not specified"}.";
            await _notificationService.CreateNotificationAsync(employeeId, notificationMessage);

            // Optionally, send an email to IT support
            string itEmail = "it-support@digitvant.com"; // Replace with actual IT support email
            string itNotificationSubject = $"New Laptop Issue Reported by {employee.FullName} ({employee.StaffId})";
            string itNotificationMessage = $"Employee: {employee.FullName} ({employee.StaffId})\n" +
                                           $"Email: {employee.Email}\n" +
                                           $"Phone: {employee.PhoneNumber}\n" +
                                           $"Laptop: {laptop.Brand} {laptop.Model} (SN: {laptop.SerialNumber})\n" +
                                           $"Issue: {dto.Description}\n" +
                                           $"Contact Preference: {dto.ContactPreference ?? "Not specified"}";

            var notificationRequest = new NotificationRequest
            {
                Channels = new List<string> { "Email" },
                From = _notificationApiSettings.FromEmail,
                To = itEmail,
                Subject = itNotificationSubject,
                Message = itNotificationMessage
            };

            var notificationResponse = await _notificationApi.SendNotificationAsync(notificationRequest);

            if (!notificationResponse.IsSuccessStatusCode || notificationResponse.Content is null || !notificationResponse.Content.IsSuccessful)
            {
                Console.WriteLine($"Warning: Failed to send IT issue report email: {notificationResponse.Error?.Content}");
            }
        }

        public async Task<PaginatedResultDto<RequestResponseDto>> GetFilteredAndPaginatedRequestsForAdminAsync(AdminRequestFilterDto filter)
        {
            var paginatedRequests = await _requestRepository.GetFilteredAndPaginatedRequestsAsync(filter);

            var mappedItems = paginatedRequests.Items.Select(request => Map(request)).ToList();

            return new PaginatedResultDto<RequestResponseDto>
            {
                Items = mappedItems,
                TotalCount = paginatedRequests.TotalCount,
                PageNumber = paginatedRequests.PageNumber,
                PageSize = paginatedRequests.PageSize
            };
        }

        public async Task<byte[]> ExportFilteredRequestsForAdminAsync(AdminRequestFilterDto filter)
        {
            // Retrieve all filtered requests (no pagination for export)
            var allFilteredRequests = (await _requestRepository.GetFilteredAndPaginatedRequestsAsync(new AdminRequestFilterDto
            {
                SearchTerm = filter.SearchTerm,
                Status = filter.Status,
                EmployeeId = filter.EmployeeId,
                DepartmentId = filter.DepartmentId,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                SortBy = filter.SortBy,
                SortOrder = filter.SortOrder,
                PageNumber = 1, // Get all pages
                PageSize = int.MaxValue // Get all items
            })).Items.ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Admin Laptop Requests");

                // Add headers
                worksheet.Cell(1, 1).Value = "Request ID";
                worksheet.Cell(1, 2).Value = "Employee Name";
                worksheet.Cell(1, 3).Value = "Employee Email";
                worksheet.Cell(1, 4).Value = "Department";
                worksheet.Cell(1, 5).Value = "Purpose";
                worksheet.Cell(1, 6).Value = "Preferred Specs";
                worksheet.Cell(1, 7).Value = "Status";
                worksheet.Cell(1, 8).Value = "Laptop Serial Number";
                worksheet.Cell(1, 9).Value = "Assigned At";
                worksheet.Cell(1, 10).Value = "Created At";
                worksheet.Cell(1, 11).Value = "Approved/Rejected At";
                worksheet.Cell(1, 12).Value = "Rejection Reason";
                worksheet.Cell(1, 13).Value = "Alternative Device Note";

                
                for (int i = 0; i < allFilteredRequests.Count(); i++)
                {
                    var request = allFilteredRequests.ElementAt(i);
                    int row = i + 2; 

                    worksheet.Cell(row, 1).Value = request.Id.ToString();
                    worksheet.Cell(row, 2).Value = request.Employee?.FullName;
                    worksheet.Cell(row, 3).Value = request.Employee?.Email;
                    worksheet.Cell(row, 4).Value = request.Employee?.Department?.Name;
                    worksheet.Cell(row, 5).Value = request.Purpose;
                    worksheet.Cell(row, 6).Value = request.PreferredSpecs;
                    worksheet.Cell(row, 7).Value = request.Status.ToString();
                    worksheet.Cell(row, 8).Value = request.Laptop?.SerialNumber;
                    worksheet.Cell(row, 9).Value = request.AssignedAt?.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cell(row, 10).Value = request.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cell(row, 11).Value = request.ApprovedRejectedAt?.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cell(row, 12).Value = request.RejectionReason;
                    worksheet.Cell(row, 13).Value = request.AlternativeDeviceNote;
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