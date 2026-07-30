import JsBarcode from "jsbarcode";

/**
 * Thermal label sizes for HPRT N41-class printers.
 * N41 media width: 50–118 mm (40 mm is too narrow).
 */
export const QR_LABEL_MIN_WIDTH_MM = 50;

export const QR_LABEL_SIZES = [
  {
    id: "50x30",
    widthMm: 50,
    heightMm: 30,
    recommended: true,
    labelKey: "printQrLabelSize50x30",
  },
  {
    id: "50x40",
    widthMm: 50,
    heightMm: 40,
    recommended: true,
    labelKey: "printQrLabelSize50x40",
  },
  {
    id: "60x40",
    widthMm: 60,
    heightMm: 40,
    recommended: true,
    labelKey: "printQrLabelSize60x40",
  },
  {
    id: "70x40",
    widthMm: 70,
    heightMm: 40,
    recommended: false,
    labelKey: "printQrLabelSize70x40",
  },
  {
    id: "80x50",
    widthMm: 80,
    heightMm: 50,
    recommended: false,
    labelKey: "printQrLabelSize80x50",
  },
  {
    id: "100x150",
    widthMm: 100,
    heightMm: 150,
    recommended: false,
    labelKey: "printQrLabelSize100x150",
  },
];

export const DEFAULT_QR_LABEL_SIZE_ID = "50x30";

/** portrait = بالطول (current), landscape = بالعرض (swap page axes for label printers). */
export const QR_LABEL_ORIENTATIONS = [
  { id: "portrait", labelKey: "printQrLabelOrientationPortrait" },
  { id: "landscape", labelKey: "printQrLabelOrientationLandscape" },
];

export const DEFAULT_QR_LABEL_ORIENTATION = "landscape";

export function getQrLabelSize(sizeId) {
  return (
    QR_LABEL_SIZES.find((s) => s.id === sizeId) ||
    QR_LABEL_SIZES.find((s) => s.id === DEFAULT_QR_LABEL_SIZE_ID) ||
    QR_LABEL_SIZES[0]
  );
}

export function resolveLabelPageSize(size, orientation = DEFAULT_QR_LABEL_ORIENTATION) {
  const isLandscape = String(orientation || "").toLowerCase() === "landscape";
  if (isLandscape) {
    return {
      widthMm: size.heightMm,
      heightMm: size.widthMm,
      isLandscape: true,
    };
  }
  return {
    widthMm: size.widthMm,
    heightMm: size.heightMm,
    isLandscape: false,
  };
}

export function formatQrLabelSizeOption(size, t) {
  const mm = (t && t("mmUnit")) || "مم";
  const base = `${size.widthMm}×${size.heightMm} ${mm}`;
  if (size.recommended) {
    const tag = (t && t("printQrLabelRecommended")) || "موصى به";
    return `${base} — ${tag}`;
  }
  return base;
}

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text == null ? "" : String(text);
  return div.innerHTML;
}

/** Thermal/label printers commonly 203–300 DPI; use 300 for sharp bars when scaled in CSS. */
const BARCODE_PRINT_DPI = 300;

function mmToPx(mm, dpi = BARCODE_PRINT_DPI) {
  return Math.max(1, Math.round((Number(mm) / 25.4) * dpi));
}

/**
 * High-DPI CODE128 PNG so label printers do not upscale a tiny canvas (pixelation).
 * Pass targetWidthMm / targetHeightMm for the usable barcode area on the label.
 */
export function buildBarcodeDataUrl(code, options = {}) {
  const value = String(code || "").trim();
  if (!value) return "";

  const dpi = options.dpi ?? BARCODE_PRINT_DPI;
  const targetWidthMm = options.targetWidthMm ?? 46;
  const targetHeightMm = options.targetHeightMm ?? 14;
  const targetWidthPx = mmToPx(targetWidthMm, dpi);
  const barAreaHeightPx = mmToPx(Math.max(targetHeightMm * 0.72, 8), dpi);

  // CODE128 ~11 modules/char + quiet zones; pick module width so bars fill the label width.
  const estimatedModules = Math.max(value.length * 11 + 35, 60);
  const moduleWidth = Math.max(
    2,
    Math.min(6, Math.floor(targetWidthPx / estimatedModules))
  );
  const fontSize = Math.max(
    18,
    Math.round(mmToPx(Math.min(targetHeightMm * 0.22, 4.2), dpi))
  );
  const margin = Math.max(4, Math.round(moduleWidth * 2));

  const canvas = document.createElement("canvas");
  JsBarcode(canvas, value, {
    format: "CODE128",
    displayValue: true,
    fontSize: options.fontSize ?? fontSize,
    height: options.height ?? barAreaHeightPx,
    width: options.width ?? moduleWidth,
    margin: options.margin ?? margin,
    textMargin: options.textMargin ?? Math.max(2, Math.round(fontSize * 0.15)),
    lineColor: "#000",
    background: "#fff",
  });
  return canvas.toDataURL("image/png");
}

