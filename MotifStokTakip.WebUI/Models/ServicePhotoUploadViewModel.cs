using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MotifStokTakip.WebUI.Models;

public class ServicePhotoUploadViewModel
{
    [Required] public int ServiceOrderId { get; set; }
    public bool IsBefore { get; set; } = true;          // önce mi?
    [Required] public IFormFileCollection Files { get; set; } = null!;
}
