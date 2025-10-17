using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MotifStokTakip.WebUI.Models
{
    public class VehicleFormViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        [Display(Name = "Plaka")]
        public string Plate { get; set; } = null!;

        [Required, MaxLength(50)]
        [Display(Name = "Marka")]
        public string Brand { get; set; } = null!;

        [Required, MaxLength(50)]
        [Display(Name = "Model")]
        public string Model { get; set; } = null!;

        [Range(1900, 2100)]
        [Display(Name = "Yıl")]
        public int? Year { get; set; }

        [MaxLength(17)]
        [Display(Name = "Şasi No (VIN)")]
        public string? Vin { get; set; }

        [MaxLength(500)]
        [Display(Name = "Not")]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Cari (Müşteri) seçiniz.")]
        [Display(Name = "Cari (Müşteri)")]
        public int? CustomerId { get; set; }

        // Dropdown için seçenekler
        public IEnumerable<SelectListItem> Customers { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
