using System.ComponentModel.DataAnnotations;

public class ServiceOrderCreateViewModel
{
    [Required] public int VehicleId { get; set; }
    [Required] public int CustomerId { get; set; }

    // Görünen metin alanları (autocomplete/quick pick)
    public string? VehicleText { get; set; }
    public string? CustomerText { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Açıklama / Şikayet zorunludur.")]
    public string Description { get; set; } = "";

}
