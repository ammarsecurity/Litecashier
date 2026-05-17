/**
 * Section keys match navItems.js item.name and backend SectionDefinitions.AssignableSectionKeys.
 */

export const ASSIGNABLE_SECTION_KEYS = [
  "category",
  "items",
  "tables",
  "reservations",
  "reports",
  "endOfDayReport",
  "publicOrders",
  "orderQueue",
  "expenses",
  "inventory",
  "printServer",
  "deliveryDrivers",
  "employees",
  "customers",
  "auditLog",
  "printTemplates",
];

/** Route path → section key (first match wins). */
const ROUTE_SECTION_MAP = [
  { prefix: "/reports", key: "reports" },
  { prefix: "/end-of-day-report", key: "endOfDayReport" },
  { prefix: "/inventory", key: "inventory" },
  { prefix: "/expenses", key: "expenses" },
  { prefix: "/restaurant/table-layout", key: "tables" },
  { prefix: "/restaurant/tables", key: "tables" },
  { prefix: "/restaurant/reservations", key: "reservations" },
  { prefix: "/delivery-drivers", key: "deliveryDrivers" },
  { prefix: "/employees", key: "employees" },
  { prefix: "/customers", key: "customers" },
  { prefix: "/audit-log", key: "auditLog" },
  { prefix: "/print-server-new", key: "printServer" },
  { prefix: "/print-server", key: "printServer" },
  { prefix: "/print-templates", key: "printTemplates" },
  { prefix: "/order-queue", key: "orderQueue" },
  { prefix: "/public-orders", key: "publicOrders" },
  { prefix: "/category", key: "category" },
  { prefix: "/items", key: "items" },
];

const STORAGE_KEY = "allowedSections";

export function routeToSectionKey(path) {
  if (!path) return null;
  const p = String(path).split("?")[0];
  const hit = ROUTE_SECTION_MAP.find((r) => p === r.prefix || p.startsWith(r.prefix + "/"));
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
  category: "itemTagsPlaceholder",
  items: "Items",
  tables: "tables",
  reservations: "reservations",
  reports: "Reports",
  endOfDayReport: "endOfDayReportTitle",
  publicOrders: "publicOrders",
  orderQueue: "orderQueue",
  expenses: "expenses",
  inventory: "inventory",
  printServer: "printServerManagement",
  deliveryDrivers: "deliveryDriversManagement",
  employees: "employeesManagement",
  customers: "customersManagement",
  auditLog: "auditLog",
  printTemplates: "printTemplates",
};
