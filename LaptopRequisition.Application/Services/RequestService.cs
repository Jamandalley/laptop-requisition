using LaptopRequisition.Application.DTOs.Request;
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
using ClosedXML.Excel;
using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.DTOs.Page;
using LaptopRequisition.Domain.Common; // NEW: Added for Response<T>

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
        private readonly ILaptopAssignmentRepository _laptopAssignmentRepository; // NEW: Inject LaptopAssignmentRepository

        public RequestService(
            IRequestRepository requestRepository,
            IEmployeeRepository employeeRepository,
            ILaptopRepository laptopRepository,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService,
            INotificationApi notificationApi,
            IOptions<NotificationApiSettings> notificationApiSettingsOptions,
            IReturnRequestRepository returnRequestRepository,
            ILaptopAssignmentRepository laptopAssignmentRepository) // NEW: Inject LaptopAssignmentRepository
        {
            _requestRepository = requestRepository;
            _employeeRepository = employeeRepository;
            _laptopRepository = laptopRepository;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
            _notificationApi = notificationApi;
            _notificationApiSettings = notificationApiSettingsOptions.Value;
            _returnRequestRepository = returnRequestRepository; // Initialized
            _laptopAssignmentRepository = laptopAssignmentRepository; // NEW: Initialize LaptopAssignmentRepository
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

        // NEW: Helper method to get AssignedDate from LaptopAssignments
        private async Task<DateTime?> GetAssignedAtForRequest(Request request)
        {
            if (request.EmployeeId.HasValue && request.LaptopId.HasValue)
            {
                var assignment = await _laptopAssignmentRepository.GetCurrentAssignmentForEmployeeAndLaptopAsync(request.EmployeeId.Value, request.LaptopId.Value);
                return assignment?.AssignedDate;
            }
            return null;
        }

        // =========================
        // CREATE REQUEST
        // =========================
        public async Task<Response<RequestResponseDto>> CreateRequestAsync(CreateRequestDto dto)
        {
            var employeeId = GetCurrentEmployeeId();

            var existingPending = await _requestRepository.GetPendingRequestByEmployeeIdAsync(employeeId);
            if (existingPending != null)
                return Response<RequestResponseDto>.Fail(
                    ResponseCode.BadRequest,
                    new List<string> { "You already have a pending request." });

            var request = new Request
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                Purpose = dto.Purpose,
                PreferredSpecs = dto.PreferredSpecs,
                IsSwapRequest = dto.IsSwapRequest,
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDismissed = false
            };

            await _requestRepository.AddAsync(request);

            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
            {
                return Response<RequestResponseDto>.Fail(ResponseCode.NotFound, new List<string> { "Employee not found for current user." });
            }

            await _notificationService.CreateNotificationAsync(employeeId, $"Your laptop request (ID: {request.Id.ToString().Substring(0, 8)}...) has been submitted successfully.");

            var emailBody = await BuildRequestConfirmationEmailBodyAsync(employee.FullName, "N/A", request.Status.ToString());
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
                return Response<RequestResponseDto>.Fail(ResponseCode.ServerError, new List<string> { $"Failed to send request confirmation email: {notificationResponse.Error?.Content}" });
            }

            // FIX: Re-fetch the request with Employee and Laptop navigation properties loaded
            var createdRequestWithDetails = await _requestRepository.GetByIdAsync(request.Id, includeRelatedEntities: true);
            if (createdRequestWithDetails == null)
            {
                return Response<RequestResponseDto>.Fail(ResponseCode.NotFound, new List<string> { "Created request not found after adding." });
            }

            return Response<RequestResponseDto>.Ok(await Map(createdRequestWithDetails)); // FIX: Await Map with re-fetched request
        }

        // =========================
        // GET REQUEST BY ID
        // =========================
        public async Task<Response<RequestResponseDto>> GetRequestByIdAsync(Guid id)
        {
            var request = await _requestRepository.GetByIdAsync(id, true);

            if (request == null)
                return Response<RequestResponseDto>.Fail(ResponseCode.NotFound, new List<string> { "Request not found." });

            return Response<RequestResponseDto>.Ok(await Map(request)); // FIX: Await Map
        }

        // =========================
        // EMPLOYEE REQUESTS
        // =========================
        public async Task<Response<PaginatedResultDto<RequestResponseDto>>> GetEmployeeRequestsAsync(Guid employeeId, RequestFilterDto filter)
        {
            var result = await _requestRepository.GetByEmployeeIdAsync(employeeId, filter);

            var mappedItems = new List<RequestResponseDto>();
            foreach (var req in result.Items)
            {
                mappedItems.Add(await Map(req)); // FIX: Await Map
            }

            return Response<PaginatedResultDto<RequestResponseDto>>.Ok(new PaginatedResultDto<RequestResponseDto>
            {
                Items = mappedItems,
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            });
        }

        // =========================
        // GET ALL REQUESTS (ADMIN)
        // =========================
        public async Task<Response<PaginatedResultDto<RequestResponseDto>>> GetAllRequestsAsync(RequestFilterDto filter)
        {
            var paginatedRequests = await _requestRepository.GetFilteredAndPaginatedRequestsAsync(filter);

            var mappedItems = new List<RequestResponseDto>();
            foreach (var req in paginatedRequests.Items)
            {
                mappedItems.Add(await Map(req)); // FIX: Await Map
            }

            return Response<PaginatedResultDto<RequestResponseDto>>.Ok(new PaginatedResultDto<RequestResponseDto>
            {
                Items = mappedItems,
                TotalCount = paginatedRequests.TotalCount,
                PageNumber = paginatedRequests.PageNumber,
                PageSize = paginatedRequests.PageSize
            });
        }

        // =========================
        // APPROVE REQUEST
        // =========================
        public async Task<Response> ApproveRequestAsync(Guid requestId)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);

            if (request == null)
                return Response.Fail(ResponseCode.NotFound, new List<string> { "Request not found." });

            if (request.Status != RequestStatus.Pending)
                return Response.Fail(ResponseCode.BadRequest, new List<string> { "Only pending requests can be approved." });

            request.Status = RequestStatus.Approved;
            request.UpdatedAt = DateTime.UtcNow;
            request.ApprovedRejectedAt = DateTime.UtcNow;

            await _requestRepository.UpdateAsync(request);

            if (request.EmployeeId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(request.EmployeeId.Value, $"Your laptop request (ID: {request.Id.ToString().Substring(0, 8)}...) has been approved!");
            }
            return Response.Ok();
        }

        // =========================
        // REJECT REQUEST
        // =========================
        public async Task<Response> RejectRequestAsync(Guid requestId, string reason)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);

            if (request == null)
                return Response.Fail(ResponseCode.NotFound, new List<string> { "Request not found." });

            if (request.Status != RequestStatus.Pending)
                return Response.Fail(ResponseCode.BadRequest, new List<string> { "Only pending requests can be rejected." });

            request.Status = RequestStatus.Rejected;
            request.RejectionReason = reason;
            request.UpdatedAt = DateTime.UtcNow;
            request.ApprovedRejectedAt = DateTime.UtcNow;

            await _requestRepository.UpdateAsync(request);

            if (request.EmployeeId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(request.EmployeeId.Value, $"Your laptop request (ID: {request.Id.ToString().Substring(0, 8)}...) has been rejected. Reason: {reason}");
            }
            return Response.Ok();
        }

        // =========================
        // ASSIGN LAPTOP
        // =========================
        public async Task<Response> AssignLaptopAsync(Guid requestId, Guid laptopId)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);
            var laptop = await _laptopRepository.GetByIdAsync(laptopId);

            if (request == null)
                return Response.Fail(ResponseCode.NotFound, new List<string> { "Request not found." });

            if (laptop == null)
                return Response.Fail(ResponseCode.NotFound, new List<string> { "Laptop not found." });

            if (request.Status != RequestStatus.Approved)
                return Response.Fail(ResponseCode.BadRequest, new List<string> { "Only approved requests can be assigned." });

            var existingLaptopAssignment = await _laptopAssignmentRepository.GetCurrentAssignmentForLaptopAsync(laptopId);
            if (existingLaptopAssignment != null)
            {
                return Response.Fail(ResponseCode.Conflict, new List<string> { $"Laptop '{laptop.SerialNumber}' is already assigned to employee '{existingLaptopAssignment.Employee?.FullName}'." });
            }

            if (!request.EmployeeId.HasValue)
            {
                return Response.Fail(ResponseCode.BadRequest, new List<string> { "Request employee ID is missing." });
            }
            var existingEmployeeAssignment = await _laptopAssignmentRepository.GetCurrentAssignmentForEmployeeAsync(request.EmployeeId.Value);
            if (existingEmployeeAssignment != null)
            {
                return Response.Fail(ResponseCode.Conflict, new List<string> { $"Employee '{existingEmployeeAssignment.Employee?.FullName}' already has laptop '{existingEmployeeAssignment.Laptop?.SerialNumber}' assigned. Please unassign it first." });
            }

            string? alternativeNote = null;
            if (!string.IsNullOrEmpty(request.PreferredSpecs))
            {
                var preferredSpecsLower = request.PreferredSpecs.ToLower();
                var assignedLaptopDetails = $"{laptop.Brand} {laptop.Model} {laptop.Processor} {laptop.RAM} {laptop.Storage}".ToLower();

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
            }

            request.LaptopId = laptopId;
            request.Status = RequestStatus.Assigned;
            request.UpdatedAt = DateTime.UtcNow;
            request.AlternativeDeviceNote = alternativeNote;

            var newAssignment = new LaptopAssignments
            {
                EmployeeId = request.EmployeeId.Value,
                LaptopId = laptopId,
                AssignedDate = DateTime.UtcNow // This is the source of the AssignedAt date
            };
            await _laptopAssignmentRepository.AddAsync(newAssignment);

            laptop.Status = LaptopStatus.Assigned;
            await _laptopRepository.UpdateAsync(laptop);

            await _requestRepository.UpdateAsync(request);

            if (request.EmployeeId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(request.EmployeeId.Value, $"A laptop ({laptop.SerialNumber}) has been assigned to your request (ID: {request.Id.ToString().Substring(0, 8)}...). Please check your request status.");
            }
            return Response.Ok();
        }

        // =========================
        // GET EMPLOYEE REQUEST STATUS DETAIL
        // =========================
        public async Task<Response<RequestStatusDetailDto>> GetEmployeeRequestStatusDetailAsync(Guid employeeId)
        {
            var request = await _requestRepository.GetLatestRequestByEmployeeIdAsync(employeeId);

            if (request == null || request.IsDismissed)
            {
                return Response<RequestStatusDetailDto>.Ok(new RequestStatusDetailDto { HasActiveRequest = false });
            }

            var assignedLaptopDto = new AssignedLaptopDetailDto();
            DateTime? assignedDateFromAssignment = null; // To store the derived AssignedAt date

            if (request.LaptopId.HasValue)
            {
                // We need to fetch the LaptopAssignment for this request's laptop
                var assignment = await _laptopAssignmentRepository.GetCurrentAssignmentForEmployeeAndLaptopAsync(employeeId, request.LaptopId.Value); // FIX: Explicitly query
                assignedDateFromAssignment = assignment?.AssignedDate;

                var laptop = await _laptopRepository.GetByIdAsync(request.LaptopId.Value); // FIX: Explicitly fetch laptop
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
                        AssignedDate = assignedDateFromAssignment ?? DateTime.MinValue
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

            // FIX: Use assignedDateFromAssignment for timeline
            if (request.Status >= RequestStatus.Assigned && assignedDateFromAssignment.HasValue)
            {
                timeline.Add(new RequestTimelineEventDto { Status = RequestStatus.Assigned, Timestamp = assignedDateFromAssignment, Notes = $"Laptop Assigned: {assignedLaptopDto.Brand} {assignedLaptopDto.Model}" });
            }

            if (request.IsReceiptConfirmed && request.ReceiptConfirmedAt.HasValue)
            {
                timeline.Add(new RequestTimelineEventDto { Status = RequestStatus.Completed, Timestamp = request.ReceiptConfirmedAt, Notes = "Receipt Confirmed" });
            }

            return Response<RequestStatusDetailDto>.Ok(new RequestStatusDetailDto
            {
                RequestId = request.Id,
                DateSubmitted = request.CreatedAt,
                RequestedLaptopModel = request.PreferredSpecs,
                CurrentStatus = request.Status,
                Purpose = request.Purpose,
                PreferredSpecs = request.PreferredSpecs,
                RejectionReason = request.RejectionReason,
                IsReceiptConfirmed = request.IsReceiptConfirmed,
                ReceiptConfirmedAt = request.ReceiptConfirmedAt,
                AssignedLaptop = assignedLaptopDto.Id != Guid.Empty ? assignedLaptopDto : null,
                Timeline = timeline,
                IsDismissed = request.IsDismissed
            });
        }

        // =========================
        // DISMISS REJECTED REQUEST
        // =========================
        public async Task<Response> DismissRejectedRequestAsync(Guid requestId, Guid employeeId)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);

            if (request == null)
            {
                return Response.Fail(ResponseCode.NotFound, new List<string> { "Request not found." });
            }
            if (request.EmployeeId != employeeId)
            {
                return Response.Fail(ResponseCode.Forbidden, new List<string> { "Request does not belong to the current user." });
            }

            if (request.Status != RequestStatus.Rejected)
            {
                return Response.Fail(ResponseCode.BadRequest, new List<string> { "Only rejected requests can be dismissed." });
            }

            request.IsDismissed = true;
            request.UpdatedAt = DateTime.UtcNow;
            await _requestRepository.UpdateAsync(request);

            if (request.EmployeeId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(request.EmployeeId.Value, $"Your rejected request (ID: {request.Id.ToString().Substring(0, 8)}...) has been dismissed.");
            }
            return Response.Ok();
        }

        // =========================
        // CONFIRM RECEIPT
        // =========================
        public async Task<Response> ConfirmReceiptAsync(Guid requestId, Guid employeeId)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);

            if (request == null)
            {
                return Response.Fail(ResponseCode.NotFound, new List<string> { "Request not found." });
            }
            if (request.EmployeeId != employeeId)
            {
                return Response.Fail(ResponseCode.Forbidden, new List<string> { "Request does not belong to the current user." });
            }

            if (request.Status != RequestStatus.Assigned)
            {
                return Response.Fail(ResponseCode.BadRequest, new List<string> { "Only assigned requests can have receipt confirmed." });
            }

            if (request.IsReceiptConfirmed)
            {
                return Response.Fail(ResponseCode.BadRequest, new List<string> { "Receipt already confirmed for this request." });
            }

            request.IsReceiptConfirmed = true;
            request.ReceiptConfirmedAt = DateTime.UtcNow;
            request.Status = RequestStatus.Completed;
            request.UpdatedAt = DateTime.UtcNow;
            await _requestRepository.UpdateAsync(request);

            if (request.EmployeeId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(request.EmployeeId.Value, $"Receipt confirmed for your laptop request (ID: {request.Id.ToString().Substring(0, 8)}...).");
            }
            return Response.Ok();
        }

        // =========================
        // GET EMPLOYEE HISTORY
        // =========================
        public async Task<Response<PaginatedResultDto<RequestHistoryDto>>> GetEmployeeHistoryAsync(Guid employeeId, HistoryFilterDto filter)
        {
            var requestsPaginated = await _requestRepository.GetEmployeeRequestsAsync(employeeId, filter);
            var returnRequestsPaginated = await _returnRequestRepository.GetEmployeeReturnRequestsAsync(employeeId, filter);

            var combinedHistoryItems = new List<RequestHistoryDto>();

            foreach (var req in requestsPaginated.Items)
            {
                if (req.IsDismissed && filter.Status != RequestStatus.Rejected)
                {
                    continue;
                }

                string? laptopDetails = null;
                if (req.Laptop != null)
                {
                    laptopDetails = $"{req.Laptop.Brand} {req.Laptop.Model} (SN: {req.Laptop.SerialNumber})";
                }

                // Derive AssignedAt for RequestHistoryDto
                DateTime? assignedAt = null;
                if (req.EmployeeId.HasValue && req.LaptopId.HasValue)
                {
                    var assignment = await _laptopAssignmentRepository.GetCurrentAssignmentForEmployeeAndLaptopAsync(req.EmployeeId.Value, req.LaptopId.Value);
                    assignedAt = assignment?.AssignedDate;
                }

                combinedHistoryItems.Add(new RequestHistoryDto
                {
                    Id = req.Id,
                    Date = req.CreatedAt,
                    RequestType = "Laptop Request",
                    Status = req.Status,
                    LaptopDetails = laptopDetails,
                    Purpose = req.Purpose,
                    Notes = req.RejectionReason,
                    AssignedAt = assignedAt // Populate AssignedAt in History DTO
                });
            }

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
                    ReturnStatus = (ReturnRequestStatus)Enum.Parse(typeof(ReturnRequestStatus), retReq.Status),
                    LaptopDetails = laptopDetails,
                    Reason = retReq.Reason,
                    Notes = retReq.Reason
                });
            }

            if (!string.IsNullOrEmpty(filter.RequestType))
            {
                combinedHistoryItems = combinedHistoryItems
                    .Where(item => item.RequestType.Equals(filter.RequestType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            combinedHistoryItems = combinedHistoryItems.OrderByDescending(item => item.Date).ToList();

            var totalCount = combinedHistoryItems.Count;

            var paginatedItems = combinedHistoryItems
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            foreach (var item in paginatedItems)
            {
                if (item.Status.HasValue && item.Status == RequestStatus.Completed && item.Date != DateTime.MinValue)
                {
                    var request = await _requestRepository.GetByIdAsync(item.Id);
                    if (request != null && request.ReceiptConfirmedAt.HasValue)
                    {
                        item.Duration = (request.ReceiptConfirmedAt.Value - item.Date).Days > 0 ? $"{(request.ReceiptConfirmedAt.Value - item.Date).Days} days" : "Less than a day";
                    }
                }
                else if (item.ReturnStatus.HasValue && item.ReturnStatus == ReturnRequestStatus.Returned && item.Date != DateTime.MinValue)
                {
                    var returnRequest = await _returnRequestRepository.GetByIdAsync(item.Id);
                    if (returnRequest != null && returnRequest.ReturnedAt.HasValue)
                    {
                        item.Duration = (returnRequest.ReturnedAt.Value - item.Date).Days > 0 ? $"{(returnRequest.ReturnedAt.Value - item.Date).Days} days" : "Less than a day";
                    }
                }
                else if (item.Date != DateTime.MinValue)
                {
                    item.Duration = (DateTime.UtcNow - item.Date).Days > 0 ? $"{(DateTime.UtcNow - item.Date).Days} days" : "Less than a day";
                }
            }

            return Response<PaginatedResultDto<RequestHistoryDto>>.Ok(new PaginatedResultDto<RequestHistoryDto>
            {
                Items = paginatedItems,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            });
        }

        // =========================
        // GET HISTORY ITEM BY ID
        // =========================
        public async Task<Response<RequestHistoryDto>> GetHistoryItemByIdAsync(Guid id, Guid employeeId)
        {
            var request = await _requestRepository.GetRequestWithLaptopAndEmployeeAsync(id);
            if (request != null)
            {
                if (request.EmployeeId.HasValue && request.EmployeeId.Value == employeeId)
                {
                    string? laptopDetails = null;
                    if (request.Laptop != null)
                    {
                        laptopDetails = $"{request.Laptop.Brand} {request.Laptop.Model} (SN: {request.Laptop.SerialNumber})";
                    }

                    // Derive AssignedAt for RequestHistoryDto
                    DateTime? assignedAt = null;
                    if (request.EmployeeId.HasValue && request.LaptopId.HasValue)
                    {
                        var assignment = await _laptopAssignmentRepository.GetCurrentAssignmentForEmployeeAndLaptopAsync(request.EmployeeId.Value, request.LaptopId.Value);
                        assignedAt = assignment?.AssignedDate;
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

                    return Response<RequestHistoryDto>.Ok(new RequestHistoryDto
                    {
                        Id = request.Id,
                        Date = request.CreatedAt,
                        RequestType = "Laptop Request",
                        Status = request.Status,
                        LaptopDetails = laptopDetails,
                        Purpose = request.Purpose,
                        Notes = request.RejectionReason,
                        Duration = duration,
                        AssignedAt = assignedAt // Populate AssignedAt in History DTO
                    });
                }
            }

            var returnRequest = await _returnRequestRepository.GetReturnRequestWithLaptopAndEmployeeAsync(id);
            if (returnRequest != null)
            {
                if (returnRequest.EmployeeId.HasValue && returnRequest.EmployeeId.Value == employeeId)
                {
                    string? laptopDetails = null;
                    if (returnRequest.Laptop != null)
                    {
                        laptopDetails = $"{returnRequest.Laptop.Brand} {returnRequest.Laptop.Model} (SN: {returnRequest.Laptop.SerialNumber})";
                    }

                    string? duration = null;
                    if (returnRequest.Status == ReturnRequestStatus.Returned.ToString() && returnRequest.ReturnedAt.HasValue)
                    {
                        duration = (returnRequest.ReturnedAt.Value - returnRequest.CreatedAt).Days > 0 ? $"{(returnRequest.ReturnedAt.Value - returnRequest.CreatedAt).Days} days" : "Less than a day";
                    }
                    else
                    {
                        duration = (DateTime.UtcNow - returnRequest.CreatedAt).Days > 0 ? $"{(DateTime.UtcNow - returnRequest.CreatedAt).Days} days" : "Less than a day";
                    }

                    return Response<RequestHistoryDto>.Ok(new RequestHistoryDto
                    {
                        Id = returnRequest.Id,
                        Date = returnRequest.CreatedAt,
                        RequestType = "Return Request",
                        ReturnStatus = (ReturnRequestStatus)Enum.Parse(typeof(ReturnRequestStatus), returnRequest.Status),
                        LaptopDetails = laptopDetails,
                        Reason = returnRequest.Reason,
                        Notes = returnRequest.Reason,
                        Duration = duration
                    });
                }
            }

            return Response<RequestHistoryDto>.Fail(ResponseCode.NotFound, new List<string> { "History item not found or does not belong to the current user." });
        }

        // =========================
        // EXPORT EMPLOYEE HISTORY
        // =========================
        public async Task<Response<byte[]>> ExportEmployeeHistoryAsync(Guid employeeId, HistoryFilterDto filter)
        {
            var historyResponse = await GetEmployeeHistoryAsync(employeeId, new HistoryFilterDto
            {
                PageNumber = 1,
                PageSize = int.MaxValue,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                Status = filter.Status,
                RequestType = filter.RequestType
            });

            if (!historyResponse.IsSuccessful || historyResponse.Data == null)
            {
                return Response<byte[]>.Fail(historyResponse.Code, historyResponse.Errors);
            }

            var historyItems = historyResponse.Data.Items.ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Requisition History");

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
                worksheet.Cell(1, 11).Value = "Assigned At"; // Added Assigned At column

                for (int i = 0; i < historyItems.Count; i++)
                {
                    var item = historyItems[i];
                    int row = i + 2;

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
                    worksheet.Cell(row, 11).Value = item.AssignedAt?.ToString("yyyy-MM-dd HH:mm"); // Populate Assigned At
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return Response<byte[]>.Ok(stream.ToArray());
                }
            }
        }

        // =========================
        // REPORT ISSUE
        // =========================
        public async Task<Response> ReportIssueAsync(Guid employeeId, ReportIssueDto dto)
        {
            var laptop = await _laptopRepository.GetByIdAsync(dto.LaptopId);
            if (laptop == null)
            {
                return Response.Fail(ResponseCode.NotFound, new List<string> { "Laptop not found." });
            }

            var currentAssignment = await _laptopAssignmentRepository.GetCurrentAssignmentForLaptopAsync(dto.LaptopId);
            if (currentAssignment == null || currentAssignment.EmployeeId != employeeId)
            {
                return Response.Fail(ResponseCode.NotFound, new List<string> { "Laptop not found or not assigned to the current employee." });
            }

            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
            {
                return Response.Fail(ResponseCode.NotFound, new List<string> { "Employee not found." });
            }

            string notificationMessage = $"Issue reported for your laptop ({laptop.SerialNumber}): '{dto.Description}'. IT has been notified. Contact preference: {dto.ContactPreference ?? "Not specified"}.";
            await _notificationService.CreateNotificationAsync(employeeId, notificationMessage);

            string itEmail = "it-support@digitvant.com";
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
                return Response.Fail(ResponseCode.ServerError, new List<string> { $"Failed to send IT issue report email: {notificationResponse.Error?.Content}" });
            }
            return Response.Ok();
        }

        // =========================
        // GET FILTERED AND PAGINATED REQUESTS FOR ADMIN
        // =========================
        public async Task<Response<PaginatedResultDto<RequestResponseDto>>> GetFilteredAndPaginatedRequestsForAdminAsync(AdminRequestFilterDto filter)
        {
            var paginatedRequests = await _requestRepository.GetFilteredAndPaginatedRequestsForAdminAsync(filter);

            var mappedItems = new List<RequestResponseDto>();
            foreach (var req in paginatedRequests.Items)
            {
                mappedItems.Add(await Map(req)); // FIX: Await Map
            }

            return Response<PaginatedResultDto<RequestResponseDto>>.Ok(new PaginatedResultDto<RequestResponseDto>
            {
                Items = mappedItems,
                TotalCount = paginatedRequests.TotalCount,
                PageNumber = paginatedRequests.PageNumber,
                PageSize = paginatedRequests.PageSize
            });
        }

        // =========================
        // EXPORT FILTERED REQUESTS FOR ADMIN
        // =========================
        public async Task<Response<byte[]>> ExportFilteredRequestsForAdminAsync(AdminRequestFilterDto filter)
        {
            var allFilteredRequests = (await _requestRepository.GetFilteredAndPaginatedRequestsForAdminAsync(new AdminRequestFilterDto
            {
                PageNumber = 1,
                PageSize = int.MaxValue,
                SearchTerm = filter.SearchTerm,
                Status = filter.Status,
                EmployeeId = filter.EmployeeId,
                DepartmentId = filter.DepartmentId,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                SortBy = filter.SortBy,
                SortOrder = filter.SortOrder
            })).Items.ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Admin Laptop Requests");

                worksheet.Cell(1, 1).Value = "Request ID";
                worksheet.Cell(1, 2).Value = "Employee Name";
                worksheet.Cell(1, 3).Value = "Employee Email";
                worksheet.Cell(1, 4).Value = "Department";
                worksheet.Cell(1, 5).Value = "Purpose";
                worksheet.Cell(1, 6).Value = "Preferred Specs";
                worksheet.Cell(1, 7).Value = "Status";
                worksheet.Cell(1, 8).Value = "Laptop Serial Number";
                worksheet.Cell(1, 9).Value = "Assigned At"; // Added Assigned At column
                worksheet.Cell(1, 10).Value = "Created At";
                worksheet.Cell(1, 11).Value = "Approved/Rejected At";
                worksheet.Cell(1, 12).Value = "Rejection Reason";
                worksheet.Cell(1, 13).Value = "Alternative Device Note";

                foreach (var request in allFilteredRequests) // FIX: Changed loop to use request directly
                {
                    int row = allFilteredRequests.IndexOf(request) + 2; // Get current row index

                    // Derive AssignedAt for export
                    DateTime? assignedAt = null;
                    if (request.EmployeeId.HasValue && request.LaptopId.HasValue)
                    {
                        var assignment = await _laptopAssignmentRepository.GetCurrentAssignmentForEmployeeAndLaptopAsync(request.EmployeeId.Value, request.LaptopId.Value);
                        assignedAt = assignment?.AssignedDate;
                    }

                    worksheet.Cell(row, 1).Value = request.Id.ToString();
                    worksheet.Cell(row, 2).Value = request.Employee?.FullName;
                    worksheet.Cell(row, 3).Value = request.Employee?.Email;
                    worksheet.Cell(row, 4).Value = request.Employee?.Department?.Name;
                    worksheet.Cell(row, 5).Value = request.Purpose;
                    worksheet.Cell(row, 6).Value = request.PreferredSpecs;
                    worksheet.Cell(row, 7).Value = request.Status.ToString();
                    worksheet.Cell(row, 8).Value = request.Laptop?.SerialNumber;
                    worksheet.Cell(row, 9).Value = assignedAt?.ToString("yyyy-MM-dd HH:mm"); // Populate Assigned At
                    worksheet.Cell(row, 10).Value = request.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cell(row, 11).Value = request.ApprovedRejectedAt?.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cell(row, 12).Value = request.RejectionReason;
                    worksheet.Cell(row, 13).Value = request.AlternativeDeviceNote;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return Response<byte[]>.Ok(stream.ToArray());
                }
            }
        }

        // =========================
        // MAPPER (ONLY SOURCE OF TRUTH FOR DTO)
        // =========================
        private async Task<RequestResponseDto> Map(Request request) // FIX: Made async
        {
            // Derive AssignedAt from LaptopAssignments
            DateTime? assignedAt = null;
            if (request.EmployeeId.HasValue && request.LaptopId.HasValue)
            {
                var assignment = await _laptopAssignmentRepository.GetCurrentAssignmentForEmployeeAndLaptopAsync(request.EmployeeId.Value, request.LaptopId.Value);
                assignedAt = assignment?.AssignedDate;
            }

            return new RequestResponseDto
            {
                Id = request.Id,
                EmployeeId = request.EmployeeId,

                EmployeeName = request.Employee?.FullName ?? string.Empty,
                EmployeeEmail = request.Employee?.Email,
                DepartmentName = request.Employee?.Department?.Name,

                Status = request.Status,
                Purpose = request.Purpose,
                PreferredSpecs = request.PreferredSpecs,

                IsSwapRequest = request.IsSwapRequest,
                RejectionReason = request.RejectionReason,

                LaptopId = request.LaptopId,

                LaptopName = request.Laptop != null
                    ? $"{request.Laptop.Brand} {request.Laptop.Model}"
                    : null,

                IsReceiptConfirmed = request.IsReceiptConfirmed,

                CreatedAt = request.CreatedAt,
                ApprovedRejectedAt = request.ApprovedRejectedAt,
                // Derived AssignedAt for DTO
                AssignedAt = assignedAt,
                ReceiptConfirmedAt = request.ReceiptConfirmedAt,

                AlternativeDeviceNote = request.AlternativeDeviceNote
            };
        }

        private Task<string> BuildRequestConfirmationEmailBodyAsync(
            string employeeName,
            string laptopModel,
            string status)
        {
            var body = $@"
        Hello {employeeName},
        
        Your laptop request has been submitted successfully.
        
        Laptop: {laptopModel}
        Status: {status}
        
        Regards,
        IT Team";

            return Task.FromResult(body);
        }
    }
}