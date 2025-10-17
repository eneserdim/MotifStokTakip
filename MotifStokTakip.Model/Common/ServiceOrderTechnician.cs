using MotifStokTakip.Model.Common;

namespace MotifStokTakip.Model.Entities
{
    public class ServiceOrderTechnician : BaseEntity
    {
        public int ServiceOrderId { get; set; }
        public ServiceOrder ServiceOrder { get; set; } = null!;

        public int TechnicianId { get; set; }
        public Technician Technician { get; set; } = null!;

        public bool IsPrimary { get; set; } = true;   // Ana usta?
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }
    }
}
