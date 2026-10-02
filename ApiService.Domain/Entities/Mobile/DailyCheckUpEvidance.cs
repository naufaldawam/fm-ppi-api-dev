using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Foto dokumentasi DCU.
    /// File fisik disimpan menggunakan IFileService dan metadata disimpan di database.
    /// </summary>
    public class DailyCheckUpEvidance : BaseEntity
    {
        [Required]
        public string DataDcuId { get; set; } = string.Empty;
        public DailyCheckUpEntity? DataDcu { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string GeneratedName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
