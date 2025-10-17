namespace MotifStokTakip.Core.Barcodes;

public static class Ean13Helper
{
    public static int ComputeCheckDigit(string twelveDigits)
    {
        if (string.IsNullOrWhiteSpace(twelveDigits) || twelveDigits.Length != 12 || !twelveDigits.All(char.IsDigit))
            throw new ArgumentException("EAN-13 check digit requires 12 numeric characters.");

        int sumOdd = 0, sumEven = 0; // 0-based index
        for (int i = 0; i < 12; i++)
        {
            int d = twelveDigits[i] - '0';
            if ((i % 2) == 0) sumOdd += d; else sumEven += d;
        }
        int total = sumOdd + (sumEven * 3);
        int mod = total % 10;
        return (10 - mod) % 10;
    }

    public static string MakeEan13FromId(int id)
    {
        var core12 = "869" + id.ToString().PadLeft(9, '0'); // demo amaçlı
        var cd = ComputeCheckDigit(core12);
        return core12 + cd.ToString();
    }

    public static bool IsValid(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 13 || !code.All(char.IsDigit)) return false;
        var cd = ComputeCheckDigit(code[..12]);
        return cd == (code[12] - '0');
    }
}
