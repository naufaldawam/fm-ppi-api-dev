using System;
using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Data Master > Member Parkir.
    /// NamaPekerja / No.Pekerja / Jabatan otomatis diisi dari Pekerja yang dipilih.
    /// RfId = kode kartu. TanggalPenagihan = tanggal penagihan biaya parkir.
    /// JumlahBiaya = string (format dari UX: "Rp.999.000.000").
    /// PeriodeId = periode di mana record member parkir ini dibuat/dilaporkan.
    /// </summary>
    public class MemberParkir : BaseEntity
    {
        [Required]
        public string PekerjaId { get; set; } = string.Empty;
        public Pekerja? Pekerja { get; set; }

        /// <summary>Snapshot jabatan dari Pekerja - otomatis diisi server-side, tidak dari client.</summary>
        [Required]
        public string JabatanId { get; set; } = string.Empty;
        public MasterJabatan? Jabatan { get; set; }

        /// <summary>Periode di mana record ini dibuat (dropdown dari master Periode).</summary>
        [Required]
        public string PeriodeId { get; set; } = string.Empty;
        public Periode? Periode { get; set; }

        [Required]
        [MaxLength(50)]
        public string RfIdCode { get; set; } = string.Empty;

        [Required]
        public DateTime TanggalPenagihan { get; set; }

        [Required]
        [MaxLength(50)]
        public string JumlahBiaya { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}