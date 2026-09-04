using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Data Master > Driver. Terhubung ke Vendor (perusahaan penyedia driver)
    /// dan Atasan (Pekerja yang menjadi penanggung jawab/supervisor driver ini).
    /// </summary>
    public class Driver : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string NoPekerja { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string NamaDriver { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string NoHp { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string VendorId { get; set; } = string.Empty;
        public MasterVendor? Vendor { get; set; }

        [Required]
        public string AtasanId { get; set; } = string.Empty;
        public Pekerja? Atasan { get; set; }

        public bool IsActive { get; set; } = true;
    }
}