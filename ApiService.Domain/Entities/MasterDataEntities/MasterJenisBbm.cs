using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Data Master > Jenis BBM (jenis bahan bakar mineraal). Tabel: MasterJenisBbm.
    /// Master ringan Name + IsActive, sama pattern dengan MasterTipe/MasterBahanBakar.
    /// </summary>
    public class MasterJenisBbm : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}