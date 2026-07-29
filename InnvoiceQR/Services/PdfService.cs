using InnvoiceQR.Entities;
using iText.IO.Font;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using QRCoder;
using System.IO;

namespace InnvoiceQR.Services
{
    /// <summary>
    /// خدمة توليد فواتير PDF بتنسيق RTL كامل (من اليمين لليسار).
    /// يجب استخدام خط Amiri الذي يدعم Arabic Presentation Forms.
    /// </summary>
    public class PdfService
    {
        private const string FontRegularPath = "wwwroot/fonts/Amiri-Regular.ttf";
        private const string FontBoldPath = "wwwroot/fonts/Amiri-Bold.ttf";

        private static readonly Color ColorPrimary = new DeviceRgb(0x1A, 0x56, 0x76);
        private static readonly Color ColorHeader = new DeviceRgb(0x24, 0x6E, 0x96);
        private static readonly Color ColorRowAlt = new DeviceRgb(0xF0, 0xF7, 0xFB);
        private static readonly Color ColorBorder = new DeviceRgb(0xCC, 0xDD, 0xE8);
        private static readonly Color ColorText = new DeviceRgb(0x1C, 0x1C, 0x1C);
        private static readonly Color ColorMuted = new DeviceRgb(0x55, 0x65, 0x70);
        private static readonly Color ColorTotalBg = new DeviceRgb(0x1A, 0x56, 0x76);
        private static readonly Color ColorWhite = ColorConstants.WHITE;

        public byte[] Generate(Invoice invoice)
        {
            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms, new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf, PageSize.A4);

            doc.SetMargins(36, 40, 36, 40);

            var fontRegular = PdfFontFactory.CreateFont(FontRegularPath, PdfEncodings.IDENTITY_H);
            var fontBold = PdfFontFactory.CreateFont(FontBoldPath, PdfEncodings.IDENTITY_H);

            doc.SetFont(fontRegular).SetFontColor(ColorText);

            AddHeader(doc, invoice, fontBold, fontRegular);
            AddDivider(doc);
            AddInfoSection(doc, invoice, fontRegular, fontBold);
            AddDivider(doc);
            AddItemsTable(doc, invoice, fontRegular, fontBold);
            AddTotalsSection(doc, invoice, fontRegular, fontBold);
            AddQrCode(doc, invoice, fontRegular);
            AddFooter(doc, fontRegular);

            doc.Close();
            return ms.ToArray();
        }

        private void AddHeader(Document doc, Invoice invoice, PdfFont fontBold, PdfFont fontRegular)
        {
            var table = new Table(new float[] { 1f, 1f })
                .UseAllAvailableWidth()
                .SetBorder(Border.NO_BORDER);

            // عنوان الفاتورة
            var titleCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT);

            titleCell.Add(new Paragraph(F("فاتورة ضريبية مبسطة"))
                .SetFont(fontBold).SetFontSize(30)
                .SetFontColor(ColorPrimary)
                .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                .SetVerticalAlignment(VerticalAlignment.TOP)
                );

           

