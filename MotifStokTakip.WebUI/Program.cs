using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MotifStokTakip.Service.Data;
using MotifStokTakip.Core.Security;
using MotifStokTakip.Service.Security;
using FluentValidation;
using MotifStokTakip.WebUI.Validators;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();


builder.Services.AddValidatorsFromAssemblyContaining<ProductCreateValidator>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITokenService, JwtTokenService>();

var jwt = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

// Basit rol politikalarý
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("MuhasebeOrAdmin", p => p.RequireRole("Muhasebe", "Admin"));
    options.AddPolicy("UstaOrAdmin", p => p.RequireRole("Usta", "Admin"));
});

QuestPDF.Settings.License = LicenseType.Community;


var supportedCultures = new[] { new CultureInfo("tr-TR") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("tr-TR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

app.MapControllerRoute(
    name: "ServiceInvoicePdfLegacy",
    pattern: "ServiceInvoices/Pdf/{id:int}",
    defaults: new { controller = "ServiceOrders", action = "InvoicePdf" });

app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Cookie'den JWT okumak (UI için pratik)
// Eðer "auth_token" cookie varsa Authorization header'a aktar.
app.Use(async (ctx, next) =>
{
    var token = ctx.Request.Cookies["auth_token"];
    if (!string.IsNullOrEmpty(token) && !ctx.Request.Headers.ContainsKey("Authorization"))
        ctx.Request.Headers.Append("Authorization", $"Bearer {token}");
    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=auth}/{action=login}/{id?}");

app.Run();
