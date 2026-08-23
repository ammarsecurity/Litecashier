/**
 * Section keys match navItems.js item.name and backend SectionDefinitions.AssignableSectionKeys.
 */

export const ASSIGNABLE_SECTION_KEYS = [
  "pos",
  "category",
  "items",
  "shortcutItems",
  "priceReader",
  "reports",
  "endOfDayReport",
  "expenses",
  "inventory",
  "warehouses",
  "printServer",
  "paymentDevices",
  "cardPayments",
  "employees",
  "payroll",
  "customers",
  "deferredPayments",
  "stockAlerts",
  "stockReturns",
  "auditLog",
  "users",
  "dashboard",
];

/** Route path → section key (first match wins). */
const ROUTE_SECTION_MAP = [
  { prefix: "/pos", key: "pos" },
  { prefix: "/reports", key: "reports" },
  { prefix: "/end-of-day-report", key: "endOfDayReport" },
  { prefix: "/inventory", key: "inventory" },
  { prefix: "/warehouses", key: "warehouses" },
  { prefix: "/expenses", key: "expenses" },
  { prefix: "/employees", key: "employees" },
  { prefix: "/payroll", key: "payroll" },
  { prefix: "/customers", key: "customers" },
  { prefix: "/deferred-payments", key: "deferredPayments" },
  { prefix: "/stock-alerts", key: "stockAlerts" },
  { prefix: "/stock-returns", key: "stockReturns" },
  { prefix: "/audit-log", key: "auditLog" },
  { prefix: "/print-server", key: "printServer" },
  { prefix: "/payment-devices", key: "paymentDevices" },
  { prefix: "/card-payments", key: "cardPayments" },
  { prefix: "/priceReader", key: "priceReader" },
  { prefix: "/category", key: "category" },
  { prefix: "/shortcut-items", key: "shortcutItems" },
  { prefix: "/items", key: "items" },
  { prefix: "/users", key: "users" },
  { prefix: "/dashboard", key: "dashboard" },
];

const STORAGE_KEY = "allowedSections";

export function routeToSectionKey(path) {
  if (!path) return null;
  const p = String(path).split("?")[0];
  const hit = ROUTE_SECTION_MAP.find(
    (r) => p === r.prefix || p.startsWith(r.prefix + "/")
  );
  return hit ? hit.key : null;
}

export function parseAllowedSectionsJson(json) {
  if (!json) return [];
  try {
    if (typeof json === "string") {
      const trimmed = json.trim();
      if (!trimmed) return [];
      const parsed = JSON.parse(trimmed);
      return Array.isArray(parsed) ? parsed.map(String) : [];
    }
    if (Array.isArray(json)) return json.map(String);
  } catch (_) {
    return [];
  }
  return [];
}

export function getAllowedSections() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed.map(String) : [];
  } catch (_) {
    return [];
  }
}

export function setAllowedSections(sections) {
  const list = Array.isArray(sections) ? sections.map(String) : [];
  localStorage.setItem(STORAGE_KEY, JSON.stringify(list));
}

export function clearAllowedSections() {
  localStorage.removeItem(STORAGE_KEY);
}

export function managerCanAccessPath(path, allowedSections) {
  const section = routeToSectionKey(path);
  if (!section) return false;
  const allowed = allowedSections || getAllowedSections();
  return allowed.includes(section);
}

/** i18n keys for section labels in user forms */
export const SECTION_I18N_KEYS = {
  pos: "PointOfSale",
  category: "itemTagsPlaceholder",
  items: "Items",
  shortcutItems: "shortcutItemsTitle",
  priceReader: "PriceReader",
  reports: "Reports",
  endOfDayReport: "endOfDayReportTitle",
  expenses: "expenses",
  inventory: "inventory",
  warehouses: "warehousesTitle",
  printServer: "printServerManagement",
  paymentDevices: "paymentDevicesManagement",
  cardPayments: "cardPaymentTransactions",
  employees: "employeesManagement",
  payroll: "payrollAndAdvances",
  customers: "customersManagement",
  deferredPayments: "deferredPaymentsTitle",
  stockAlerts: "stockAlertsTitle",
  stockReturns: "stockReturnsTitle",
  auditLog: "auditLog",
  users: "Accounts",
  dashboard: "home",
};
