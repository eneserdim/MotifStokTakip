using MotifStokTakip.Model.Common;

namespace MotifStokTakip.Model.Entities
{
    public class Technician : BaseEntity   // BaseEntity: Id, CreatedAt vs sizde ne varsa
    {
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Skills { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<ServiceOrderTechnician> ServiceOrders { get; set; } = new List<ServiceOrderTechnician>();
    }
}
