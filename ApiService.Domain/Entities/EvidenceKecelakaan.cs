using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Foto bukti laporan kecelakaan - 1 DataKecelakaan punya N foto ("+ Tambahkan Foto").
    /// URL akses foto: {BaseUrl}/{FmImagesFolder}/{GeneratedName} (dibuild saat MapToDto).
    /// SortOrder bepaal volgorde foto di form.
    /// </summary>
    public class EvidenceKecelakaan : BaseEntity
    {
        [Required]
        public string DataKecelakaanId { get; set; } = string.Empty;
        public DataKecelakaan? DataKecelakaan { get; set; }

        /// <summary>Nama file asli user (display tanpa URL).</summary>
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>{GUID}_{namaAsli} - dipakai untuk URL foto. Kunci server-side.</summary>
        [Required]
        [MaxLength(255)]
        public string GeneratedName { get; set; } = string.Empty;

        /// <summary>Path file storage ({UploadPath}/{ImageFolder}/...).</summary>
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        /// <summary>Order foto di form (1..N, asc).</summary>
        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}