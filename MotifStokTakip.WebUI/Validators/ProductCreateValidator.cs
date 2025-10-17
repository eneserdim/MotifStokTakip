using FluentValidation;
using MotifStokTakip.Core.Barcodes;
using MotifStokTakip.WebUI.Models;

namespace MotifStokTakip.WebUI.Validators;

public class ProductCreateValidator : AbstractValidator<ProductCreateViewModel>
{
    public ProductCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.OemNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BrandName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BrandCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ShelfNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);

        // Barkod boş olabilir; doluysa kontrol:
        RuleFor(x => x.Barcode)
            .MaximumLength(64).WithMessage("Barkod en fazla 64 karakter olabilir.")
            .Must(code =>
            {
                if (string.IsNullOrWhiteSpace(code)) return true; // boş -> geç
                var trimmed = code.Trim();
                // 13 haneli tümü digit ise EAN-13 checksum kontrolü yap
                if (trimmed.Length == 13 && trimmed.All(char.IsDigit))
                    return Ean13Helper.IsValid(trimmed);
                // Aksi halde (alfa/numerik karışık) CODE128 olarak kabul edeceğiz
                return true;
            })
            .WithMessage("Geçersiz EAN-13 barkodu (13 hane ve geçerli checksum olmalı).");
    }
}
