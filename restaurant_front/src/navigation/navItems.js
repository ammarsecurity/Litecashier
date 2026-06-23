/**
 * Central navigation definitions + role filtering (was SidebarView).
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
      name: "tables",
      label: t("tables") || "الطاولات",
      link: "/restaurant/tables",
      icon: "table",
    },
    {
      name: "reservations",
      label: t("reservations") || "الحجوزات",
      link: "/restaurant/reservations",
      icon: "calendar-check-fill",
    },
    {
      name: "waiter",
      label: t("waiterView") || "صفحة الويتر",
      link: "/restaurant/waiter",
      icon: "person-badge-fill",
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
      name: "publicOrders",
      label: t("publicOrders") || "الطلبات العامة",
      link: "/public-orders",
      icon: "cart-check-fill",
    },
    {
      name: "orderQueue",
      label: t("orderQueue") || "طابور الطلبات",
      link: "/order-queue",
      icon: "list-ul",
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
      name: "printServer",
      label: t("printServerManagement") || "إدارة خادم الطباعة",
      link: "/print-server",
      icon: "server",
    },
    {
      name: "deliveryDrivers",
      label: t("deliveryDriversManagement") || "إدارة سائقي التوصيل",
      link: "/delivery-drivers",
      icon: "truck",
    },
    {
      name: "employees",
      label: t("employeesManagement") || "إدارة الموظفين",
      link: "/employees",
      icon: "person-badge-fill",
    },
    {
      name: "customers",
      label: t("customersManagement") || "إدارة العملاء",
      link: "/customers",
      icon: "person-lines-fill",
    },
    {
      name: "auditLog",
      label: t("auditLog") || "سجل العمليات",
      link: "/audit-log",
      icon: "journal-text",
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
      name: "deferredPayments",
      label: t("deferredPaymentsTitle") || "الدفع اللاحق",
      link: "/deferred-payments",
      icon: "wallet2",
    },
    {
      name: "databaseSync",
      label: t("databaseSyncTitle") || "نسخ احتياطي سحابي",
      link: "/database-sync",
      icon: "cloud-upload-fill",
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
    return items.filter((item) => item.name === "users" || item.name === "logout" || item.name === "customers");
  }
  if (role === "POS") {
    return items.filter(
      (item) =>
        item.name === "items" ||
        item.name === "pos" ||
        item.name === "logout"
    );
  }
  if (role === "Commercial") {
    return items;
  }
  if (role === "Waiter") {
    return items.filter((item) => item.name === "waiter" || item.name === "logout");
  }
  return items;
}

/**
 * روابط لوحة التحكم: قائمة واحدة مرتبة كما في buildNavItems، بدون الرئيسية وبدون الخروج (الخروج في الهيدر).
 */
export function flatNavItemsForHub(role, t, allowedSections = []) {
  const items = filterNavByRole(role, buildNavItems(t), allowedSections);
  return items.filter(
    (item) => item.name !== "dashboard" && item.name !== "logout"
  );
}

/**
 * عناصر شبكة أقسام النظام (الصفحة أو المودال) مع إدخال الرئيسية حسب الدور.
 */
export function buildSectionsHubItems(role, t, allowedSections = []) {
  const modules = flatNavItemsForHub(role, t, allowedSections);
  if (role === "Manager" || role === "POS") {
    return modules;
  }
  const dashboardEntry = {
    name: "dashboard-home",
    label: t("appHomeLink") || t("home") || "الرئيسية",
    link: "/dashboard",
    icon: "house-door-fill",
  };
  return [dashboardEntry, ...modules];
}
