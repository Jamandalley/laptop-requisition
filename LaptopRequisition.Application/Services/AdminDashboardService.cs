using LaptopRequisition.Application.DTOs.Admin;
using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Domain.Enums; // For RequestStatus
using System.Threading.Tasks;

namespace LaptopRequisition.Application.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILaptopRepository _laptopRepository;
        private readonly IRequestRepository _requestRepository;

        public AdminDashboardService(IEmployeeRepository employeeRepository,
                                     ILaptopRepository laptopRepository,
                                     IRequestRepository requestRepository)
        {
            _employeeRepository = employeeRepository;
            _laptopRepository = laptopRepository;
            _requestRepository = requestRepository;
        }

        public async Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync()
        {
            var totalStaff = await _employeeRepository.CountAllAsync(); // Assuming this method exists
            var totalLaptops = await _laptopRepository.CountAllAsync(); // Assuming this method exists
            var availableLaptops = await _laptopRepository.CountAvailableAsync(); // Assuming this method exists
            var pendingRequests = await _requestRepository.CountByStatusAsync(RequestStatus.Pending); // Assuming this method exists

            var thirtyDaysAgo = System.DateTime.UtcNow.AddDays(-30);
            var totalStaffLastMonth = await _employeeRepository.CountUpToDateAsync(thirtyDaysAgo);
            var totalLaptopsLastMonth = await _laptopRepository.CountUpToDateAsync(thirtyDaysAgo);
            var availableLaptopsLastMonth = await _laptopRepository.CountAvailableUpToDateAsync(thirtyDaysAgo);

            return new AdminDashboardSummaryDto
            {
                TotalStaff = totalStaff,
                TotalLaptops = totalLaptops,
                AvailableLaptops = availableLaptops,
                PendingRequests = pendingRequests,
                TotalStaffLastMonth = totalStaffLastMonth,
                TotalLaptopsLastMonth = totalLaptopsLastMonth,
                AvailableLaptopsLastMonth = availableLaptopsLastMonth
            };
        }
    }
}