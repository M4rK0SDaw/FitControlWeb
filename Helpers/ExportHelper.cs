using System.Text;
using ClosedXML.Excel;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
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

        ws.Range(1, 1, 1, totalColumns).Merge();
        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 18;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#111318");
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(2, 1, 2, totalColumns).Merge();
        ws.Cell(2, 1).Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(2, 1).Style.Font.Italic = true;
        ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int row = 4;

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
        var document = new Document(pdf, PageSize.A4.Rotate());
        document.SetMargins(28, 24, 28, 24);

        var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        var colorPrincipal = new DeviceRgb(255, 122, 0);
        var colorTexto = new DeviceRgb(55, 65, 81);
        var colorSuave = new DeviceRgb(248, 250, 252);
        var colorBorde = new DeviceRgb(226, 232, 240);

        var safeRows = data
            .Select(map)
            .Select(row => row.Select(LimpiarTextoPdf).ToArray())
            .ToList();

        document.Add(
            new Paragraph("FITCONTROL WEB")
                .SetFont(fontBold)
                .SetFontSize(20)
                .SetFontColor(colorPrincipal)
                .SetTextAlignment(TextAlignment.CENTER));

        document.Add(
            new Paragraph(title)
                .SetFont(fontBold)
                .SetFontSize(13)
                .SetTextAlignment(TextAlignment.CENTER));

        document.Add(
            new Paragraph(subtitle)
                .SetFont(font)
                .SetFontSize(9)
                .SetFontColor(colorTexto)
                .SetTextAlignment(TextAlignment.CENTER));

        document.Add(
            new Paragraph($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                .SetFont(font)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetFontColor(colorTexto));

        document.Add(new Paragraph(" ").SetMarginBottom(4));

        if (summary.Any())
        {
            var kpiTable = new Table(summary.Count).UseAllAvailableWidth();

            foreach (var item in summary)
            {
                kpiTable.AddCell(
                    new Cell()
                        .SetBackgroundColor(colorSuave)
                        .SetBorder(new SolidBorder(colorBorde, 1))
                        .SetPadding(8)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .Add(new Paragraph(item.Label).SetFont(font).SetFontSize(8).SetFontColor(colorTexto))
                        .Add(new Paragraph(item.Value).SetFont(fontBold).SetFontSize(13)));
            }

            document.Add(kpiTable);
            document.Add(new Paragraph(" ").SetMarginBottom(3));
        }

        if (filters.Any())
        {
            document.Add(new Paragraph("Filtros aplicados").SetFont(fontBold).SetFontSize(10));

            foreach (var f in filters)
            {
                document.Add(
                    new Paragraph($"- {LimpiarTextoPdf(f)}")
                        .SetFont(font)
                        .SetFontSize(8.5f)
                        .SetFontColor(colorTexto));
            }

            document.Add(new Paragraph(" ").SetMarginBottom(3));
        }

        var table = new Table(UnitValue.CreatePercentArray(headers.Length)).UseAllAvailableWidth();
        table.SetFixedLayout();

        foreach (var h in headers)
        {
            table.AddHeaderCell(
                new Cell()
                    .SetBackgroundColor(colorPrincipal)
                    .SetBorder(Border.NO_BORDER)
                    .SetPadding(7)
                    .SetFont(fontBold)
                    .SetFontColor(ColorConstants.WHITE)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph(LimpiarTextoPdf(h)).SetFontSize(8.5f)));
        }

        foreach (var row in safeRows)
        {
            foreach (var value in row)
            {
                table.AddCell(
                    new Cell()
                        .SetBorder(new SolidBorder(colorBorde, 1))
                        .SetPadding(6)
                        .SetFont(font)
                        .SetFontSize(8)
                        .SetFontColor(colorTexto)
                        .Add(new Paragraph(value).SetMargin(0)));
            }
        }

        document.Add(table);

        document.Add(
            new Paragraph("FitControl Web · Informe generado automaticamente")
                .SetFont(font)
                .SetFontSize(8)
                .SetFontColor(ColorConstants.GRAY)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(20));

        document.Close();

        return stream.ToArray();
    }

    private static string LimpiarTextoPdf(string? value)
    {
        var clean = (value ?? string.Empty)
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        return clean.Length > 90 ? clean[..87] + "..." : clean;
    }
}
