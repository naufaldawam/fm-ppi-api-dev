using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    public class MasterVendor : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}