using FitControlWeb.Models.Entities;
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

public static class FacturaPdfHelper
{
    public static byte[] GenerarFacturaPdf(Factura factura)
    {
        using var ms = new MemoryStream();
        using var writer = new PdfWriter(ms);
        using var pdf = new PdfDocument(writer);
        // using var document = new Document(pdf, PageSize.A4);
        var document = new Document(pdf, PageSize.A4.Rotate());


        document.SetMargins(30, 30, 30, 30);

        var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

        var colorPrincipal = new DeviceRgb(255, 122, 0);   // naranja
        var colorTexto = new DeviceRgb(60, 60, 60);
        var colorGris = new DeviceRgb(245, 245, 245);
        var colorBorde = new DeviceRgb(220, 220, 220);
        var colorVerde = new DeviceRgb(25, 135, 84);
        var colorRojo = new DeviceRgb(220, 53, 69);

        // =========================
        // CABECERA
        // =========================
        var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 60, 40 }))
            .UseAllAvailableWidth();

        var leftHeader = new Cell()
            .SetBorder(Border.NO_BORDER)
            .Add(new Paragraph("FITCONTROL")
                .SetFont(fontBold)
                .SetFontSize(22)
                .SetFontColor(colorPrincipal))
            .Add(new Paragraph("Factura de cliente")
                .SetFont(font)
                .SetFontSize(10)
                .SetFontColor(colorTexto));

        var rightHeader = new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetTextAlignment(TextAlignment.RIGHT)
            .Add(new Paragraph($"Factura #{factura.NumeroFactura}")
                .SetFont(fontBold)
                .SetFontSize(14)
                .SetFontColor(colorTexto))
            .Add(new Paragraph($"Fecha: {factura.FechaEmision:dd/MM/yyyy HH:mm}")
                .SetFont(font)
                .SetFontSize(10)
                .SetFontColor(colorTexto));

        headerTable.AddCell(leftHeader);
        headerTable.AddCell(rightHeader);

        document.Add(headerTable);
        document.Add(new Paragraph("\n"));

        // =========================
        // BLOQUE CLIENTE + ESTADO
        // =========================
        var infoTable = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 }))
            .UseAllAvailableWidth();

        var clienteCell = new Cell()
            .SetBackgroundColor(colorGris)
            .SetBorder(new SolidBorder(colorBorde, 1))
            .SetPadding(12)
            .Add(new Paragraph("DATOS DEL CLIENTE")
                .SetFont(fontBold)
                .SetFontSize(11)
                .SetFontColor(colorPrincipal))
            .Add(new Paragraph($"{factura.Usuario?.Nombre} {factura.Usuario?.Apellidos}")
                .SetFont(fontBold)
                .SetFontSize(12)
                .SetFontColor(colorTexto))
            .Add(new Paragraph($"Email: {factura.Usuario?.Email ?? "-"}")
                .SetFont(font)
                .SetFontSize(10)
                .SetFontColor(colorTexto));

        var estadoTexto = factura.Pagada == true ? "PAGADA" : "PENDIENTE";
        var estadoColor = factura.Pagada == true ? colorVerde : colorRojo;

        var estadoCell = new Cell()
            .SetBackgroundColor(ColorConstants.WHITE)
            .SetBorder(new SolidBorder(colorBorde, 1))
            .SetPadding(12)
            .SetTextAlignment(TextAlignment.CENTER)
            .Add(new Paragraph("ESTADO")
                .SetFont(fontBold)
                .SetFontSize(11)
                .SetFontColor(colorPrincipal))
            .Add(new Paragraph(estadoTexto)
                .SetFont(fontBold)
                .SetFontSize(14)
                .SetFontColor(estadoColor));

        infoTable.AddCell(clienteCell);
        infoTable.AddCell(estadoCell);

        document.Add(infoTable);
        document.Add(new Paragraph("\n"));

        // =========================
        // TABLA DE DETALLE
        // =========================
        var table = new Table(UnitValue.CreatePercentArray(new float[] { 46, 14, 20, 20 }))
            .UseAllAvailableWidth();
  
        void AddHeader(string text)
        {
            table.AddHeaderCell(
                new Cell()
                    .SetBackgroundColor(colorPrincipal)
                    .SetBorder(Border.NO_BORDER)
                    .SetPadding(8)
                    .Add(new Paragraph(text)
                        .SetFont(fontBold)
                        .SetFontSize(10)
                        .SetFontColor(ColorConstants.WHITE))
            );
        }

        AddHeader("Concepto");
        AddHeader("Cantidad");
        AddHeader("Precio unitario");
        AddHeader("Total");

        if (factura.FacturaDetalles != null)
        {
            foreach (var item in factura.FacturaDetalles)
            {
                table.AddCell(new Cell()
                    .SetBorder(new SolidBorder(colorBorde, 1))
                    .SetPadding(8)
                    .Add(new Paragraph(item.Concepto).SetFont(font).SetFontSize(10)));

                table.AddCell(new Cell()
                    .SetBorder(new SolidBorder(colorBorde, 1))
                    .SetPadding(8)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph(item.Cantidad.ToString()).SetFont(font).SetFontSize(10)));

                table.AddCell(new Cell()
                    .SetBorder(new SolidBorder(colorBorde, 1))
                    .SetPadding(8)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph($"{item.PrecioUnitario:0.00} €").SetFont(font).SetFontSize(10)));

                table.AddCell(new Cell()
                    .SetBorder(new SolidBorder(colorBorde, 1))
                    .SetPadding(8)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph($"{item.TotalLinea:0.00} €").SetFont(fontBold).SetFontSize(10)));
            }
        }

        document.Add(table);
        document.Add(new Paragraph("\n"));

        // =========================
        // RESUMEN TOTALES
        // =========================
        var totalesTable = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 }))
            .UseAllAvailableWidth();

        totalesTable.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER));

        var resumen = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
            .UseAllAvailableWidth();

        void AddResumen(string label, string valor, bool destacado = false)
        {
            resumen.AddCell(new Cell()
                .SetBorder(new SolidBorder(colorBorde, 1))
                .SetPadding(8)
                .Add(new Paragraph(label)
                    .SetFont(destacado ? fontBold : font)
                    .SetFontSize(10)));

            resumen.AddCell(new Cell()
                .SetBorder(new SolidBorder(colorBorde, 1))
                .SetPadding(8)
                .SetTextAlignment(TextAlignment.RIGHT)
                .Add(new Paragraph(valor)
                    .SetFont(destacado ? fontBold : font)
                    .SetFontSize(10)
                    .SetFontColor(destacado ? colorPrincipal : colorTexto)));
        }

        AddResumen("Subtotal", $"{factura.Subtotal:0.00} €");
        AddResumen("Impuestos", $"{factura.Impuestos:0.00} €");
        AddResumen("Total", $"{factura.Total:0.00} €", true);

        totalesTable.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER));

        totalesTable.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER)
            .Add(resumen));

        document.Add(totalesTable);
        document.Add(new Paragraph("\n"));

        // =========================
        // PAGO
        // =========================
        var pago = factura.Pagos?
            .Where(p => p.Activo == true)
            .OrderByDescending(p => p.FechaPago)
            .FirstOrDefault();

        if (pago != null)
        {
            var pagoTable = new Table(UnitValue.CreatePercentArray(new float[] { 35, 65 }))
                .UseAllAvailableWidth();

            pagoTable.AddCell(new Cell()
                .SetBackgroundColor(colorGris)
                .SetBorder(new SolidBorder(colorBorde, 1))
                .SetPadding(8)
                .Add(new Paragraph("Método de pago").SetFont(fontBold).SetFontSize(10)));

            pagoTable.AddCell(new Cell()
                .SetBorder(new SolidBorder(colorBorde, 1))
                .SetPadding(8)
                .Add(new Paragraph(pago.MetodoPago?.Nombre ?? "-").SetFont(font).SetFontSize(10)));

            pagoTable.AddCell(new Cell()
                .SetBackgroundColor(colorGris)
                .SetBorder(new SolidBorder(colorBorde, 1))
                .SetPadding(8)
                .Add(new Paragraph("Referencia").SetFont(fontBold).SetFontSize(10)));

            pagoTable.AddCell(new Cell()
                .SetBorder(new SolidBorder(colorBorde, 1))
                .SetPadding(8)
                .Add(new Paragraph(pago.ReferenciaExterna ?? "-").SetFont(font).SetFontSize(10)));

            pagoTable.AddCell(new Cell()
                .SetBackgroundColor(colorGris)
                .SetBorder(new SolidBorder(colorBorde, 1))
                .SetPadding(8)
                .Add(new Paragraph("Fecha de pago").SetFont(fontBold).SetFontSize(10)));

            pagoTable.AddCell(new Cell()
                .SetBorder(new SolidBorder(colorBorde, 1))
                .SetPadding(8)
                .Add(new Paragraph($"{pago.FechaPago:dd/MM/yyyy HH:mm}").SetFont(font).SetFontSize(10)));

            document.Add(pagoTable);
            document.Add(new Paragraph("\n"));
        }

        // =========================
        // PIE
        // =========================
        document.Add(new Paragraph("Gracias por confiar en FitControl.")
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFont(fontBold)
            .SetFontSize(10)
            .SetFontColor(colorPrincipal));

        document.Add(new Paragraph("Documento generado por el sistema FitControl.")
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFont(font)
            .SetFontSize(9)
            .SetFontColor(ColorConstants.GRAY));

        document.Close();
        return ms.ToArray();
    }
}