using KayCareLIS.Core.Constants;
using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KayCareLIS.Infrastructure.Services;

public class LabReportService : ILabReportService
{
    private readonly AppDbContext             _db;
    private readonly IFacilitySettingsService _facility;

    public LabReportService(AppDbContext db, IFacilitySettingsService facility)
    {
        _db       = db;
        _facility = facility;
    }

    public async Task<byte[]?> GenerateLabOrderReportAsync(Guid labOrderId, CancellationToken ct)
    {
        var order = await _db.LabOrders
            .Include(o => o.Patient)
            .Include(o => o.OrderingDoctor)
            .Include(o => o.Items.OrderBy(i => i.Department).ThenBy(i => i.TestName))
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.LabOrderId == labOrderId, ct);

        if (order == null) return null;

        var facilitySettings = await _facility.GetAsync(ct);
        var logoBytes        = facilitySettings?.HasLogo == true ? await _facility.GetLogoBytesAsync(ct) : null;
        var facilityName     = facilitySettings?.FacilityName ?? "KayCare LIS";
        var facilityAddress  = facilitySettings?.Address;
        var facilityPhone    = facilitySettings?.Phone;
        var facilityEmail    = facilitySettings?.Email;

        var patient = order.Patient;
        var doctor  = order.OrderingDoctor;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        if (logoBytes != null)
                            row.ConstantItem(70).Image(logoBytes).FitArea();
                        else
                            row.ConstantItem(0);

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(facilityName).FontSize(16).Bold();
                            if (facilityAddress != null) c.Item().Text(facilityAddress).FontSize(9);
                            if (facilityPhone   != null) c.Item().Text($"Tel: {facilityPhone}").FontSize(9);
                            if (facilityEmail   != null) c.Item().Text($"Email: {facilityEmail}").FontSize(9);
                        });

                        row.ConstantItem(100).AlignRight().Column(c =>
                        {
                            c.Item().Text("LAB REPORT").FontSize(14).Bold();
                            c.Item().Text(DateTime.UtcNow.ToString("dd MMM yyyy")).FontSize(9);
                        });
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    // Patient + Order info
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("PATIENT INFORMATION").FontSize(9).Bold().FontColor("#555555");
                            c.Item().Text($"{patient.FirstName} {patient.LastName}").Bold();
                            c.Item().Text($"MRN: {patient.MedicalRecordNumber}  |  DOB: {patient.DateOfBirth:dd MMM yyyy}  |  {patient.Gender}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("ORDER INFORMATION").FontSize(9).Bold().FontColor("#555555");
                            c.Item().Text($"Ordered by: Dr. {doctor.FirstName} {doctor.LastName}");
                            c.Item().Text($"Date: {order.CreatedAt:dd MMM yyyy HH:mm}");
                            c.Item().Text($"Organisation: {order.Organisation}");
                            c.Item().Text($"Status: {order.Status}");
                        });
                    });

                    col.Item().PaddingTop(8).PaddingBottom(4).Text("TEST RESULTS").FontSize(11).Bold();

                    // Results table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(40);
                            columns.RelativeColumn(2);
                        });

                        // Header
                        static IContainer HeaderStyle(IContainer c) => c.Background("#1e3a5f").Padding(4);
                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderStyle).Text("Test").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Accession").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Result").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Units / Ref Range").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Flag").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Element(HeaderStyle).Text("Status").FontColor(Colors.White).Bold().FontSize(9);
                        });

                        var row = 0;
                        string? lastDept = null;
                        foreach (var item in order.Items)
                        {
                            if (item.Department != lastDept)
                            {
                                lastDept = item.Department;
                                table.Cell().ColumnSpan(6)
                                    .Background("#e8f0fe").Padding(4)
                                    .Text(item.Department).Bold().FontSize(9);
                            }

                            var bg = row++ % 2 == 0 ? "#ffffff" : "#f9f9f9";
                            static IContainer CellStyle(IContainer c, string color) =>
                                c.Background(color).Padding(4).BorderBottom(0.5f).BorderColor("#dddddd");

                            table.Cell().Element(c => CellStyle(c, bg)).Text(item.TestName).FontSize(9);
                            table.Cell().Element(c => CellStyle(c, bg)).Text(item.AccessionNumber ?? "-").FontSize(9).FontColor("#666666");

                            var result = item.ManualResult ?? "-";
                            table.Cell().Element(c => CellStyle(c, bg)).Text(result).FontSize(9);

                            var unitRef = string.Join(" / ", new[] { item.ManualResultUnit, item.ManualResultReferenceRange }
                                .Where(s => !string.IsNullOrEmpty(s)));
                            table.Cell().Element(c => CellStyle(c, bg)).Text(unitRef).FontSize(8).FontColor("#555555");

                            var flag = item.ManualResultFlag ?? string.Empty;
                            var flagColor = flag == "H" ? "#f44336" : flag == "L" ? "#1565c0" : "#000000";
                            table.Cell().Element(c => CellStyle(c, bg)).Text(flag).FontSize(9).Bold().FontColor(flagColor);
                            table.Cell().Element(c => CellStyle(c, bg)).Text(item.Status).FontSize(9);
                        }
                    });

                    if (!string.IsNullOrEmpty(order.Notes))
                    {
                        col.Item().PaddingTop(12).Column(c =>
                        {
                            c.Item().Text("Notes:").Bold().FontSize(9);
                            c.Item().Text(order.Notes).FontSize(9);
                        });
                    }

                    col.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("________________________").FontSize(10);
                            c.Item().Text($"Dr. {doctor.FirstName} {doctor.LastName}").FontSize(9);
                            c.Item().Text("Authorised Signature").FontSize(8).FontColor("#888888");
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"Printed: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontSize(8).FontColor("#888888");
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ").FontSize(8).FontColor("#888888");
                    text.CurrentPageNumber().FontSize(8).FontColor("#888888");
                    text.Span(" of ").FontSize(8).FontColor("#888888");
                    text.TotalPages().FontSize(8).FontColor("#888888");
                });
            });
        });

        return document.GeneratePdf();
    }
}
