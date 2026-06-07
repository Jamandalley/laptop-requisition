using LaptopRequisition.Application.DTOs.Admin.Reports;
using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaptopRequisition.Domain.Common; // NEW: Added for Response<T>
using LaptopRequisition.Application.DTOs.Request; // NEW: Added for RequestFilterDto
using LaptopRequisition.Application.DTOs.Admin; // NEW: Added for AdminReturnRequestFilterDto

namespace LaptopRequisition.Application.Services
{
    public class AdminReportingService : IAdminReportingService
    {
        private readonly ILaptopRepository _laptopRepository;
        private readonly IRequestRepository _requestRepository;
        private readonly IReturnRequestRepository _returnRequestRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILaptopAssignmentRepository _laptopAssignmentRepository; // NEW: Inject LaptopAssignmentRepository

        public AdminReportingService(ILaptopRepository laptopRepository,
                                     IRequestRepository requestRepository,
                                     IReturnRequestRepository returnRequestRepository,
                                     IEmployeeRepository employeeRepository,
                                     ILaptopAssignmentRepository laptopAssignmentRepository) // NEW: Inject LaptopAssignmentRepository
        {
            _laptopRepository = laptopRepository;
            _requestRepository = requestRepository;
            _returnRequestRepository = returnRequestRepository;
            _employeeRepository = employeeRepository;
            _laptopAssignmentRepository = laptopAssignmentRepository; // NEW: Initialize LaptopAssignmentRepository
        }

        public async Task<Response<LaptopUtilizationReportDto>> GetLaptopUtilizationReportAsync()
        {
            try
            {
                var totalLaptops = await _laptopRepository.CountAllAsync();
                var inRepairLaptops = await _laptopRepository.CountByStatusAsync(LaptopStatus.UnderRepair); 

                // FIX: Get assigned laptops count from LaptopAssignments
                var assignedLaptops = (await _laptopAssignmentRepository.GetAllAsync()).Count();
                
                // FIX: Calculate available laptops based on total, assigned, and in repair
                var availableLaptops = totalLaptops - assignedLaptops - inRepairLaptops;

                return Response<LaptopUtilizationReportDto>.Ok(new LaptopUtilizationReportDto
                {
                    TotalLaptops = totalLaptops,
                    AssignedLaptops = assignedLaptops,
                    AvailableLaptops = availableLaptops,
                    InRepairLaptops = inRepairLaptops
                });
            }
            catch (Exception ex)
            {
                return Response<LaptopUtilizationReportDto>.Fail(ResponseCode.ServerError, new List<string> { $"Error generating laptop utilization report: {ex.Message}" });
            }
        }

        public async Task<Response<IEnumerable<RequestTrendReportDto>>> GetRequestTrendReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // FIX: Use GetFilteredAndPaginatedRequestsAsync for requests
                var requestFilter = new RequestFilterDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    PageNumber = 1,
                    PageSize = int.MaxValue // Get all for reporting
                };
                var requestsPaginated = await _requestRepository.GetFilteredAndPaginatedRequestsAsync(requestFilter);
                var requests = requestsPaginated.Items;