            // اسم المنشأة
            var nameCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT).SetVerticalAlignment(VerticalAlignment.TOP)
                .SetHorizontalAlignment(HorizontalAlignment.RIGHT);

            nameCell.Add(new Paragraph(F(invoice.SellerName))
                .SetFont(fontBold).SetFontSize(14)
                .SetFontColor(ColorHeader)
                .SetTextAlignment(TextAlignment.RIGHT));

            nameCell.Add(new Paragraph(F($"  الرقم الضريبي   {invoice.VatNumber} : "))
                .SetFont(fontRegular).SetFontSize(9)
                .SetFontColor(ColorMuted)
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(titleCell);
            table.AddCell(nameCell);
            doc.Add(table);
        }

        private void AddInfoSection(Document doc, Invoice invoice, PdfFont fontRegular, PdfFont fontBold)
        {
            var table = new Table(new float[] { 1f, 1f })
                .UseAllAvailableWidth()
                .SetBorder(Border.NO_BORDER)
                .SetMarginBottom(4);
            var inviceNum = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT);
            AddInfoPair(inviceNum, invoice.InvoiceNumber +": ", "  رقم الفاتورة  ", fontBold, fontRegular);
            var dateCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT);
            AddInfoPair(dateCell, invoice.IssueDate.ToString("yyyy/MM/dd  HH:mm")+": ", " تاريخ الإصدار  ", fontBold, fontRegular);

            var buyerCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT);
            AddInfoPair(buyerCell, F(invoice.BuyerName) + ": ", "  اسم العميل  ", fontBold, fontRegular);
            var buyerVatCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT);
            AddInfoPair(buyerVatCell, invoice.BuyerVatNumber + ": ", "  الرقم الضريبي للعميل  ", fontBold, fontRegular);

            table.AddCell(buyerVatCell);
            table.AddCell(buyerCell);
            table.AddCell(dateCell);
            table.AddCell(inviceNum);
            doc.Add(table);
        }

        private void AddInfoPair(Cell container, string label, string value,
                                 PdfFont fontBold, PdfFont fontRegular)
        {
            var p = new Paragraph()
                .SetMultipliedLeading(1.5f)
                .SetTextAlignment(TextAlignment.RIGHT);

            p.Add(new Text(F(label) + "  ").SetFont(fontBold).SetFontSize(9).SetFontColor(ColorMuted));
            p.Add(new Text(F(value)).SetFont(fontRegular).SetFontSize(10).SetFontColor(ColorText));
            container.Add(p);
        }

        private void AddItemsTable(Document doc, Invoice invoice, PdfFont fontRegular, PdfFont fontBold)
        {
            float[] widths = { 260f, 60f, 90f, 90f };
            var table = new Table(widths).UseAllAvailableWidth();

            string[] headers = { "الاجمالي", "الكمية", "سعر الوحدة", "الوصف" };
            foreach (string h in headers)
                table.AddHeaderCell(MakeHeaderCell(h, fontBold));

            var border = new SolidBorder(ColorBorder, 0.5f);
            table.AddCell(MakeDataCell(invoice.SubTotal.ToString("F2"), false, ColorRowAlt, border, fontRegular));
            table.AddCell(MakeDataCell(invoice.Quantity.ToString(), false, ColorRowAlt, border, fontRegular));
            table.AddCell(MakeDataCell(invoice.UnitPrice.ToString("F2"), false, ColorRowAlt, border, fontRegular));
            table.AddCell(MakeDataCell(invoice.ProductDescription, true, ColorRowAlt, border, fontRegular));

            doc.Add(table);
        }

        private Cell MakeHeaderCell(string text, PdfFont fontBold)
        {
            return new Cell()
                .SetBackgroundColor(ColorHeader)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(8)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph(F(text))
                    .SetFont(fontBold).SetFontSize(10)
                    .SetFontColor(ColorWhite)
                    .SetTextAlignment(TextAlignment.CENTER));
        }

        private Cell MakeDataCell(string text, bool isArabic, Color bg,
                                  Border border, PdfFont font)
        {
            string content = isArabic ? F(text) : text;
            return new Cell()
                .SetBackgroundColor(bg)
                .SetBorderTop(Border.NO_BORDER)
                .SetBorderBottom(border)
                .SetBorderLeft(border)
                .SetBorderRight(border)
                .SetPadding(7)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph(content)
                    .SetFont(font).SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER));
        }

        private void AddTotalsSection(Document doc, Invoice invoice,
                                      PdfFont fontRegular, PdfFont fontBold)
        {
            var table = new Table(new float[] { 2f, 1f })
                .UseAllAvailableWidth()
                .SetBorder(Border.NO_BORDER)
                .SetMarginTop(0).SetMarginBottom(12);

            AddTotalRow(table, $"{invoice.VatAmount:F2}", F("ضريبة القيمة المضافة (15%)"), fontRegular);
            AddTotalRowHighlighted(table, $"{invoice.TotalAmount:F2}", F("الإجمالي شامل الضريبة"), fontBold);

            doc.Add(table);
        }

        private void AddTotalRow(Table table, string label, string value, PdfFont font)
        {
            var border = new SolidBorder(ColorBorder, 0.5f);

            table.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            var p = new Paragraph()
                .SetTextAlignment(TextAlignment.RIGHT);
            p.Add(new Text(label + "   ").SetFont(font).SetFontSize(10).SetFontColor(ColorMuted));
            p.Add(new Text(value).SetFont(font).SetFontSize(10));

            table.AddCell(new Cell()
                .SetBorderTop(border).SetBorderBottom(border)
                .SetBorderLeft(border).SetBorderRight(border)
                .SetPadding(7).Add(p));
        }

        private void AddTotalRowHighlighted(Table table, string label, string value, PdfFont font)
        {
            table.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            var p = new Paragraph()
                .SetTextAlignment(TextAlignment.RIGHT);
            p.Add(new Text(label + "   ").SetFont(font).SetFontSize(12).SetFontColor(ColorWhite));
            p.Add(new Text(value).SetFont(font).SetFontSize(12).SetFontColor(ColorWhite));

            table.AddCell(new Cell()
                .SetBackgroundColor(ColorTotalBg)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(9)
                .Add(p));
        }

        private void AddQrCode(Document doc, Invoice invoice, PdfFont fontRegular)
        {
            using var qrGen = new QRCodeGenerator();
            var qrData = qrGen.CreateQrCode(invoice.QrCodeBase64, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            var qrBytes = qrCode.GetGraphic(20);

            var image = new Image(ImageDataFactory.Create(qrBytes))
                .SetWidth(110)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetMarginTop(6).SetMarginBottom(4);

            doc.Add(image);

            doc.Add(new Paragraph(F("امسح الرمز للتحقق من الفاتورة"))
                .SetFont(fontRegular).SetFontSize(8)
                .SetFontColor(ColorMuted)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(8));
        }

        private void AddFooter(Document doc, PdfFont fontRegular)
        {
            AddDivider(doc);
            doc.Add(new Paragraph(F("شكراً لتعاملكم معنا"))
                .SetFont(fontRegular).SetFontSize(9)
                .SetFontColor(ColorMuted)
                .SetTextAlignment(TextAlignment.CENTER));
        }

        private void AddDivider(Document doc)
        {
            var line = new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine(1f));
            line.SetStrokeColor(ColorBorder).SetMarginTop(8).SetMarginBottom(8);
            doc.Add(line);
        }

        private static string F(string text) => ArabicFixer.Fix(text);
    }
}