using MotifStokTakip.Model.Common;

namespace MotifStokTakip.Model.Entities;

public class Customer : BaseEntity
{
    public string FullName { get; set; } = null!;
    public string? CompanyName { get; set; }
    public string Address { get; set; } = null!;
    public string Phone { get; set; } = null!;

    public ICollection<Vehicle>? Vehicles { get; set; }
    public ICollection<ServiceOrder>? ServiceOrders { get; set; }
    public ICollection<Sale>? Sales { get; set; }
}
