using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KayCareLIS.Infrastructure.Services;

public class BillingPdfService : IBillingPdfService
{
    private readonly AppDbContext             _db;
    private readonly IFacilitySettingsService _facility;

    public BillingPdfService(AppDbContext db, IFacilitySettingsService facility)
    {
        _db       = db;
        _facility = facility;
    }

    public async Task<byte[]?> GenerateBillInvoiceAsync(Guid billId, CancellationToken ct)
    {
        var bill = await _db.Bills
            .Include(b => b.Patient)
            .Include(b => b.CreatedBy)
            .Include(b => b.Items)
            .Include(b => b.Payments).ThenInclude(p => p.ReceivedBy)
            .Include(b => b.Adjustments).ThenInclude(a => a.AdjustedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BillId == billId, ct);

        if (bill == null) return null;

        var facilitySettings = await _facility.GetAsync(ct);
        var logoBytes        = facilitySettings?.HasLogo == true ? await _facility.GetLogoBytesAsync(ct) : null;
        var facilityName     = facilitySettings?.FacilityName ?? "KayCare LIS";
        var facilityAddress  = facilitySettings?.Address;
        var facilityPhone    = facilitySettings?.Phone;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        if (logoBytes != null)
                            row.ConstantItem(60).Image(logoBytes).FitArea();

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(facilityName).FontSize(15).Bold();
                            if (facilityAddress != null) c.Item().Text(facilityAddress).FontSize(8);
                            if (facilityPhone   != null) c.Item().Text($"Tel: {facilityPhone}").FontSize(8);
                        });

                        row.ConstantItem(120).AlignRight().Column(c =>
                        {
                            c.Item().Text("INVOICE").FontSize(16).Bold().FontColor("#1e3a5f");
                            c.Item().Text(bill.BillNumber).FontSize(11).Bold();
                            c.Item().Text($"Date: {(bill.IssuedAt ?? bill.CreatedAt):dd MMM yyyy}").FontSize(8);
                        });
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("BILL TO").FontSize(8).Bold().FontColor("#888888");
                            c.Item().Text($"{bill.Patient.FirstName} {bill.Patient.LastName}").Bold();
                            c.Item().Text($"MRN: {bill.Patient.MedicalRecordNumber}");
                            if (bill.Patient.PhoneNumber != null) c.Item().Text($"Tel: {bill.Patient.PhoneNumber}");
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("STATUS").FontSize(8).Bold().FontColor("#888888");
                            c.Item().Text(bill.Status.ToUpper()).Bold();
                        });
                    });

                    // Line items
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(col =>
                        {
                            col.RelativeColumn(4);
                            col.RelativeColumn(2);
                            col.ConstantColumn(70);
                            col.ConstantColumn(80);
                            col.ConstantColumn(80);
                        });

                        static IContainer Header(IContainer c) => c.Background("#1e3a5f").Padding(5);
                        table.Header(h =>
                        {
                            h.Cell().Element(Header).Text("Description").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Element(Header).Text("Category").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Element(Header).AlignRight().Text("Qty").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Element(Header).AlignRight().Text("Unit Price").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Element(Header).AlignRight().Text("Total").FontColor(Colors.White).Bold().FontSize(9);
                        });

                        var idx = 0;
                        foreach (var item in bill.Items)
                        {
                            var bg = idx++ % 2 == 0 ? "#ffffff" : "#f9f9f9";
                            static IContainer Cell(IContainer c, string color) =>
                                c.Background(color).BorderBottom(0.5f).BorderColor("#dddddd").Padding(5);

                            table.Cell().Element(c => Cell(c, bg)).Text(item.Description).FontSize(9);
                            table.Cell().Element(c => Cell(c, bg)).Text(item.Category ?? "—").FontSize(9).FontColor("#666666");
                            table.Cell().Element(c => Cell(c, bg)).AlignRight().Text(item.Quantity.ToString()).FontSize(9);
                            table.Cell().Element(c => Cell(c, bg)).AlignRight().Text(item.UnitPrice.ToString("N2")).FontSize(9);
                            table.Cell().Element(c => Cell(c, bg)).AlignRight().Text(item.TotalPrice.ToString("N2")).FontSize(9);
                        }
                    });

                    // Totals
                    col.Item().PaddingTop(8).AlignRight().Width(200).Column(c =>
                    {
                        void Row(string label, string value, bool isBold = false)
                        {
                            c.Item().Row(r =>
                            {
                                var lt = r.RelativeItem().AlignLeft().Text(label).FontSize(9);
                                if (isBold) lt.Bold();
                                var rt = r.ConstantItem(80).AlignRight().Text(value).FontSize(9);
                                if (isBold) rt.Bold();
                            });
                        }

                        Row("Subtotal:", bill.TotalAmount.ToString("N2"));
                        if (bill.AdjustmentTotal != 0)
                            Row("Adjustments:", bill.AdjustmentTotal.ToString("N2"));
                        if (bill.DiscountAmount > 0)
                            Row($"Discount{(bill.DiscountReason != null ? $" ({bill.DiscountReason})" : "")}:", $"({bill.DiscountAmount:N2})");
                        c.Item().PaddingVertical(2).LineHorizontal(0.5f);
                        Row("AMOUNT DUE:", bill.BalanceDue.ToString("N2"), isBold: true);
                    });

                    // Payment history
                    if (bill.Payments.Any())
                    {
                        col.Item().PaddingTop(12).Text("PAYMENT HISTORY").Bold().FontSize(10);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.ConstantColumn(80);
                            });

                            static IContainer H(IContainer c) => c.Background("#4a7c59").Padding(4);
                            table.Header(h =>
                            {
                                h.Cell().Element(H).Text("Date").FontColor(Colors.White).Bold().FontSize(8);
                                h.Cell().Element(H).Text("Method").FontColor(Colors.White).Bold().FontSize(8);
                                h.Cell().Element(H).Text("Reference").FontColor(Colors.White).Bold().FontSize(8);
                                h.Cell().Element(H).AlignRight().Text("Amount").FontColor(Colors.White).Bold().FontSize(8);
                            });

                            foreach (var pay in bill.Payments)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor("#dddddd").Padding(4).Text(pay.PaymentDate.ToString("dd MMM yyyy")).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor("#dddddd").Padding(4).Text(pay.PaymentMethod).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor("#dddddd").Padding(4).Text(pay.Reference ?? "—").FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor("#dddddd").Padding(4).AlignRight().Text(pay.Amount.ToString("N2")).FontSize(8);
                            }
                        });
                    }

                    if (!string.IsNullOrEmpty(bill.Notes))
                    {
                        col.Item().PaddingTop(12).Column(c =>
                        {
                            c.Item().Text("Notes:").Bold().FontSize(9);
                            c.Item().Text(bill.Notes).FontSize(9);
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span($"Printed {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC  |  Page ").FontSize(7).FontColor("#888888");
                    text.CurrentPageNumber().FontSize(7).FontColor("#888888");
                    text.Span(" of ").FontSize(7).FontColor("#888888");
                    text.TotalPages().FontSize(7).FontColor("#888888");
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]?> GeneratePaymentReceiptAsync(Guid paymentId, CancellationToken ct)
    {
        var payment = await _db.Payments
            .Include(p => p.Bill).ThenInclude(b => b.Patient)
            .Include(p => p.ReceivedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId, ct);

        if (payment == null) return null;

        var facilitySettings = await _facility.GetAsync(ct);
        var logoBytes        = facilitySettings?.HasLogo == true ? await _facility.GetLogoBytesAsync(ct) : null;
        var facilityName     = facilitySettings?.FacilityName ?? "KayCare LIS";

        var bill    = payment.Bill;
        var patient = bill.Patient;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    if (logoBytes != null)
                        col.Item().AlignCenter().Width(60).Image(logoBytes).FitArea();
                    col.Item().AlignCenter().Text(facilityName).FontSize(14).Bold();
                    col.Item().PaddingVertical(4).LineHorizontal(1);
                    col.Item().AlignCenter().Text("PAYMENT RECEIPT").FontSize(13).Bold();
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text("Patient:");
                        row.RelativeItem().AlignRight().Text($"{patient.FirstName} {patient.LastName}").Bold();
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("MRN:");
                        row.RelativeItem().AlignRight().Text(patient.MedicalRecordNumber);
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Invoice:");
                        row.RelativeItem().AlignRight().Text(bill.BillNumber).Bold();
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Payment date:");
                        row.RelativeItem().AlignRight().Text(payment.PaymentDate.ToString("dd MMM yyyy HH:mm"));
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Method:");
                        row.RelativeItem().AlignRight().Text(payment.PaymentMethod);
                    });
                    if (!string.IsNullOrEmpty(payment.Reference))
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Reference:");
                            row.RelativeItem().AlignRight().Text(payment.Reference);
                        });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Received by:");
                        row.RelativeItem().AlignRight().Text($"{payment.ReceivedBy.FirstName} {payment.ReceivedBy.LastName}");
                    });
                    col.Item().PaddingTop(10).LineHorizontal(1);
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Text("AMOUNT PAID").FontSize(12).Bold();
                        row.RelativeItem().AlignRight().Text(payment.Amount.ToString("N2")).FontSize(14).Bold().FontColor("#1e3a5f");
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1);
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text("Total bill:");
                        row.RelativeItem().AlignRight().Text(bill.TotalAmount.ToString("N2"));
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Total paid:");
                        row.RelativeItem().AlignRight().Text(bill.PaidAmount.ToString("N2"));
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Balance due:");
                        row.RelativeItem().AlignRight().Text(bill.BalanceDue.ToString("N2")).Bold();
                    });
                    col.Item().PaddingTop(16).AlignCenter().Text("Thank you for choosing us.").FontColor("#555555").Italic();
                    col.Item().PaddingTop(4).AlignCenter().Text($"Printed {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontSize(7).FontColor("#888888");
                });
            });
        });

        return document.GeneratePdf();
    }
}
