using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities.Mobile
{
    /// <summary>
    /// Detail item inspeksi kendaraan.
    /// Satu item bernilai OK/Tidak OK melalui IsOk.
    /// </summary>
    public class InspeksiKendaraanDetailEntity : BaseEntity
    {
        [Required]
        public string InspeksiKendaraanId { get; set; } = string.Empty;
        public InspeksiKendaraanEntity? InspeksiKendaraan { get; set; }

        [Required]
        [MaxLength(100)]
        public string Kategori { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string NamaPemeriksaan { get; set; } = string.Empty;

        public bool IsOk { get; set; }

        [MaxLength(500)]
        public string? Keterangan { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
