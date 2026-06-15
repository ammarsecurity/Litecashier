namespace PrintServer.Services;

/// <summary>
/// Receipt CSS aligned with restaurant_front/src/utils/receiptPrint.js
/// </summary>
public static class ReceiptPrintStyles
{
    public const string CairoFontLink = "<link rel=\"stylesheet\" href=\"http://127.0.0.1:5000/fonts/cairo.css\">";

    public const string CssBlock = """
        <style>
          @page { size: 72mm auto; margin: 2mm 4mm; }
          * { margin: 0; padding: 0; box-sizing: border-box; }
          body {
            font-family: 'Cairo', 'Arial', sans-serif;
            direction: rtl;
            font-size: 11px;
            line-height: 1.35;
            color: #000;
            background: #fff;
            padding: 3mm 3mm 3mm 5mm;
            width: 72mm;
            max-width: 72mm;
            margin: 0 auto;
          }
          .bill-container { width: 100%; max-width: 100%; margin: 0 auto; padding: 0 2mm 0 3mm; }
          .bill-header {
            text-align: center;
            margin-bottom: 8px;
            padding-bottom: 8px;
            border-bottom: 1px dashed #000;
          }
          .bill-logo-img { max-width: 50px; height: auto; margin-bottom: 4px; }
          .bill-store-name { font-size: 16px; font-weight: 800; margin: 4px 0 2px 0; color: #000; }
          .bill-store-subtitle { font-size: 9px; color: #666; margin: 0; }
          .bill-info-section { margin: 8px 0; padding: 0 1mm 0 2mm; font-size: 10px; }
          .bill-info-row { display: flex; flex-direction: row; justify-content: space-between; align-items: flex-start; gap: 6px; margin-bottom: 4px; padding: 0 1px; }
          .bill-info-label { flex: 0 0 44%; max-width: 44%; font-weight: 600; line-height: 1.35; text-align: right; }
          .bill-info-value { flex: 1 1 auto; min-width: 0; font-weight: 400; text-align: right; padding-left: 2mm; word-break: break-word; overflow-wrap: anywhere; line-height: 1.35; }
          .bill-barcode-section { text-align: center; margin: 8px 0; padding: 4px 0; }
          .bill-barcode-img { max-width: 100%; height: auto; display: block; margin: 0 auto; }
          .bill-divider { border-top: 1px dashed #000; margin: 8px 0; }
          .bill-items-section { margin: 8px 0; padding: 0 1mm 0 2mm; overflow: hidden; }
          .bill-items-table { width: 100%; table-layout: fixed; border-collapse: collapse; font-size: 9px; }
          .bill-items-table thead { border-bottom: 1px solid #000; }
          .bill-items-table th { padding: 4px 3px; text-align: right; font-weight: 700; font-size: 8px; line-height: 1.2; word-break: break-word; }
          .bill-item-name-col { width: 32%; }
          .bill-item-qty-col { width: 13%; text-align: center; }
          .bill-item-price-col { width: 22%; text-align: center; }
          .bill-item-total-col { width: 33%; text-align: right; padding-left: 2mm; }
          .bill-items-table td { padding: 4px 3px; vertical-align: top; line-height: 1.25; }
          .bill-item-name { font-weight: 500; word-break: break-word; }
          .bill-discount-badge { display: block; font-size: 7px; color: #dc2626; font-weight: 600; margin-top: 2px; }
          .bill-item-qty { text-align: center; font-weight: 600; }
          .bill-item-price { text-align: center; font-size: 8px; word-break: break-word; }
          .bill-price-discounted { display: block; }
          .bill-original-price { display: block; text-decoration: line-through; color: #999; font-size: 8px; }
          .bill-discount-price { display: block; color: #dc2626; font-weight: 600; }
          .bill-item-total { text-align: right; font-weight: 700; font-size: 8px; padding-left: 2mm; word-break: break-word; overflow-wrap: anywhere; }
          .bill-summary-section { margin: 8px 0; padding: 0 1mm 0 2mm; font-size: 11px; }
          .bill-summary-row { display: flex; flex-direction: row; justify-content: space-between; align-items: flex-start; gap: 6px; margin-bottom: 4px; padding: 0 1px; }
          .bill-summary-label { flex: 0 0 44%; max-width: 44%; font-weight: 600; line-height: 1.35; text-align: right; }
          .bill-summary-value { flex: 1 1 auto; min-width: 0; font-weight: 400; text-align: right; padding-left: 2mm; word-break: break-word; overflow-wrap: anywhere; line-height: 1.35; }
          .bill-summary-total { border-top: 1px solid #000; padding-top: 4px; margin-top: 4px; font-size: 12px; }
          .bill-summary-total .bill-summary-label { font-weight: 700; font-size: 13px; }
          .bill-summary-total .bill-summary-value { font-weight: 800; font-size: 13px; }
          .bill-notes-section { margin-top: 12px; padding-top: 8px; }
          .bill-notes-content { margin-bottom: 8px; padding: 6px 0; }
          .bill-notes-label { font-weight: 600; font-size: 10px; margin-bottom: 4px; color: #000; }
          .bill-notes-text { font-size: 10px; color: #333; line-height: 1.4; word-wrap: break-word; }
          .bill-footer { text-align: center; margin-top: 12px; padding-top: 8px; border-top: 1px dashed #000; }
          .bill-footer-text { font-size: 9px; margin: 2px 0; color: #666; }
          @media print {
            body { width: 72mm !important; max-width: 72mm !important; padding: 3mm 3mm 3mm 5mm !important; }
            .bill-container { width: 100% !important; max-width: 100% !important; padding: 0 2mm 0 3mm !important; }
            .bill-info-section, .bill-items-section, .bill-summary-section { padding: 0 1mm 0 2mm !important; }
          }
        </style>
        """;

    public static string EnsureFullDocument(string htmlContent, string title = "Receipt")
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return htmlContent;

        var trimmed = htmlContent.TrimStart();
        if (trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
        {
            if (!htmlContent.Contains("<style", StringComparison.OrdinalIgnoreCase))
            {
                return htmlContent.Replace(
                    "<head>",
                    "<head>" + CairoFontLink + CssBlock,
                    StringComparison.OrdinalIgnoreCase);
            }
            return htmlContent;
        }

        var safeTitle = System.Net.WebUtility.HtmlEncode(title);
        return "<!DOCTYPE html>\n"
            + "<html dir=\"rtl\" lang=\"ar\">\n"
            + "<head>\n"
            + "  <meta charset=\"UTF-8\">\n"
            + "  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n"
            + "  <title>" + safeTitle + "</title>\n"
            + "  " + CairoFontLink + "\n"
            + CssBlock + "\n"
            + "</head>\n"
            + "<body>\n"
            + htmlContent + "\n"
            + "</body>\n"
            + "</html>";
    }
}
