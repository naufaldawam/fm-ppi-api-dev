using System;
using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Biaya Kesehatan per Pekerja per bulan/tahun, dikelompok sesuai periode.
    /// Rfid / NoPekerja / Jabatan diturunkan dari Pekerja (read-only di form + detail).
    /// </summary>
    public class BiayaKesehatan : BaseEntity
    {
        [Required]
        public string PekerjaId { get; set; } = string.Empty;
        public Pekerja? Pekerja { get; set; }

        /// <summary>Bulan &amp; tahun biaya kesehatan (first day bulan, esempio 2026-09-01).</summary>
        [Required]
        public DateTime BulanTahun { get; set; }

        [Required]
        public string PeriodeId { get; set; } = string.Empty;
        public Periode? Periode { get; set; }

        [Required]
        public decimal TotalBiaya { get; set; }

        public bool IsActive { get; set; } = true;
    }
}