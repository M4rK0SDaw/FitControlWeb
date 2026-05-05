using System.Text;
using ClosedXML.Excel;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace FitControlWeb.Helpers;

public static class ExportHelper
{
    public static byte[] ToCsv<T>(
        IEnumerable<T> data,
        string[] headers,
        Func<T, string[]> map)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(";", headers));

        foreach (var item in data)
        {
            var values = map(item)
                .Select(v => (v ?? "")
                    .Replace(";", ",")
                    .Replace("\n", " ")
                    .Replace("\r", " "));

            sb.AppendLine(string.Join(";", values));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static byte[] ToExcel<T>(
    IEnumerable<T> data,
    string sheetName,
    string title,
    string subtitle,
    string[] filters,
    List<ReportSummaryItem> summary,
    string[] headers,
    Func<T, object[]> map)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(sheetName);

        int totalColumns = headers.Length;

        // Título
        ws.Range(1, 1, 1, totalColumns).Merge();
        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 18;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#111318");
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Fecha
        ws.Range(2, 1, 2, totalColumns).Merge();
        ws.Cell(2, 1).Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(2, 1).Style.Font.Italic = true;
        ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int row = 4;

        // Filtros
        if (filters.Any())
        {
            ws.Cell(row, 1).Value = "Filtros aplicados";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            foreach (var filter in filters)
            {
                ws.Cell(row, 1).Value = filter;
                row++;
            }

            row++;
        }

        // Cabeceras
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#ff7a00");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        row++;

        // Datos
        foreach (var item in data)
        {
            var values = map(item);

            for (int col = 0; col < values.Length; col++)
            {
                ws.Cell(row, col + 1).Value = values[col]?.ToString() ?? "";
            }

            row++;
        }

        var usedRange = ws.RangeUsed();
        if (usedRange != null)
        {
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] ToPdf<T>(
     IEnumerable<T> data,
     string title,
     string subtitle,
     string[] filters,
     List<ReportSummaryItem> summary,
     string[] headers,
     Func<T, string[]> map)
    {
        using var stream = new MemoryStream();

        var writer = new PdfWriter(stream);
        var pdf = new PdfDocument(writer);
        var document = new iText.Layout.Document(pdf);

        // =========================
        // HEADER
        // =========================
        document.Add(
            new Paragraph("FITCONTROL WEB")
                .SetFontSize(20)
                .SimulateBold()
                .SetTextAlignment(TextAlignment.CENTER)
        );

        document.Add(
            new Paragraph(title)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
        );

        document.Add(
            new Paragraph(subtitle)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER)
        );

        document.Add(
            new Paragraph($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT)
        );

        document.Add(new Paragraph(" "));

        // =========================
        // KPIs (RESUMEN)
        // =========================
        if (summary.Any())
        {
            var kpiTable = new Table(summary.Count).UseAllAvailableWidth();

            foreach (var item in summary)
            {
                kpiTable.AddCell(
                    new Cell()
                        .SetTextAlignment(TextAlignment.CENTER)
                        .Add(new Paragraph(item.Label).SetFontSize(9))
                        .Add(new Paragraph(item.Value).SimulateBold().SetFontSize(14))
                );
            }

            document.Add(kpiTable);
            document.Add(new Paragraph(" "));
        }

        // =========================
        // FILTROS
        // =========================
        if (filters.Any())
        {
            document.Add(new Paragraph("Filtros aplicados").SimulateBold());

            foreach (var f in filters)
            {
                document.Add(new Paragraph($"• {f}").SetFontSize(9));
            }

            document.Add(new Paragraph(" "));
        }

        // =========================
        // TABLA
        // =========================
        var table = new Table(headers.Length).UseAllAvailableWidth();

        foreach (var h in headers)
        {
            table.AddHeaderCell(
                new Cell()
                    .SetBackgroundColor(new iText.Kernel.Colors.DeviceRgb(255, 122, 0))
                    .SetFontColor(iText.Kernel.Colors.ColorConstants.WHITE)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph(h))
            );
        }

        foreach (var item in data)
        {
            foreach (var value in map(item))
            {
                table.AddCell(value ?? "");
            }
        }

        document.Add(table);

        // =========================
        // FOOTER
        // =========================
        document.Add(
            new Paragraph("FitControl Web · Informe generado automáticamente")
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(20)
        );

        document.Close();

        return stream.ToArray();
    }
}