                // FIX: Use GetFilteredAndPaginatedReturnRequestsAsync for return requests
                var returnRequestFilter = new AdminReturnRequestFilterDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    PageNumber = 1,
                    PageSize = int.MaxValue // Get all for reporting
                };
                var returnRequestsPaginated = await _returnRequestRepository.GetFilteredAndPaginatedReturnRequestsAsync(returnRequestFilter);
                var returnRequests = returnRequestsPaginated.Items;

                var reportData = new List<RequestTrendReportDto>();

                for (DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    var newRequests = requests.Count(r => r.CreatedAt.Date == date.Date && r.Status == RequestStatus.Pending);
                    var approvedRequests = requests.Count(r => r.ApprovedRejectedAt?.Date == date.Date && r.Status == RequestStatus.Approved);
                    var rejectedRequests = requests.Count(r => r.ApprovedRejectedAt?.Date == date.Date && r.Status == RequestStatus.Rejected);
                    var completedRequests = requests.Count(r => r.ReceiptConfirmedAt?.Date == date.Date && r.Status == RequestStatus.Completed);

                    reportData.Add(new RequestTrendReportDto
                    {
                        Date = date,
                        NewRequests = newRequests,
                        ApprovedRequests = approvedRequests,
                        RejectedRequests = rejectedRequests,
                        CompletedRequests = completedRequests
                    });
                }

                return Response<IEnumerable<RequestTrendReportDto>>.Ok(reportData);
            }
            catch (Exception ex)
            {
                return Response<IEnumerable<RequestTrendReportDto>>.Fail(ResponseCode.ServerError, new List<string> { $"Error generating request trend report: {ex.Message}" });
            }
        }

        public async Task<Response<IEnumerable<EmployeeActivityReportDto>>> GetEmployeeActivityReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var employees = await _employeeRepository.GetAllWithDepartmentAndRoleAsync(); // Get all employees with their details

                // FIX: Use GetFilteredAndPaginatedRequestsAsync for requests
                var requestFilter = new RequestFilterDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    PageNumber = 1,
                    PageSize = int.MaxValue // Get all for reporting
                };
                var requestsPaginated = await _requestRepository.GetFilteredAndPaginatedRequestsAsync(requestFilter);
                var requests = requestsPaginated.Items;

                // FIX: Use GetFilteredAndPaginatedReturnRequestsAsync for return requests
                var returnRequestFilter = new AdminReturnRequestFilterDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    PageNumber = 1,
                    PageSize = int.MaxValue // Get all for reporting
                };
                var returnRequestsPaginated = await _returnRequestRepository.GetFilteredAndPaginatedReturnRequestsAsync(returnRequestFilter);
                var returnRequests = returnRequestsPaginated.Items;

                // FIX: Get all laptop assignments to efficiently count per employee
                var allLaptopAssignments = await _laptopAssignmentRepository.GetAllAsync();

                var reportData = new List<EmployeeActivityReportDto>();

                foreach (var employee in employees)
                {
                    var employeeRequests = requests.Where(r => r.EmployeeId == employee.Id && r.CreatedAt >= startDate && r.CreatedAt <= endDate).ToList();
                    var employeeReturnRequests = returnRequests.Where(rr => rr.EmployeeId == employee.Id && rr.CreatedAt >= startDate && rr.CreatedAt <= endDate).ToList();
                    
                    // FIX: Count assigned laptops using LaptopAssignments
                    var assignedLaptopsCount = allLaptopAssignments.Count(la => la.EmployeeId == employee.Id);

                    reportData.Add(new EmployeeActivityReportDto
                    {
                        EmployeeId = employee.Id,
                        EmployeeName = employee.FullName,
                        StaffId = employee.StaffId,
                        TotalRequests = employeeRequests.Count,
                        ApprovedRequests = employeeRequests.Count(r => r.Status == RequestStatus.Approved),
                        RejectedRequests = employeeRequests.Count(r => r.Status == RequestStatus.Rejected),
                        TotalReturnRequests = employeeReturnRequests.Count,
                        ApprovedReturnRequests = employeeReturnRequests.Count(rr => rr.Status == ReturnRequestStatus.Approved.ToString()),
                        RejectedReturnRequests = employeeReturnRequests.Count(rr => rr.Status == ReturnRequestStatus.Rejected.ToString()),
                        AssignedLaptopsCount = assignedLaptopsCount
                    });
                }

                return Response<IEnumerable<EmployeeActivityReportDto>>.Ok(reportData.OrderBy(e => e.EmployeeName));
            }
            catch (Exception ex)
            {
                return Response<IEnumerable<EmployeeActivityReportDto>>.Fail(ResponseCode.ServerError, new List<string> { $"Error generating employee activity report: {ex.Message}" });
            }
        }
    }
}