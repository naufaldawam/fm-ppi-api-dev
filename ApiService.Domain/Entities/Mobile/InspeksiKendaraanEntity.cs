using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities.Mobile
{
    /// <summary>
    /// Header inspeksi kendaraan.
    /// Kelayakan jalan dihitung server-side dari seluruh detail inspeksi.
    /// </summary>
    public class InspeksiKendaraanEntity : BaseEntity
    {
        [Required]
        public string KendaraanId { get; set; } = string.Empty;
        public Kendaraan? Kendaraan { get; set; }

        [Required]
        public string DriverId { get; set; } = string.Empty;
        public Driver? Driver { get; set; }

        [Required]
        public DateTime TanggalInspeksi { get; set; }

        [Required]
        [MaxLength(30)]
        public string KelayakanJalan { get; set; } = StatusTidakLayakJalan;

        [MaxLength(1000)]
        public string? Keterangan { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<InspeksiKendaraanDetailEntity> Details { get; set; } = [];

        public const string StatusLayakJalan = "Layak Jalan";
        public const string StatusTidakLayakJalan = "Tidak Layak Jalan";
    }
}
