using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Data master RF.ID (Data Master &gt; RF.ID). Merepresentasikan kartu/tag RFID yang
    /// di-assign ke seorang Pekerja dan satu Kendaraan (Nopol). Setiap create/update/delete
    /// akan mensinkronkan kode RF.ID ini ke Pekerja.RfIds.
    /// </summary>
    public class RfId : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string RfIdCode { get; set; } = string.Empty;

        [Required]
        public string PekerjaId { get; set; } = string.Empty;
        public Pekerja? Pekerja { get; set; }

        [Required]
        public string KendaraanId { get; set; } = string.Empty;
        public Kendaraan? Kendaraan { get; set; }

        public bool IsActive { get; set; } = true;
    }
}