using LaptopRequisition.Application.DTOs.Dashboard;
using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;
using LaptopRequisition.Application.DTOs.Notification; 
using LaptopRequisition.Domain; 

namespace LaptopRequisition.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IRequestRepository _requestRepository;
        private readonly ILaptopRepository _laptopRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IReturnRequestRepository _returnRequestRepository; // Added
        private readonly ILaptopAssignmentRepository _laptopAssignmentRepository; // NEW: Inject LaptopAssignmentRepository

        public DashboardService(IEmployeeRepository employeeRepository,
                                IRequestRepository requestRepository,
                                ILaptopRepository laptopRepository,
                                INotificationRepository notificationRepository,
                                IReturnRequestRepository returnRequestRepository,
                                ILaptopAssignmentRepository laptopAssignmentRepository) // NEW: Inject LaptopAssignmentRepository
        {
            _employeeRepository = employeeRepository;
            _requestRepository = requestRepository;
            _laptopRepository = laptopRepository;
            _notificationRepository = notificationRepository;
            _returnRequestRepository = returnRequestRepository; 
            _laptopAssignmentRepository = laptopAssignmentRepository; // NEW: Initialize LaptopAssignmentRepository
        }

        public async Task<DashboardSummaryDto> GetEmployeeDashboardSummaryAsync(Guid employeeId)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }

            var totalRequests = await _requestRepository.CountByEmployeeIdAsync(employeeId);
            // FIX: Get current laptop assignment from LaptopAssignmentRepository
            var currentAssignment = await _laptopAssignmentRepository.GetCurrentAssignmentForEmployeeAsync(employeeId);
            var currentLaptop = currentAssignment?.Laptop; // Extract the laptop from the assignment

            var currentActiveRequest = await _requestRepository.GetPendingOrApprovedRequestByEmployeeIdAsync(employeeId);
            var unreadNotificationsCount = await _notificationRepository.CountUnreadByEmployeeIdAsync(employeeId);
            var recentNotifications = (await _notificationRepository.GetRecentNotificationsByEmployeeIdAsync(employeeId, 4))
                                        .Select(n => new NotificationResponseDto
                                        {
                                            Id = n.Id,
                                            EmployeeId = n.EmployeeId,
                                            Message = n.Message,
                                            IsRead = n.IsRead,
                                            CreatedAt = n.CreatedAt
                                        }).ToList();

            var currentLaptopDto = currentLaptop != null ? new CurrentLaptopDetailsDto
            {
                Id = currentLaptop.Id,
                AssetTag = currentLaptop.AssetTag,
                Brand = currentLaptop.Brand,
                Model = currentLaptop.Model,
                SerialNumber = currentLaptop.SerialNumber,
                Processor = currentLaptop.Processor,
                RAM = currentLaptop.RAM,
                Storage = currentLaptop.Storage,
                OperatingSystem = currentLaptop.OperatingSystem.ToString(), 
                ScreenSize = currentLaptop.ScreenSize,
                AssignedDate = currentAssignment?.AssignedDate ?? DateTime.MinValue // FIX: Use AssignedDate from LaptopAssignment
            } : null;

            var currentRequestStatusDto = new RequestStatusSummaryDto
            {
                HasActiveRequest = currentActiveRequest != null,
                RequestId = currentActiveRequest?.Id,
                Status = currentActiveRequest?.Status ?? RequestStatus.None,
                Purpose = currentActiveRequest?.Purpose,
                RejectionReason = currentActiveRequest?.RejectionReason,
                LastUpdate = currentActiveRequest?.UpdatedAt,
                IsReceiptConfirmationPending = currentActiveRequest != null && currentActiveRequest.Status == RequestStatus.Assigned && !currentActiveRequest.IsReceiptConfirmed
            };

            // Check for pending return requests
            var pendingReturnRequest = await _returnRequestRepository.GetPendingReturnRequestByEmployeeIdAsync(employeeId);

            return new DashboardSummaryDto
            {
                TotalRequests = totalRequests,
                CurrentLaptop = currentLaptopDto,
                CurrentRequestStatus = currentRequestStatusDto,
                UnreadNotificationsCount = unreadNotificationsCount,
                RecentNotifications = recentNotifications,
                IsReturnRequestPending = pendingReturnRequest != null
            };
        }
    }
}