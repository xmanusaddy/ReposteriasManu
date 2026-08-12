using System.ComponentModel.DataAnnotations;

namespace ReposteriasManu.Application.Dtos.Product
{
    public class ProductCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(300)]
        public string Description { get; set; }

        [Required]
        [Range(0.01, 99999.99)]
        public decimal Price { get; set; }

        [MaxLength(100)]
        public string Flavor { get; set; }

        [MaxLength(50)]
        public string Size { get; set; }
    }
}