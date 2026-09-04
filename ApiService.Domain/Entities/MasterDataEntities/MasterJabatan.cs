using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Master data Jabatan (posisi/jabatan pekerja) - dipakai sebagai dropdown di form Pekerja.
    /// </summary>
    public class MasterJabatan : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}