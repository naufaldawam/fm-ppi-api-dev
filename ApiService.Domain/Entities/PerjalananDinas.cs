using System;
using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Perjalanan Dinas - biaya dinas pekerja per bulan/tahun, dikelompok sesuai periode.
    /// NamaPekerja / NoPekerja / NopekHome / NopekHost / Jabatan diturunkan dari Pekerja.
    /// </summary>
    public class PerjalananDinas : BaseEntity
    {
        [Required]
        public string PekerjaId { get; set; } = string.Empty;
        public Pekerja? Pekerja { get; set; }

        /// <summary>Bulan & tahun dinas (first day bulan, contoh 2026-09-01).</summary>
        [Required]
        public DateTime BulanTahun { get; set; }

        [Required]
        public string PeriodeId { get; set; } = string.Empty;
        public Periode? Periode { get; set; }

        [Required]
        public decimal TotalBiayaDinas { get; set; }

        public bool IsActive { get; set; } = true;
    }
}