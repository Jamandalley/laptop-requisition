namespace LaptopRequisition.Application.DTOs.Admin
{
    public class AdminDashboardSummaryDto
    {
        public int TotalStaff { get; set; }
        public int TotalLaptops { get; set; }
        public int AvailableLaptops { get; set; }
        public int PendingRequests { get; set; }
        
        // Historical metrics for % growth
        public int TotalStaffLastMonth { get; set; }
        public int TotalLaptopsLastMonth { get; set; }
        public int AvailableLaptopsLastMonth { get; set; }
    }
}