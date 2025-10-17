using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotifStokTakip.Service.Data;
using MotifStokTakip.Core.Security;
using MotifStokTakip.WebUI.Models;

namespace MotifStokTakip.WebUI.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _cfg;

    public AuthController(AppDbContext db, ITokenService tokenService, IConfiguration cfg)
    {
        _db = db; _tokenService = tokenService; _cfg = cfg;
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var hash = PasswordHasher.Sha256(vm.Password);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserName == vm.UserName && x.PasswordHash == hash && x.IsActive);
        if (user == null)
        {
            ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı.");
            return View(vm);
        }

        // JWT oluştur
        var token = _tokenService.CreateToken(
            user,
            _cfg["Jwt:Issuer"]!,
            _cfg["Jwt:Audience"]!,
            _cfg["Jwt:Key"]!,
            int.Parse(_cfg["Jwt:ExpireMinutes"]!)
        );

        // UI pratikliği: token'ı HttpOnly cookie'ye yaz
        Response.Cookies.Append("auth_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_cfg["Jwt:ExpireMinutes"]!))
        });

        TempData["ok"] = $"Hoş geldin {user.FullName}";
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        Response.Cookies.Delete("auth_token");
        return RedirectToAction("Login");
    }
}
