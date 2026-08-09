import Vue from 'vue'
import VueRouter from 'vue-router'
import LoginView from '../views/Auth/LoginView.vue'
import DashboardView from '../views/DashboardView.vue'
import ItemsView from '../views/ItemsView.vue'
import UsersView from '../views/UsersView.vue'
import CategoryView from '../views/CategoryView.vue'
import ReporstView from '../views/ReporstView.vue'
import EndOfDayReportView from '../views/EndOfDayReportView.vue'
import PosView from '../views/PosView.vue'
import PriceReaderView from '../views/PriceReaderView.vue'
import RegisterView from '../views/Auth/RegisterView.vue'
import PrintServerManagementNewView from '../views/PrintServerManagementNewView.vue'
import EmployeesView from '../views/EmployeesView.vue'
import PayrollView from '../views/PayrollView.vue'
import CustomersView from '../views/CustomersView.vue'
import ExpensesView from '../views/ExpensesView.vue'
import InventoryView from '../views/InventoryView.vue'
import WarehousesView from '../views/WarehousesView.vue'
import AuditLogView from '../views/AuditLogView.vue'
import PaymentDevicesView from '../views/PaymentDevicesView.vue'
import CardPaymentsView from '../views/CardPaymentsView.vue'
import DeferredPaymentsView from '../views/DeferredPaymentsView.vue'
import StockAlertsView from '../views/StockAlertsView.vue'
import StockReturnsView from '../views/StockReturnsView.vue'
import SettingsView from '../views/SettingsView.vue'
import { i18n } from '../main'
import { managerCanAccessPath } from '../navigation/sectionRegistry.js'
Vue.use(VueRouter)

export const getDefaultPathForRole = (role) => {
  if (role === 'Admin') return '/users'
  if (role === 'POS') return '/pos'
  if (role === 'Manager') return '/sections'
  if (role === 'Reader') return '/priceReader'
  return '/dashboard'
}

function userMayAccessRoute(role, to) {
  const roles = to.meta && to.meta.roles
  if (!roles || !Array.isArray(roles)) return true
  if (roles.includes(role)) return true
  if (role === 'Manager' && managerCanAccessPath(to.path)) return true
  return false
}

const routes = [
  {
    path: '/',
    name: 'home',
    redirect: () => {
      const token = localStorage.getItem('token')
      const role = localStorage.getItem('role')
      if (token && role) {
        return getDefaultPathForRole(role)
      }
      return '/login'
    }
  },
  {
    path: '/login',
    name: 'login',
    component: LoginView,
    meta: {
      requiresAuth: false
    }
  },
  {
    path: '/register',
    name: 'register',
    component: RegisterView,
    meta: {
      requiresAuth: false
    }
  },
  {
    path: '/pos',
    name: 'pos',
    component: PosView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS']
    }
  },
  {
    path: '/items',
    name: 'items',
    component: ItemsView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS']
    }
  },
  {
    path: '/priceReader',
    name: 'priceReader',
    component: PriceReaderView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS', 'Reader']
    }
  },
  {
    path: '/reports',
    name: 'reports',
    component: ReporstView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial']
    }
  },
  {
    path: '/end-of-day-report',
    name: 'endOfDayReport',
    component: EndOfDayReportView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial']
    }
  },
  {
    path: '/users',
    name: 'users',
    component: UsersView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'Admin']
    }
  },
  {
    path: '/category',
    name: 'category',
    component: CategoryView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'Admin']
    }
  },
  {
    path: '/dashboard',
    name: 'dashboard',
    component: DashboardView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS']
    }
  },
  {
    path: '/sections',
    name: 'sections',
    component: () => import('../views/SectionsView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'Admin', 'POS', 'Reader', 'Manager']
    }
  },
  {
    path: '/print-server',
    name: 'printServerManagement',
    component: PrintServerManagementNewView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS', 'Admin', 'Manager']
    }
  },
  {
    path: '/payment-devices',
    name: 'paymentDevices',
    component: PaymentDevicesView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'Admin']
    }
  },
  {
    path: '/card-payments',
    name: 'cardPayments',
    component: CardPaymentsView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'Admin']
    }
  },
  {
    path: '/employees',
    name: 'employees',
    component: EmployeesView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'Admin']
    }
  },
  {
    path: '/payroll',
    name: 'payroll',
    component: PayrollView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'Admin']
    }
  },
  {
    path: '/customers',
    name: 'customers',
    component: CustomersView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial']
    }
  },
  {
    path: '/deferred-payments',
    name: 'deferredPayments',
    component: DeferredPaymentsView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial']
    }
  },
  {
    path: '/stock-alerts',
    name: 'stockAlerts',
    component: StockAlertsView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS']
    }
  },
  {
    path: '/stock-returns',
    name: 'stockReturns',
    component: StockReturnsView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS']
    }
  },
  {
    path: '/expenses',
    name: 'expenses',
    component: ExpensesView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'Admin']
    }
  },
  {
    path: '/inventory',
    name: 'inventory',
    component: InventoryView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS', 'Admin']
    }
  },
  {
    path: '/warehouses',
    name: 'warehouses',
    component: WarehousesView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'Admin']
    }
  },
  {
    path: '/audit-log',
    name: 'auditLog',
    component: AuditLogView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial']
    }
  },
  {
    path: '/settings',
    name: 'settings',
    component: SettingsView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial']
    }
  },
  {
    path: '/logout',
    name: 'logout',
  },
  {
    path: '/about',
    name: 'about',
    component: function () {
      return import(/* webpackChunkName: "about" */ '../views/AboutView.vue')
    }
  }
]

const router = new VueRouter({
  mode: 'history',
  routes
})

router.beforeEach((to, from, next) => {
  try {
    i18n.locale = localStorage.getItem('language') || 'ar';

    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');

    if ((to.path === '/login' || to.path === '/register') && token && role) {
      return next(getDefaultPathForRole(role));
    }

    if (to.meta.requiresAuth === false) {
      return next();
    }

    if (to.path === '/logout') {
      localStorage.removeItem('token');
      localStorage.removeItem('role');
      localStorage.removeItem('info');
      localStorage.removeItem('allowedSections');
      return next('/login');
    }

    if (to.meta.requiresAuth === true && !token) {
      return next('/login');
    }

    if (to.meta.requiresAuth === true && token && role) {
      if (userMayAccessRoute(role, to)) {
        if (role === 'Manager') {
          if (
            to.path === '/sections' ||
            to.path === '/logout' ||
            managerCanAccessPath(to.path)
          ) {
            return next();
          }
          return next('/sections');
        }
        if (
          role === 'Admin' &&
          to.path !== '/users' &&
          to.path !== '/logout' &&
          to.path !== '/sections'
        ) {
          return next('/users');
        }
        if (
          role === 'POS' &&
          to.path !== '/dashboard' &&
          to.path !== '/items' &&
          to.path !== '/pos' &&
          to.path !== '/inventory' &&
          to.path !== '/stock-alerts' &&
          to.path !== '/stock-returns' &&
          to.path !== '/print-server' &&
          to.path !== '/logout' &&
          to.path !== '/sections'
        ) {
          return next('/pos');
        }
        if (role === 'Reader' && to.path !== '/priceReader' && to.path !== '/logout' && to.path !== '/sections') {
          return next('/priceReader');
        }
        return next();
      } else {
        return next(getDefaultPathForRole(role));
      }
    }

    return next('/login');
  } catch (error) {
    console.error('Router navigation error:', error);
    return next('/login');
  }
});


export default router
