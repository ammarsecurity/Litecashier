/**
 * Central navigation definitions + role filtering (retail cashier).
 */

/**
 * @param {(key: string) => string} t - vue-i18n $t
 */
export function buildNavItems(t) {
  return [
    {
      name: "dashboard",
      label: t("home"),
      link: "/dashboard",
      icon: "house-door-fill",
    },
    {
      name: "pos",
      label: t("PointOfSale"),
      link: "/pos",
      icon: "cash-stack",
    },
    {
      name: "category",
      label: t("itemTagsPlaceholder"),
      link: "/category",
      icon: "tags-fill",
    },
    {
      name: "items",
      label: t("Items"),
      link: "/items",
      icon: "inbox-fill",
    },
    {
      name: "priceReader",
      label: t("PriceReader"),
      link: "/priceReader",
      icon: "upc-scan",
    },
    {
      name: "users",
      label: t("Accounts"),
      link: "/users",
      icon: "people-fill",
    },
    {
      name: "reports",
      label: t("Reports"),
      link: "/reports",
      icon: "file-earmark-bar-graph-fill",
    },
    {
      name: "endOfDayReport",
      label: t("endOfDayReportTitle") || "تقرير نهاية اليوم",
      link: "/end-of-day-report",
      icon: "calendar2-check-fill",
    },
    {
      name: "expenses",
      label: t("expenses") || "الصرفيات",
      link: "/expenses",
      icon: "wallet2",
    },
    {
      name: "inventory",
      label: t("inventory") || "مخزن المواد",
      link: "/inventory",
      icon: "box-seam",
    },
    {
      name: "warehouses",
      label: t("warehousesTitle") || "المخازن",
      link: "/warehouses",
      icon: "building",
    },
    {
      name: "printServer",
      label: t("printServerManagement") || "إدارة خادم الطباعة",
      link: "/print-server",
      icon: "server",
    },
    {
      name: "paymentDevices",
      label: t("paymentDevicesManagement") || "إدارة أجهزة الدفع",
      link: "/payment-devices",
      icon: "credit-card-2-front-fill",
    },
    {
      name: "cardPayments",
      label: t("cardPaymentTransactions") || "معاملات البطاقة",
      link: "/card-payments",
      icon: "credit-card-fill",
    },
    {
      name: "employees",
      label: t("employeesManagement") || "إدارة الموظفين",
      link: "/employees",
      icon: "person-badge-fill",
    },
    {
      name: "payroll",
      label: t("payrollAndAdvances") || "الرواتب والسلف",
      link: "/payroll",
      icon: "cash-stack",
    },
    {
      name: "customers",
      label: t("customersManagement") || "إدارة العملاء",
      link: "/customers",
      icon: "person-lines-fill",
    },
    {
      name: "deferredPayments",
      label: t("deferredPaymentsTitle") || "الدفع اللاحق",
      link: "/deferred-payments",
      icon: "wallet2",
    },
    {
      name: "stockAlerts",
      label: t("stockAlertsTitle") || "تنبيهات المخزون",
      link: "/stock-alerts",
      icon: "bell-fill",
    },
    {
      name: "stockReturns",
      label: t("stockReturnsTitle") || "إرجاع مخزني",
      link: "/stock-returns",
      icon: "arrow-return-left",
    },
    {
      name: "auditLog",
      label: t("auditLog") || "سجل العمليات",
      link: "/audit-log",
      icon: "journal-text",
    },
    {
      name: "profile",
      label: t("myProfile") || "الحساب الشخصي",
      link: "/profile",
      icon: "person-circle",
    },
    {
      name: "settings",
      label: t("settingsTitle") || "الإعدادات",
      link: "/settings",
      icon: "gear-fill",
    },
    {
      name: "logout",
      label: t("Logout"),
      link: "/logout",
      icon: "box-arrow-right",
    },
  ];
}

export function filterNavByRole(role, items, allowedSections = []) {
  if (role === "Manager") {
    const allowed = new Set(
      Array.isArray(allowedSections) ? allowedSections.map(String) : []
    );
    return items.filter(
      (item) => item.name === "logout" || allowed.has(item.name)
    );
  }
  if (role === "Admin") {
    return items.filter(
      (item) => item.name === "users" || item.name === "logout"
    );
  }
  if (role === "POS") {
    return items.filter(
      (item) =>
        item.name === "dashboard" ||
        item.name === "items" ||
        item.name === "pos" ||
        item.name === "inventory" ||
        item.name === "stockAlerts" ||
        item.name === "stockReturns" ||
        item.name === "printServer" ||
        item.name === "logout"
    );
  }
  if (role === "Reader") {
    return items.filter(
      (item) => item.name === "priceReader" || item.name === "logout"
    );
  }
  if (role === "Commercial") {
    return items;
  }
  return items;
}

/**
 * Hub cards for /sections — ordered like buildNavItems, without home/logout.
 */
export function flatNavItemsForHub(role, t, allowedSections = []) {
  const items = filterNavByRole(role, buildNavItems(t), allowedSections);
  return items.filter(
    (item) => item.name !== "dashboard" && item.name !== "logout"
  );
}
