using System;
using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Entity utama laporan kecelakaan (Data Kecelakaan).
    /// Groep field:
    ///  - Info utama : Judul, KategoriId, TanggalKejadian, WaktuKejadian, Dampak
    ///  - Referenties: DriverId, PejabatId, Alamat, Detail/Penyebab/Bagaimana
    ///  - Rich text  : AkarPermasalahan, TindakanSegara, TindakanPerbaikan (nvarchar max)
    ///  - Status     : Draft / Published (default Draft)
    ///  - Nomor      : auto-generated server-side (nantinya)
    /// </summary>
    public class DataKecelakaan : BaseEntity
    {
        // ================= INFO UTAMA =================
        [Required]
        [MaxLength(500)]
        public string Judul { get; set; } = string.Empty;

        [Required]
        public string KategoriId { get; set; } = string.Empty;
        public MasterKategoriKecelakaan? Kategori { get; set; }

        [Required]
        public string PeriodeId { get; set; } = string.Empty;
        public Periode? Periode { get; set; }

        [Required]
        public DateTime TanggalKejadian { get; set; }

        /// <summary>Waktu kejadian, format "HH:mm" (dikirim user, bukan TimeSpan).</summary>
        [Required]
        [MaxLength(5)]
        public string WaktuKejadian { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Dampak { get; set; } = string.Empty;

        // ================= REFERENTIES + TEKS =================
        /// <summary>FK ke Driver</summary>
        public string? DriverId { get; set; }
        public Driver? Driver { get; set; }

        /// <summary>FK ke Pekerja (pejabat/supervisor)</summary>
        public string? PejabatId { get; set; }
        public Pekerja? Pejabat { get; set; }

        [MaxLength(500)]
        public string Alamat { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string DetailKejadian { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string PenyebabKejadian { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string BagaimanaTerjadinya { get; set; } = string.Empty;

        // ================= RICH TEXT (nvarchar max) =================
        public string AkarPermasalahan { get; set; } = string.Empty;
        public string TindakanSegara { get; set; } = string.Empty;
        public string TindakanPerbaikan { get; set; } = string.Empty;

        // ================= STATUS + NOMOR =================
        public const string StatusDraft = "Draft";
        public const string StatusPublished = "Published";

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = StatusDraft;

        /// <summary>Nomor laporan - diinput OLEH USER (bukan auto-generated).</summary>
        [Required]
        [MaxLength(50)]
        public string Nomor { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}