using System.ComponentModel.DataAnnotations;

namespace MotifStokTakip.WebUI.Models;

public class VehicleUpsertViewModel
{
    public int? Id { get; set; }

    [Required] public int CustomerId { get; set; }

    [Required, StringLength(15)]
    public string Plate { get; set; } = null!;

    [StringLength(60)] public string? Brand { get; set; }
    [StringLength(60)] public string? Model { get; set; }
    public int? Year { get; set; }

    [StringLength(60)] public string? ChassisNo { get; set; }
    [StringLength(250)] public string? Note { get; set; }
}
