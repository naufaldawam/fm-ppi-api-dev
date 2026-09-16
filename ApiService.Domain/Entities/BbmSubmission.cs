using System;
using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Pengajuan penggunaan BBM oleh Driver, disubmit dari mobile app (wizard 4 langkah:
    /// Informasi Perjalanan -> Data Kendaraan -> Data BBM/Energi -> Foto &amp; Verifikasi).
    /// Butuh approval (oleh Atasan/VP driver ybs) sebelum ikut dihitung sebagai biaya
    /// operasional di modul OperasionalUpah.
    /// </summary>
    public class BbmSubmission : BaseEntity
    {
        public const string StatusPending = "Pending";
        public const string StatusApproved = "Approved";
        public const string StatusRejected = "Rejected";

        // ---------- Step 1: Informasi Perjalanan ----------

        /// <summary>Driver yang submit (bukan Pekerja - Driver adalah personel vendor).</summary>
        [Required]
        public string DriverId { get; set; } = string.Empty;
        public Driver? Driver { get; set; }

        /// <summary>
        /// Snapshot Atasan (VP) driver ini pada saat submit - dipakai untuk agregasi
        /// biaya ke OperasionalUpah milik VP tsb. Bisa null kalau Driver saat itu
        /// belum/sedang tidak punya Atasan (kekosongan jabatan atasan bisa sampai
        /// 3 bulan sesuai bisnis proses) - submission tetap bisa dibuat & di-approve,
        /// tapi belum ke-agregasi ke siapapun sampai ada admin yang assign manual.
        /// </summary>
        public string? AtasanPekerjaId { get; set; }
        public Pekerja? AtasanPekerja { get; set; }

        [Required]
        public string PeriodeId { get; set; } = string.Empty;
        public Periode? Periode { get; set; }

        [Required]
        public DateTime TanggalPenggunaan { get; set; }

        // ---------- Step 2: Data Kendaraan ----------
        [Required]
        public string KendaraanId { get; set; } = string.Empty;
        public Kendaraan? Kendaraan { get; set; }

        // ---------- Step 3: Data BBM / Energi ----------
        // Catatan: Jenis BBM TIDAK disimpan di sini - selalu diturunkan dari
        // Kendaraan.BahanBakar (read-only di form mobile, sesuai kendaraan yang dipilih).

        /// <summary>Jumlah pemakaian (liter/kWh - satuan mengikuti Jenis BBM kendaraan).</summary>
        [Required]
        public decimal JumlahPenggunaanBbm { get; set; }

        [Required]
        public decimal NilaiOdometer { get; set; }

        /// <summary>Nominal sesuai nota (Rp).</summary>
        [Required]
        public decimal NilaiNota { get; set; }

        // ---------- Step 4: Foto & Verifikasi ----------
        [Required]
        [MaxLength(500)]
        public string FotoOdometerUrl { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FotoNotaUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? CatatanTambahan { get; set; }

        // ---------- Approval ----------

        /// <summary>Pending | Approved | Rejected.</summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = StatusPending;

        /// <summary>UserId (AuthService) yang memutuskan (approve/reject).</summary>
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        /// <summary>Alasan penolakan - diisi kalau Status = Rejected.</summary>
        [MaxLength(500)]
        public string? RejectedReason { get; set; }
    }
}