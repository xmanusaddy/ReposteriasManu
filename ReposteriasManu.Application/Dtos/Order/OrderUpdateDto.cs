using System.ComponentModel.DataAnnotations;

namespace ReposteriasManu.Application.Dtos.Order
{
    public class OrderUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public DateTime DeliveryDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        [Required]
        public int CustomerId { get; set; }
    }
}