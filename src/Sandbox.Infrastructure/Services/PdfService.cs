using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sandbox.Application.Abstractions.Services;
using Sandbox.Application.DTOs;

namespace Sandbox.Infrastructure.Services;

public class PdfService : IPdfService
{
    public Task<PdfGenerationResult> GenerateEmployeePdfAsync(EmployeeRequest employee)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header()
                    .Text("Employee Certificate")
                    .SemiBold()
                    .FontSize(24)
                    .FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Text($"Client ID: {employee.ClientId}");
                        column.Item().Text($"Employee ID: {employee.EmployeeId}");
                        column.Item().Text($"Name: {employee.EmployeeName}");
                        column.Item().Text($"Age: {employee.EmployeeAge}");
                        column.Item().Text($"Email: {employee.Email}");

                        column.Item().PaddingTop(20).Text("Certificates:").SemiBold();

                        if (employee.EmployeeCertificates.Any())
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Certificate ID").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Certificate Name").SemiBold();
                                });

                                foreach (var cert in employee.EmployeeCertificates)
                                {
                                    table.Cell().Element(CellStyle).Text(cert.CertificateId);
                                    table.Cell().Element(CellStyle).Text(cert.CertificateName);
                                }

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container
                                        .Border(1)
                                        .BorderColor(Colors.Grey.Lighten1)
                                        .Padding(5);
                                }
                            });
                        }
                        else
                        {
                            column.Item().Text("No certificates found.");
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Generated on: ");
                        x.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));
                    });
            });
        });

        var pdfBytes = document.GeneratePdf();
        var fileName = $"employee_{employee.EmployeeId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";

        return Task.FromResult(new PdfGenerationResult(pdfBytes, fileName));
    }
}
