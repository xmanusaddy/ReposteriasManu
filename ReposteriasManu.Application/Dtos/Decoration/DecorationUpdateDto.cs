using System.ComponentModel.DataAnnotations;

namespace ReposteriasManu.Application.Dtos.Decoration
{
    public class DecorationUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Type { get; set; }

        [MaxLength(50)]
        public string Color { get; set; }

        [MaxLength(300)]
        public string Message { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }
    }
}