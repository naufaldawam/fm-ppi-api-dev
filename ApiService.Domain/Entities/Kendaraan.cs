using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Data master Kendaraan (Data Master &gt; Kendaraan).
    /// Mereferensikan MasterTipe, MasterBahanBakar, MasterVendor, MasterKepemilikan,
    /// MasterJabatan (alokasi jabatan) dan opsional Pekerja (pejabat pemegang kendaraan).
    /// </summary>
    public class Kendaraan : BaseEntity
    {
        [Required]
        [MaxLength(20)]
        public string NomorPolisi { get; set; } = string.Empty;

        [Required]
        public string TipeId { get; set; } = string.Empty;
        public MasterTipe? Tipe { get; set; }

        [Required]
        public string BahanBakarId { get; set; } = string.Empty;
        public MasterBahanBakar? BahanBakar { get; set; }

        [Required]
        [MaxLength(100)]
        public string Merek { get; set; } = string.Empty;

        [Required]
        public string VendorId { get; set; } = string.Empty;
        public MasterVendor? Vendor { get; set; }

        [Required]
        public string KepemilikanId { get; set; } = string.Empty;
        public MasterKepemilikan? Kepemilikan { get; set; }

        /// <summary>Alokasi Jabatan - jabatan yang berhak memakai kendaraan ini.</summary>
        [Required]
        public string JabatanId { get; set; } = string.Empty;
        public MasterJabatan? Jabatan { get; set; }
        public string? PekerjaId { get; set; }
        public Pekerja? Pekerja { get; set; }
        public bool IsActive { get; set; } = true;
    }
}