using FitControlWeb.Models.Entities;
using iText.Barcodes;
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
        var document = new Document(pdf, PageSize.A4.Rotate());

        document.SetMargins(24, 24, 24, 24);

        var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

        var colorPrincipal = new DeviceRgb(255, 122, 0);
        var colorTexto = new DeviceRgb(60, 60, 60);
        var colorSuave = new DeviceRgb(245, 245, 245);
        var colorBorde = new DeviceRgb(220, 220, 220);
        var colorVerde = new DeviceRgb(25, 135, 84);
        var colorRojo = new DeviceRgb(220, 53, 69);

        var fechaRegistro = factura.FechaEmision ?? DateTime.Now;
        var hashBase = $"{factura.NumeroFactura}|{fechaRegistro:yyyyMMddHHmmss}|{factura.Total:0.00}|{factura.UsuarioId}";
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(hashBase)));
        var hashCorto = hash[..24];
        var codigoRegistro = $"FC-{fechaRegistro:yyyyMMdd}-{factura.Id:D6}";
        var porcentajeIva = factura.Subtotal > 0
            ? Math.Round((factura.Impuestos / factura.Subtotal) * 100m, 2)
            : 0m;
        var estadoTexto = factura.Pagada == true ? "PAGADA" : "PENDIENTE";
        var estadoColor = factura.Pagada == true ? colorVerde : colorRojo;

        var qrPayload = string.Join(Environment.NewLine, new[]
        {
            "FITCONTROL VERIFICACION",
            $"REGISTRO={codigoRegistro}",
            $"FACTURA={factura.NumeroFactura}",
            $"FECHA={fechaRegistro:yyyy-MM-dd HH:mm:ss}",
            $"TOTAL={factura.Total:0.00}",
            $"CLIENTE={factura.UsuarioId}",
            $"HUELLA={hash}"
        });

        var qrCode = new BarcodeQRCode(qrPayload);
        var qrImage = new Image(qrCode.CreateFormXObject(colorTexto, pdf))
            .SetWidth(84)
            .SetHeight(84)
            .SetHorizontalAlignment(HorizontalAlignment.RIGHT);

        var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 58, 42 }))
            .UseAllAvailableWidth();

        headerTable.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetPadding(0)
            .Add(new Paragraph("FITCONTROL")
                .SetFont(fontBold)
                .SetFontSize(22)
                .SetFontColor(colorPrincipal))
            .Add(new Paragraph("Factura de cliente")
                .SetFont(font)
                .SetFontSize(10)
                .SetFontColor(colorTexto)
                .SetMarginTop(2)));

        headerTable.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetPadding(0)
            .SetTextAlignment(TextAlignment.RIGHT)
            .Add(new Paragraph($"Factura #{factura.NumeroFactura}")
                .SetFont(fontBold)
                .SetFontSize(14)
                .SetFontColor(colorTexto))
            .Add(new Paragraph($"Fecha: {factura.FechaEmision:dd/MM/yyyy HH:mm}")
                .SetFont(font)
                .SetFontSize(10)
                .SetFontColor(colorTexto)
                .SetMarginTop(2)));

        document.Add(headerTable);
        document.Add(new Paragraph().SetMarginTop(0).SetMarginBottom(10));

        var infoTable = new Table(UnitValue.CreatePercentArray(new float[] { 42, 18, 40 }))
            .UseAllAvailableWidth();

        infoTable.AddCell(new Cell()
            .SetBackgroundColor(colorSuave)
            .SetBorder(new SolidBorder(colorBorde, 1))
            .SetPadding(8)
            .Add(new Paragraph("DATOS DEL CLIENTE")
                .SetFont(fontBold)
                .SetFontSize(10)
                .SetFontColor(colorPrincipal)
                .SetMarginBottom(4))
            .Add(new Paragraph($"{factura.Usuario?.Nombre} {factura.Usuario?.Apellidos}")
                .SetFont(fontBold)
                .SetFontSize(11)
                .SetFontColor(colorTexto)
                .SetMarginBottom(2))
            .Add(new Paragraph($"Email: {factura.Usuario?.Email ?? "-"}")
                .SetFont(font)
                .SetFontSize(9)
                .SetFontColor(colorTexto)
                .SetMargin(0)));

        infoTable.AddCell(new Cell()
            .SetBorder(new SolidBorder(colorBorde, 1))
            .SetPadding(8)
            .SetTextAlignment(TextAlignment.CENTER)
            .Add(new Paragraph("ESTADO")
                .SetFont(fontBold)
                .SetFontSize(10)
                .SetFontColor(colorPrincipal)
                .SetMarginBottom(6))
            .Add(new Paragraph(estadoTexto)
                .SetFont(fontBold)
                .SetFontSize(13)
                .SetFontColor(estadoColor)
                .SetMargin(0)));

        var veriInfoTable = new Table(UnitValue.CreatePercentArray(new float[] { 68, 32 }))
            .UseAllAvailableWidth();

        veriInfoTable.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetPadding(0)
            .Add(new Paragraph("VERIFACTU")
                .SetFont(fontBold)
                .SetFontSize(10)
                .SetFontColor(colorPrincipal)
                .SetMarginBottom(4))
            .Add(new Paragraph($"Id registro: {codigoRegistro}")
                .SetFont(font)
                .SetFontSize(8.5f)
                .SetFontColor(colorTexto)
                .SetMarginBottom(2))
            .Add(new Paragraph($"Factura vinculada: {factura.NumeroFactura}")
                .SetFont(font)
                .SetFontSize(8.5f)
                .SetFontColor(colorTexto)
                .SetMarginBottom(2))
            .Add(new Paragraph($"Fecha registro: {fechaRegistro:dd/MM/yyyy HH:mm:ss}")
                .SetFont(font)
                .SetFontSize(8.5f)
                .SetFontColor(colorTexto)
                .SetMarginBottom(2))
            .Add(new Paragraph($"Huella resumida: {hashCorto}")
                .SetFont(font)
                .SetFontSize(8.5f)
                .SetFontColor(colorTexto)
                .SetMarginBottom(2))
            .Add(new Paragraph("QR y huella para comprobacion interna del documento")
                .SetFont(font)
                .SetFontSize(8)
                .SetFontColor(ColorConstants.GRAY)
                .SetMargin(0)));

        veriInfoTable.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetPaddingLeft(6)
            .SetTextAlignment(TextAlignment.RIGHT)
            .Add(qrImage));

        infoTable.AddCell(new Cell()
            .SetBackgroundColor(ColorConstants.WHITE)
            .SetBorder(new SolidBorder(colorBorde, 1))
            .SetPadding(8)
            .Add(veriInfoTable));

        document.Add(infoTable);
        document.Add(new Paragraph().SetMarginTop(0).SetMarginBottom(10));

        var table = new Table(UnitValue.CreatePercentArray(new float[] { 46, 14, 20, 20 }))
            .UseAllAvailableWidth();

        void AddHeader(string text)
        {
            table.AddHeaderCell(new Cell()
                .SetBackgroundColor(colorPrincipal)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(7)
                .Add(new Paragraph(text)
                    .SetFont(fontBold)
                    .SetFontSize(9.5f)
                    .SetFontColor(ColorConstants.WHITE)));
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
                    .SetPadding(6)
                    .Add(new Paragraph(item.Concepto)
                        .SetFont(font)
                        .SetFontSize(9.5f)
                        .SetMargin(0)));

                table.AddCell(new Cell()
                    .SetBorder(new SolidBorder(colorBorde, 1))
                    .SetPadding(6)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph(item.Cantidad.ToString())
                        .SetFont(font)
                        .SetFontSize(9.5f)
                        .SetMargin(0)));

                table.AddCell(new Cell()
                    .SetBorder(new SolidBorder(colorBorde, 1))
                    .SetPadding(6)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph($"{item.PrecioUnitario:0.00} €")
                        .SetFont(font)
                        .SetFontSize(9.5f)
                        .SetMargin(0)));

                table.AddCell(new Cell()
                    .SetBorder(new SolidBorder(colorBorde, 1))
                    .SetPadding(6)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph($"{item.TotalLinea:0.00} €")
                        .SetFont(fontBold)
                        .SetFontSize(9.5f)
                        .SetMargin(0)));
            }
        }

        document.Add(table);
        document.Add(new Paragraph().SetMarginTop(0).SetMarginBottom(10));

        var resumenWrap = new Table(UnitValue.CreatePercentArray(new float[] { 64, 36 }))
            .UseAllAvailableWidth();
        resumenWrap.AddCell(new Cell().SetBorder(Border.NO_BORDER));

        var resumen = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
            .UseAllAvailableWidth();

        void AddResumen(string label, string valor, bool destacado = false)
        {
            resumen.AddCell(new Cell()
                .SetBorder(new SolidBorder(colorBorde, 1))
                .SetPadding(6)
                .Add(new Paragraph(label)
                    .SetFont(destacado ? fontBold : font)
                    .SetFontSize(9.5f)
                    .SetMargin(0)));

            resumen.AddCell(new Cell()
                .SetBorder(new SolidBorder(colorBorde, 1))
                .SetPadding(6)
                .SetTextAlignment(TextAlignment.RIGHT)
                .Add(new Paragraph(valor)
                    .SetFont(destacado ? fontBold : font)
                    .SetFontSize(9.5f)
                    .SetFontColor(destacado ? colorPrincipal : colorTexto)
                    .SetMargin(0)));
        }

        AddResumen("Subtotal", $"{factura.Subtotal:0.00} €");
        AddResumen($"IVA ({porcentajeIva:0.##}%)", $"{factura.Impuestos:0.00} €");
        AddResumen("Total", $"{factura.Total:0.00} €", true);

        resumenWrap.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetPadding(0)
            .Add(resumen));

        document.Add(resumenWrap);

        var pago = factura.Pagos?
            .Where(p => p.Activo == true)
            .OrderByDescending(p => p.FechaPago)
            .FirstOrDefault();

        if (pago != null)
        {
            document.Add(new Paragraph().SetMarginTop(0).SetMarginBottom(10));

            var pagoTable = new Table(UnitValue.CreatePercentArray(new float[] { 22, 28, 22, 28 }))
                .UseAllAvailableWidth();

            pagoTable.AddCell(LabelCell("Metodo de pago", fontBold, colorSuave, colorBorde));
            pagoTable.AddCell(ValueCell(pago.MetodoPago?.Nombre ?? "-", font, colorBorde));
            pagoTable.AddCell(LabelCell("Fecha de pago", fontBold, colorSuave, colorBorde));
            pagoTable.AddCell(ValueCell($"{pago.FechaPago:dd/MM/yyyy HH:mm}", font, colorBorde));
            pagoTable.AddCell(LabelCell("Referencia", fontBold, colorSuave, colorBorde));
            pagoTable.AddCell(new Cell(1, 3)
                .SetBorder(new SolidBorder(colorBorde, 1))
                .SetPadding(6)
                .Add(new Paragraph(pago.ReferenciaExterna ?? "-")
                    .SetFont(font)
                    .SetFontSize(9.5f)
                    .SetMargin(0)));

            document.Add(pagoTable);
        }

        document.Add(new Paragraph().SetMarginTop(0).SetMarginBottom(10));

        document.Add(new Paragraph("Gracias por confiar en FitControl.")
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFont(fontBold)
            .SetFontSize(10)
            .SetFontColor(colorPrincipal)
            .SetMarginBottom(3));

        document.Add(new Paragraph("Documento generado por el sistema FitControl.")
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFont(font)
            .SetFontSize(8.5f)
            .SetFontColor(ColorConstants.GRAY)
            .SetMargin(0));

        document.Close();
        return ms.ToArray();
    }

    private static Cell LabelCell(string text, PdfFont fontBold, DeviceRgb bgColor, DeviceRgb borderColor)
    {
        return new Cell()
            .SetBackgroundColor(bgColor)
            .SetBorder(new SolidBorder(borderColor, 1))
            .SetPadding(6)
            .Add(new Paragraph(text)
                .SetFont(fontBold)
                .SetFontSize(9.5f)
                .SetMargin(0));
    }

    private static Cell ValueCell(string text, PdfFont font, DeviceRgb borderColor)
    {
        return new Cell()
            .SetBorder(new SolidBorder(borderColor, 1))
            .SetPadding(6)
            .Add(new Paragraph(text)
                .SetFont(font)
                .SetFontSize(9.5f)
                .SetMargin(0));
    }
}
