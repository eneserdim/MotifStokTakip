using MotifStokTakip.Model.Common;
using MotifStokTakip.Model.Enums;

namespace MotifStokTakip.Model.Entities;

public class AppUser : BaseEntity
{
    public string FullName { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}
