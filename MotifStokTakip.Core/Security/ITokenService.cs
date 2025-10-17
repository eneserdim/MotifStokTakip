using MotifStokTakip.Model.Entities;

namespace MotifStokTakip.Core.Security;
public interface ITokenService
{
    string CreateToken(AppUser user, string issuer, string audience, string key, int expireMinutes);
}
