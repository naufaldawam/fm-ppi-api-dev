using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// EXAMPLE ENTITY - Copy this pattern for any new resource.
    /// Rename "Product" to your entity name (e.g. Order, Invoice, etc.)
    /// </summary>
    public class Product : BaseEntity
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int Stock { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string? Category { get; set; }

        // Track who created/modified (from JWT claim)
        public string? OwnerId { get; set; }
    }
}
