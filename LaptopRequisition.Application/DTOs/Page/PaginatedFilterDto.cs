namespace LaptopRequisition.Application.DTOs.Page
{
    public abstract class PaginatedFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? SearchTerm { get; set; } // NEW: Added SearchTerm property
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
    }
}