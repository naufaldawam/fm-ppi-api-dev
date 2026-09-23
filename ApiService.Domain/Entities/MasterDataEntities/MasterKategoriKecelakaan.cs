using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Data Master > Kategori Kecelakaan (kategori terjun). Tabel: MasterKategoriKecelakaan.
    /// Master ringan Name + IsActive, sama pattern dengan MasterJenisBbm.
    /// </summary>
    public class MasterKategoriKecelakaan : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}