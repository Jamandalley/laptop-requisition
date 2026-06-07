using LaptopRequisition.Application.DTOs.Page;
using LaptopRequisition.Domain.Enums;
using System;

namespace LaptopRequisition.Application.DTOs.Request
{
    public class RequestFilterDto : PaginatedFilterDto
    {
        public RequestStatus? Status { get; set; }
        public Guid? EmployeeId { get; set; }
        public bool? IsSwapRequest { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}