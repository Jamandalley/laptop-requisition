using System.ComponentModel.DataAnnotations;

namespace LaptopRequisition.Application.DTOs
{
    public class RequestPasswordResetDto
    {
        [Required]
        public string EmployeeId { get; set; }
    }
}