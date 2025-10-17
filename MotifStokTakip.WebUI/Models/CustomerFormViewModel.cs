using System.ComponentModel.DataAnnotations;

namespace MotifStokTakip.WebUI.Models
{
    public class CustomerFormViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string FullName { get; set; } = null!;

        [MaxLength(120)]
        public string? CompanyName { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }
    }
}
