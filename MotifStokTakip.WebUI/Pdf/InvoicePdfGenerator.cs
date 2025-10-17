using System.Globalization;
using System.Linq;
using MotifStokTakip.Model.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MotifStokTakip.WebUI.Pdf;

public static class InvoicePdfGenerator
{
    private static readonly CultureInfo tr = new("tr-TR");

    public static byte[] Generate(ServiceInvoice invoice)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));

                // -------- Header --------
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Motif Otomotiv").Bold().FontSize(16);
                        col.Item().Text("Servis Faturası").SemiBold();
                        col.Item().Text($"Fatura No: {invoice.Id}");
                        col.Item().Text($"Tarih: {invoice.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}");
                        var o = invoice.ServiceOrder!;
                        col.Item().Text("Araç Bilgileri").Bold();
                        col.Item().Text($"Araç: {o.Vehicle!.Plate} - {o.Vehicle.Brand} {o.Vehicle.Model}");
                        col.Item().Text($"Model Yılı: {o.Vehicle!.Year}");
                        col.Item().Text($"Şasi No: {o.Vehicle!.Vin}");
                        if (o.AssignedUser != null)
                            col.Item().Text($"Usta: {o.AssignedUser.FullName}");
                    });

                    row.ConstantItem(200).AlignRight().Column(col =>
                    {
                        var o = invoice.ServiceOrder!;
                        col.Item().Text("Müşteri Bilgileri").Bold();
                        col.Item().Text(o.Customer!.FullName);
                        col.Item().Text(o.Customer!.CompanyName);
                        col.Item().Text(o.Customer!.Phone);
                        col.Item().Text("Adres Bilgisi").Bold(); col.Item().Text(o.Customer!.Address);
                    });
                });

                // -------- Content --------
                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    // Kalemler tablosu
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5); // Kalem
                            c.RelativeColumn(2); // Adet
                            c.RelativeColumn(3); // Alış
                            c.RelativeColumn(3); // Satış
                            c.RelativeColumn(3); // Tutar
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("Kalem");
                            h.Cell().Element(HeaderCell).Text("Adet");
                            h.Cell().Element(HeaderCell).Text("Alış");
                            h.Cell().Element(HeaderCell).Text("Satış");
                            h.Cell().Element(HeaderCell).Text("Tutar");

                            static IContainer HeaderCell(IContainer c) =>
                                c.DefaultTextStyle(x => x.SemiBold())
                                 .Padding(4)
                                 .Background(Colors.Grey.Lighten3);
                        });

                        foreach (var it in invoice.Items)
                        {
                            table.Cell().Element(Cell).Text(it.ItemName);
                            table.Cell().Element(Cell).Text(it.Quantity.ToString(tr));
                            table.Cell().Element(Cell).Text(it.CostPrice.ToString("N2", tr));
                            table.Cell().Element(Cell).Text(it.SalePrice.ToString("N2", tr));
                            table.Cell().Element(Cell).Text((it.SalePrice * it.Quantity).ToString("N2", tr));

                            static IContainer Cell(IContainer c) =>
                                c.Padding(4)
                                 .BorderBottom(0.25f)
                                 .BorderColor(Colors.Grey.Lighten3);
                        }
                    });

                    // Özet
                    var subtotal = invoice.Items.Sum(x => x.SalePrice * x.Quantity);
                    var total = subtotal; // KDV uygulamıyorsak

                    col.Item().AlignRight().Column(sum =>
                    {
                        sum.Item().Text($"Ara Toplam: {subtotal.ToString("N2", tr)}").SemiBold();
                        sum.Item().Text($"Genel Toplam: {total.ToString("N2", tr)}").Bold().FontSize(12);
                        sum.Item().Text($"Ödeme Durumu: {(invoice.IsPaid ? "FATURA ÖDENDİ" : "ÖDEME BEKLENİYOR")}")
                                  .FontColor(invoice.IsPaid ? Colors.Green.Darken2 : Colors.Red.Darken2);
                    });

                    // Not: Text(...) sonrası chain yok; rengi ya span içinde veriyoruz
                    //     ya da Element(...DefaultTextStyle...) ile container'a uyguluyoruz.
                    col.Item()
                       .Element(e => e.DefaultTextStyle(s => s.FontColor(Colors.Grey.Darken1)))
                       .Text(t =>
                       {
                           t.Span("Not: ");
                           t.Span("Bu döküman dijital olarak oluşturulmuştur. Resmi fatura yerine geçmemektedir.").Italic();
                       });
                });

                // -------- Footer --------
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Motif Otomotiv • Sayfa ");
                    t.CurrentPageNumber();   // zincir yok
                    t.Span(" / ");
                    t.TotalPages();          // zincir yok
                });
            });
        });

        return document.GeneratePdf();
    }
}
