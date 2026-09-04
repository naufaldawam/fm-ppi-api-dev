using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Data master Pekerja (Data Master &gt; Pekerja). Master independen -
    /// tidak bergantung ke entity lain untuk bisa dibuat.
    /// </summary>
    public class Pekerja : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string NoPekerja { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string NopekHome { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string NopekHost { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string NamaPekerja { get; set; } = string.Empty;

        [Required]
        public string JabatanId { get; set; } = string.Empty;
        public MasterJabatan? Jabatan { get; set; }

        /// <summary>
        /// Daftar kode RF.ID yang di-assign ke pekerja ini (bisa lebih dari satu, bisa kosong).
        /// READ-ONLY dari sisi Pekerja: diisi/diubah lewat menu RF.ID terpisah, bukan lewat
        /// endpoint Create/Update Pekerja.
        /// </summary>
        public List<string> RfIds { get; set; } = new();

        public bool IsActive { get; set; } = true;
    }
}