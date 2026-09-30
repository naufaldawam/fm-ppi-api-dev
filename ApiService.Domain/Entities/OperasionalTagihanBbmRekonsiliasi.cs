using System;
using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Menyimpan data BBM dari Ritel dan hasil pencocokannya
    /// dengan pengajuan BBM dari Driver (BbmSubmission).
    /// Satu row = satu row data hasil upload Ritel.
    /// </summary>
    public class OperasionalTagihanBbmRekonsiliasi : BaseEntity
    {
        public const string StatusPending = "Pending";
        public const string StatusMatched = "Matched";
        public const string StatusMismatch = "Mismatch";
        public const string StatusNotMatched = "NotMatched";

        [Required]
        public string PeriodeId { get; set; } = string.Empty;
        public Periode? Periode { get; set; }

        // Referensi ke pengajuan BBM Driver setelah proses matching berhasil.
        public string? BbmSubmissionId { get; set; }
        public BbmSubmission? BbmSubmission { get; set; }

        // Data dari file / sumber Ritel.
        [Required]
        public DateTime TanggalRitel { get; set; }

        [MaxLength(100)]
        public string? NomorReferensiRitel { get; set; }

        [MaxLength(50)]
        public string? NoPekerjaRitel { get; set; }

        [Required]
        [MaxLength(20)]
        public string NomorPolisiRitel { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? NamaRitel { get; set; }

        [Required]
        public decimal JumlahBbmRitel { get; set; }

        [Required]
        public decimal NilaiRitel { get; set; }

        // Hasil pencocokan ke master / input Driver.
        public string? DriverId { get; set; }
        public Driver? Driver { get; set; }

        public string? KendaraanId { get; set; }
        public Kendaraan? Kendaraan { get; set; }

        // Snapshot nilai dari BbmSubmission saat matching.
        public decimal? JumlahBbmDriver { get; set; }
        public decimal? NilaiNotaDriver { get; set; }
        public DateTime? TanggalDriver { get; set; }

        [Required]
        [MaxLength(30)]
        public string StatusMatching { get; set; } = StatusPending;

        [MaxLength(500)]
        public string? MismatchReason { get; set; }

        [MaxLength(255)]
        public string? SourceFileName { get; set; }

        public int? SourceRowNumber { get; set; }

        public DateTime? MatchedAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
