using System;
using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// JumlahBiaya = nilai uang (Rp), JumlahKwh = pemakaian listrik (KWH).
    /// PeriodeId = periode di mana tagihan ini dibuat/dilaporkan.
    /// </summary>
    public class TagihanKwh : BaseEntity
    {
        public const string KategoriP8 = "p8";
        public const string KategoriUmum = "umum";

        [Required]
        public string PeriodeId { get; set; } = string.Empty;
        public Periode? Periode { get; set; }

        [Required]
        public DateTime TanggalPenagihan { get; set; }

        [Required]
        public decimal JumlahBiaya { get; set; }

        [Required]
        public decimal JumlahKwh { get; set; }

        [Required]
        [MaxLength(20)]
        public string Kategori { get; set; } = KategoriP8;

        public bool IsActive { get; set; } = true;
    }
}