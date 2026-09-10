using System;
using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    public class Periode : BaseEntity
    {
        /// <summary>Nama/label periode - string bebas, format ditentukan dari FE.</summary>
        [Required]
        [MaxLength(100)]
        public string NamaPeriode { get; set; } = string.Empty;

        [Required]
        public DateTime TanggalAwal { get; set; }

        [Required]
        public DateTime TanggalAkhir { get; set; }

        public bool IsActive { get; set; } = false;
    }
}