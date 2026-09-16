using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    public class OperasionalUpah : BaseEntity
    {
        [Required]
        public string PekerjaId { get; set; } = string.Empty;
        public Pekerja? Pekerja { get; set; }

        [Required]
        public string PeriodeId { get; set; } = string.Empty;
        public Periode? Periode { get; set; }

        [Required]
        public decimal TotalLembur { get; set; }

        [Required]
        public decimal TotalEMoneyMember { get; set; }

        [Required]
        public decimal DanaOps { get; set; }

        [Required]
        public decimal TotalParkir { get; set; }

        [Required]
        public decimal TotalSewaKendaraan { get; set; }

        [Required]
        public decimal TotalUpahDriver { get; set; }

        public bool IsActive { get; set; } = true;
    }
}