using MotifStokTakip.Model.Common;
using MotifStokTakip.Model.Enums;

namespace MotifStokTakip.Model.Entities;

public class ServiceOrder : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    // Usta (AppUser) ataması
    public int? AssignedUserId { get; set; }
    public AppUser? AssignedUser { get; set; }

    public string ComplaintText { get; set; } = null!;    // Şikayetler (textarea)
    public string? DoneWorksText { get; set; }            // Yapılan işlemler (usta doldurur)

    public ServiceStatus Status { get; set; } = ServiceStatus.ServiseAlindi;
    public DateTime? CompletedAt { get; set; }

    public ICollection<ServicePhoto>? Photos { get; set; }
    public ICollection<ServiceInvoice>? Invoices { get; set; }
    public ICollection<ServiceOrderTechnician> Technicians { get; set; } = new List<ServiceOrderTechnician>();

}
