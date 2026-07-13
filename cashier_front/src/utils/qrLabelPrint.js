import JsBarcode from "jsbarcode";

/** Common thermal QR / barcode label sizes (not A4). */
export const QR_LABEL_SIZES = [
  { id: "40x30", widthMm: 40, heightMm: 30 },
  { id: "50x30", widthMm: 50, heightMm: 30 },
  { id: "40x25", widthMm: 40, heightMm: 25 },
  { id: "60x40", widthMm: 60, heightMm: 40 },
];

export function getQrLabelSize(sizeId) {
  return (
    QR_LABEL_SIZES.find((s) => s.id === sizeId) || QR_LABEL_SIZES[0]
  );
}

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text == null ? "" : String(text);
  return div.innerHTML;
}

export function buildBarcodeDataUrl(code, options = {}) {
  const value = String(code || "").trim();
  if (!value) return "";

  const canvas = document.createElement("canvas");
  JsBarcode(canvas, value, {
    format: "CODE128",
    displayValue: true,
    fontSize: options.fontSize ?? 11,
    height: options.height ?? 38,
    width: options.width ?? 1.35,
    margin: options.margin ?? 2,
    lineColor: "#000",
    background: "#fff",
  });
  return canvas.toDataURL("image/png");
}

function buildLabelStyles(widthMm, heightMm) {
  return `
    <style>
      @page {
        size: ${widthMm}mm ${heightMm}mm;
        margin: 0;
      }

      * {
        margin: 0;
        padding: 0;
        box-sizing: border-box;
        -webkit-print-color-adjust: exact;
        print-color-adjust: exact;
      }

      html, body {
        width: ${widthMm}mm;
        margin: 0;
        padding: 0;
        background: #fff;
        color: #000;
        font-family: Arial, 'Segoe UI', Tahoma, sans-serif;
      }

      .qr-label {
        width: ${widthMm}mm;
        height: ${heightMm}mm;
        padding: 1.5mm 2mm;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        text-align: center;
        overflow: hidden;
        page-break-after: always;
        break-after: page;
      }

      .qr-label:last-child {
        page-break-after: auto;
        break-after: auto;
      }

      .qr-label-name {
        width: 100%;
        font-size: 9px;
        font-weight: 700;
        line-height: 1.15;
        max-height: 2.4em;
        overflow: hidden;
        margin-bottom: 1mm;
        word-break: break-word;
      }

      .qr-label-barcode {
        display: block;
        max-width: 100%;
        max-height: ${Math.max(heightMm - 14, 12)}mm;
        height: auto;
        object-fit: contain;
      }

      .qr-label-price {
        width: 100%;
        font-size: 10px;
        font-weight: 800;
        margin-top: 1mm;
        line-height: 1.1;
      }

      @media print {
        html, body {
          width: ${widthMm}mm;
        }
        .qr-label {
          width: ${widthMm}mm;
          height: ${heightMm}mm;
        }
      }
    </style>
  `;
}

/**
 * Build a print document for QR/barcode label printers (one label per page).
 * @param {{ code: string, name?: string, priceText?: string }} item
 * @param {{ copies?: number, sizeId?: string }} options
 */
export function buildQrLabelPrintDocument(item, options = {}) {
  const copies = Math.min(Math.max(Number(options.copies) || 1, 1), 200);
  const size = getQrLabelSize(options.sizeId);
  const barcodeUrl = buildBarcodeDataUrl(item.code, {
    height: size.heightMm >= 35 ? 48 : 34,
    width: size.widthMm >= 50 ? 1.5 : 1.25,
    fontSize: size.widthMm >= 50 ? 12 : 10,
  });

  const name = escapeHtml(item.name || "");
  const priceText = item.priceText ? escapeHtml(item.priceText) : "";
  const title = escapeHtml(
    `${item.name || item.code || "label"}`.slice(0, 80)
  );

  const labels = Array.from({ length: copies }, () => {
    return `
      <div class="qr-label">
        ${name ? `<div class="qr-label-name">${name}</div>` : ""}
        ${
          barcodeUrl
            ? `<img class="qr-label-barcode" src="${barcodeUrl}" alt="${escapeHtml(
                String(item.code || "")
              )}" />`
            : `<div class="qr-label-name">${escapeHtml(String(item.code || ""))}</div>`
        }
        ${priceText ? `<div class="qr-label-price">${priceText}</div>` : ""}
      </div>
    `;
  }).join("");

  return `<!DOCTYPE html>
<html lang="ar" dir="rtl">
<head>
  <meta charset="UTF-8" />
  <title>${title}</title>
  ${buildLabelStyles(size.widthMm, size.heightMm)}
</head>
<body>
  ${labels}
</body>
</html>`;
}

export function printQrLabels(item, options = {}) {
  const html = buildQrLabelPrintDocument(item, options);
  const printWindow = window.open("", "_blank", "width=420,height=640");
  if (!printWindow) {
    const iframe = document.createElement("iframe");
    iframe.style.cssText =
      "position:fixed;right:0;bottom:0;width:0;height:0;border:0;";
    document.body.appendChild(iframe);
    const doc = iframe.contentWindow?.document;
    if (!doc) {
      document.body.removeChild(iframe);
      return false;
    }
    doc.open();
    doc.write(html);
    doc.close();
    setTimeout(() => {
      iframe.contentWindow?.focus();
      iframe.contentWindow?.print();
      setTimeout(() => document.body.removeChild(iframe), 600);
    }, 300);
    return true;
  }

  printWindow.document.open();
  printWindow.document.write(html);
  printWindow.document.close();
  setTimeout(() => {
    printWindow.focus();
    printWindow.print();
    setTimeout(() => printWindow.close(), 400);
  }, 350);
  return true;
}
