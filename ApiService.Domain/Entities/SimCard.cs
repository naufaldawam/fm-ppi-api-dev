using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    /// <summary>
    /// Biaya SIM Card per Pekerja. Form: Pekerja + BiayaSimCard.
    /// NamaPekerja / NoPekerja / NopekHome / NopekHost / Jabatan diturunkan dari Pekerja.
    /// </summary>
    public class SimCard : BaseEntity
    {
        [Required]
        public string PekerjaId { get; set; } = string.Empty;
        public Pekerja? Pekerja { get; set; }

        [Required]
        public decimal BiayaSimCard { get; set; }

        public bool IsActive { get; set; } = true;
    }
}