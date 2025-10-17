using MotifStokTakip.Model.Common;

namespace MotifStokTakip.Model.Entities;

public class ServicePhoto : BaseEntity
{
    public int ServiceOrderId { get; set; }
    public ServiceOrder ServiceOrder { get; set; } = null!;

    public string FilePath { get; set; } = null!; // yüklenen resmin yolu
    public bool IsBefore { get; set; }            // hasarlı/önce mi? yoksa işlem sonrası mı?
}