function buildLabelStyles(widthMm, heightMm, orientation = DEFAULT_QR_LABEL_ORIENTATION) {
  const page = resolveLabelPageSize(
    { widthMm, heightMm },
    orientation
  );
  const pageW = page.widthMm;
  const pageH = page.heightMm;
  const isTall = pageH >= 80;
  const nameSize = isTall ? 14 : pageW >= 60 ? 11 : 9;
  const priceSize = isTall ? 16 : pageW >= 60 ? 12 : 10;
  const padY = isTall ? 4 : 1.2;
  const padX = isTall ? 4 : 1.5;
  const barcodeMaxH = Math.max(pageH - (isTall ? 28 : 12), 10);

  return `
    <style>
      @page {
        size: ${pageW}mm ${pageH}mm;
        margin: 0 !important;
      }

      * {
        margin: 0;
        padding: 0;
        box-sizing: border-box;
        -webkit-print-color-adjust: exact !important;
        print-color-adjust: exact !important;
        color-adjust: exact !important;
      }

      html, body {
        width: ${pageW}mm;
        height: auto;
        margin: 0;
        padding: 0;
        background: #fff;
        color: #000;
        font-family: Arial, 'Segoe UI', Tahoma, sans-serif;
      }

      .qr-label {
        width: ${pageW}mm;
        height: ${pageH}mm;
        max-width: ${pageW}mm;
        max-height: ${pageH}mm;
        padding: ${padY}mm ${padX}mm;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        text-align: center;
        overflow: hidden;
        page-break-after: always;
        break-after: page;
        page-break-inside: avoid;
      }

      .qr-label:last-child {
        page-break-after: auto;
        break-after: auto;
      }

      .qr-label-name {
        width: 100%;
        font-size: ${nameSize}px;
        font-weight: 700;
        line-height: 1.15;
        max-height: 2.4em;
        overflow: hidden;
        margin-bottom: 0.8mm;
        word-break: break-word;
      }

      .qr-label-barcode {
        display: block;
        width: 96%;
        max-width: 96%;
        max-height: ${barcodeMaxH}mm;
        height: auto;
        object-fit: contain;
        image-rendering: crisp-edges;
        image-rendering: pixelated;
        -ms-interpolation-mode: nearest-neighbor;
      }

      .qr-label-price {
        width: 100%;
        font-size: ${priceSize}px;
        font-weight: 800;
        margin-top: 0.8mm;
        line-height: 1.1;
      }

      @media print {
        html, body {
          width: ${pageW}mm !important;
          margin: 0 !important;
        }
        .qr-label {
          width: ${pageW}mm !important;
          height: ${pageH}mm !important;
        }
      }
    </style>
  `;
}

/**
 * Build a print document for QR/barcode label printers (one label per page).
 * @param {{ code: string, name?: string, priceText?: string }} item
 * @param {{ copies?: number, sizeId?: string, orientation?: 'portrait'|'landscape' }} options
 */
export function buildQrLabelPrintDocument(item, options = {}) {
  const copies = Math.min(Math.max(Number(options.copies) || 1, 1), 200);
  const size = getQrLabelSize(options.sizeId);
  const orientation = options.orientation || DEFAULT_QR_LABEL_ORIENTATION;
  const page = resolveLabelPageSize(size, orientation);
  const wide = page.widthMm >= 60;
  const tall = page.heightMm >= 80;

  // Leave room for name/price; generate barcode at ~300 DPI for the printable area.
  const barcodeWidthMm = Math.max(page.widthMm - (wide ? 6 : 4), 28);
  const barcodeHeightMm = Math.max(
    Math.min(page.heightMm - (tall ? 32 : page.heightMm >= 40 ? 16 : 12), tall ? 55 : 22),
    10
  );
  const barcodeUrl = buildBarcodeDataUrl(item.code, {
    targetWidthMm: barcodeWidthMm,
    targetHeightMm: barcodeHeightMm,
    dpi: BARCODE_PRINT_DPI,
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
  ${buildLabelStyles(size.widthMm, size.heightMm, orientation)}
</head>
<body>
  ${labels}
</body>
</html>`;
}

export function printQrLabels(item, options = {}) {
  const html = buildQrLabelPrintDocument(item, options);
  const printWindow = window.open("", "_blank", "width=480,height=720");
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
    }, 350);
    return true;
  }

  printWindow.document.open();
  printWindow.document.write(html);
  printWindow.document.close();
  setTimeout(() => {
    printWindow.focus();
    printWindow.print();
    setTimeout(() => printWindow.close(), 500);
  }, 400);
  return true;
}
