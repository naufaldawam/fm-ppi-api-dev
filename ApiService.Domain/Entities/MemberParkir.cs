using System;
using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Data Master > Member Parkir.
    /// NamaPekerja / No.Pekerja / Jabatan otomatis diisi dari Pekerja yang dipilih.
    /// RF.ID TIDAK disimpan di sini - seorang Pekerja bisa punya banyak RF.ID atau
    /// belum punya sama sekali, jadi selalu ditarik on-the-fly dari Pekerja.RfIds
    /// saat data ditampilkan (lihat MemberParkirDto.RfIds).
    /// JumlahBiaya = nilai uang, disimpan sebagai decimal (bukan string).
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
        public DateTime TanggalPenagihan { get; set; }

        [Required]
        public decimal JumlahBiaya { get; set; }

        public bool IsActive { get; set; } = true;
    }
}