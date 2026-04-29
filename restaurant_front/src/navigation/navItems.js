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
      name: "auditLog",
      label: t("auditLog") || "سجل العمليات",
      link: "/audit-log",
      icon: "journal-text",
    },
    {
      name: "printTemplates",
      label: t("printTemplates") || "نماذج الطباعة",
      link: "/print-templates",
      icon: "printer-fill",
    },
    {
      name: "logout",
      label: t("Logout"),
      link: "/logout",
      icon: "box-arrow-right",
    },
  ];
}

export function filterNavByRole(role, items) {
  if (role === "Admin") {
    return items.filter((item) => item.name === "users" || item.name === "logout");
  }
  if (role === "POS") {
    return items.filter(
      (item) =>
        item.name === "items" ||
        item.name === "pos" ||
        item.name === "printServer" ||
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
export function flatNavItemsForHub(role, t) {
  const items = filterNavByRole(role, buildNavItems(t));
  return items.filter(
    (item) => item.name !== "dashboard" && item.name !== "logout"
  );
}
