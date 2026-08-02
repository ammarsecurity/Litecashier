import { HTTP } from "@/http/api.js";
import { resolvePrintServerUrl } from "@/utils/apiBase.js";

function money(v) {
  const n = Number(v);
  if (!Number.isFinite(n)) return "0";
  return n.toLocaleString("en-US", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  });
}

/**
 * إيصال تسليم راتب بأسلوب طابعة POS (عرض ضيق، monospace).
 */
export function buildSalaryHandoverReceiptHtml({ line, run, locale = "ar" }) {
  const emp = line?.employee || line?.Employee || {};
  const name = emp.name || emp.Name || `#${line?.employeeId || line?.EmployeeId || ""}`;
  const job = emp.jobTitle || emp.JobTitle || "";
  const year = run?.year ?? run?.Year ?? "";
  const month = String(run?.month ?? run?.Month ?? "").padStart(2, "0");
  const period = `${year}/${month}`;
  const handedAt = line?.handedOverAt || line?.HandedOverAt || new Date().toISOString();
  const dateStr = String(handedAt).slice(0, 19).replace("T", " ");
  const rtl = locale === "ar";
  const dir = rtl ? "rtl" : "ltr";
  const t = rtl
    ? {
        title: "إيصال تسليم راتب",
        employee: "الموظف",
        job: "المسمى",
        period: "الفترة",
        base: "الأساسي",
        overtime: "إضافي",
        deduction: "خصم",
        absence: "غياب",
        advance: "خصم سلف",
        net: "صافي المستلم",
        date: "تاريخ التسليم",
        thanks: "تم الاستلام",
        sign: "توقيع المستلم",
      }
    : {
        title: "Salary Handover Receipt",
        employee: "Employee",
        job: "Job title",
        period: "Period",
        base: "Base",
        overtime: "Overtime",
        deduction: "Deduction",
        absence: "Absence",
        advance: "Advance",
        net: "Net paid",
        date: "Handover date",
        thanks: "Received",
        sign: "Recipient signature",
      };

  const row = (label, value, bold = false) =>
    `<div style="display:flex;justify-content:space-between;gap:8px;padding:3px 0;${
      bold ? "font-weight:700;font-size:15px;margin-top:6px;border-top:1px dashed #333;padding-top:8px;" : ""
    }"><span>${label}</span><span>${value}</span></div>`;

  return `<!DOCTYPE html><html dir="${dir}"><head><meta charset="utf-8" />
<style>
  body{font-family:'Courier New',Courier,monospace;width:280px;margin:0 auto;padding:10px;color:#000;background:#fff;font-size:13px;}
  .center{text-align:center}
  .muted{color:#444;font-size:11px}
  hr{border:none;border-top:1px dashed #333;margin:8px 0}
</style></head><body>
  <div class="center"><strong style="font-size:16px">${t.title}</strong></div>
  <div class="center muted">${period}</div>
  <hr/>
  ${row(t.employee, name)}
  ${job ? row(t.job, job) : ""}
  ${row(t.period, period)}
  <hr/>
  ${row(t.base, money(line?.baseAmount ?? line?.BaseAmount))}
  ${row(t.overtime, money(line?.overtimeAmount ?? line?.OvertimeAmount))}
  ${row(t.deduction, money(line?.deductionAmount ?? line?.DeductionAmount))}
  ${row(t.absence, money(line?.absenceAmount ?? line?.AbsenceAmount))}
  ${row(t.advance, money(line?.advanceDeducted ?? line?.AdvanceDeducted))}
  ${row(t.net, money(line?.netAmount ?? line?.NetAmount), true)}
  <hr/>
  <div class="muted">${t.date}: ${dateStr}</div>
  <div class="center" style="margin-top:18px">${t.thanks}</div>
  <div class="center muted" style="margin-top:22px">_______________<br/>${t.sign}</div>
</body></html>`;
}

async function resolveMainPrinterId() {
  const res = await HTTP.get("Printers");
  const list = res.data?.data || res.data || [];
  const arr = Array.isArray(list) ? list : list.items || [];
  const main =
    arr.find((p) => (p.isMain ?? p.IsMain) && (p.isActive ?? p.IsActive) !== false) ||
    arr.find((p) => (p.isActive ?? p.IsActive) !== false);
  return main?.id ?? main?.Id ?? null;
}

export async function printSalaryHandoverReceipt({ line, run, locale = "ar" }) {
  const htmlContent = buildSalaryHandoverReceiptHtml({ line, run, locale });
  const printerId = await resolveMainPrinterId();

  if (printerId) {
    const response = await HTTP.post(`Printers/${printerId}/print`, {
      htmlContent,
      copies: 1,
    });
    if (response.data && !response.data.errorStatus) {
      return { ok: true, via: "api" };
    }
  }

  const printServer = resolvePrintServerUrl();
  try {
    const response = await fetch(`${printServer}/print`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ htmlContent, copies: 1 }),
    });
    if (response.ok) return { ok: true, via: "printServer" };
  } catch (_) {
    /* fall through */
  }

  // Browser fallback
  const w = window.open("", "_blank", "width=360,height=640");
  if (w) {
    w.document.write(htmlContent);
    w.document.close();
    w.focus();
    w.print();
    return { ok: true, via: "browser" };
  }
  return { ok: false };
}
