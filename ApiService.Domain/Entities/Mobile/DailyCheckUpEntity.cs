using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Daily Check Up (DCU) yang diinput oleh Driver.
    /// Satu driver dapat melakukan satu DCU per tanggal.
    /// </summary>
    public class DailyCheckUpEntity : BaseEntity
    {
        public const string StatusFit = "Fit";
        public const string StatusUnfit = "Unfit";

        [Required]
        public string DriverId { get; set; } = string.Empty;
        public Driver? Driver { get; set; }

        [Required]
        public DateTime TanggalDcu { get; set; }

        [Required]
        public decimal TekananDarahSistolik { get; set; }

        [Required]
        public decimal TekananDarahDiastolik { get; set; }

        [Required]
        public decimal SaturasiOksigen { get; set; }

        [Required]
        public decimal NadiDenyut { get; set; }

        [Required]
        public decimal SuhuTubuh { get; set; }

        /// <summary>Fit atau Unfit. Status dipilih oleh Driver.</summary>
        [Required]
        [MaxLength(20)]
        public string StatusKesehatan { get; set; } = StatusFit;

        [MaxLength(1000)]
        public string? Keterangan { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<DailyCheckUpEvidance> Evidences { get; set; } = new List<DailyCheckUpEvidance>();
    }
}
