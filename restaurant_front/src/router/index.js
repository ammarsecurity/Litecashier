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
import RegisterView from '../views/Auth/RegisterView.vue'
import PrintServerManagementView from '../views/PrintServerManagementView.vue'
import PrintServerManagementNewView from '../views/PrintServerManagementNewView.vue'
import TablesView from '../views/Restaurant/TablesView.vue'
import TableLayoutView from '../views/Restaurant/TableLayoutView.vue'
import ReservationsView from '../views/Restaurant/ReservationsView.vue'
import WaiterView from '../views/Restaurant/WaiterView.vue'
import PublicMenuView from '../views/PublicMenuView.vue'
import OrderQueueView from '../views/OrderQueueView.vue'
import DeliveryDriversView from '../views/DeliveryDriversView.vue'
import EmployeesView from '../views/EmployeesView.vue'
import CustomersView from '../views/CustomersView.vue'
import ExpensesView from '../views/ExpensesView.vue'
import InventoryView from '../views/InventoryView.vue'
import AuditLogView from '../views/AuditLogView.vue'
import PrintTemplatesView from '../views/PrintTemplatesView.vue'
import { i18n } from '../main'
import { managerCanAccessPath } from '../navigation/sectionRegistry.js'
Vue.use(VueRouter)

const getDefaultPathForRole = (role) => {
  if (role === 'Admin') return '/users'
  if (role === 'POS') return '/pos'
  if (role === 'Manager') return '/sections'
  if (role === 'LoyaltyManager') return '/restaurant/loyalty'
  if (role === 'Waiter') return '/restaurant/waiter'
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
    path: '/menu/:commercialUserId',
    name: 'publicMenu',
    component: PublicMenuView,
    meta: {
      requiresAuth: false
    }
  },
  {
    path: '/order/:commercialUserId',
    name: 'publicOrder',
    component: () => import('../views/PublicOrderView.vue'),
    meta: {
      requiresAuth: false
    }
  },
  {
    path: '/order-status/:commercialUserId/:orderCode?',
    name: 'orderStatus',
    component: () => import('../views/OrderStatusView.vue'),
    meta: {
      requiresAuth: false
    }
  },
  {
    path: '/public-queue/:commercialUserId',
    name: 'publicQueueDisplay',
    component: () => import('../views/PublicQueueDisplayView.vue'),
    meta: {
      requiresAuth: false
    }
  },
  {
    path: '/public-orders',
    name: 'publicOrders',
    component: () => import('../views/PublicOrdersView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Commercial']
    }
  },
  {
    path: '/order-queue',
    name: 'orderQueue',
    component: OrderQueueView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS', 'Admin']
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
      roles: ['Commercial', 'Admin']
    }
  },
  {
    path: '/sections',
    name: 'sections',
    component: () => import('../views/SectionsView.vue'),
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'Admin', 'POS', 'Waiter', 'LoyaltyManager', 'Manager']
    }
  },
  {
    path: '/print-server',
    name: 'printServerManagement',
    component: PrintServerManagementView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS', 'Admin']
    }
  },
  {
    path: '/print-server-new',
    name: 'printServerManagementNew',
    component: PrintServerManagementNewView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS', 'Admin']
    }
  },
  {
    path: '/restaurant/tables',
    name: 'tables',
    component: TablesView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS', 'Admin']
    }
  },
  {
    path: '/restaurant/table-layout',
    name: 'tableLayout',
    component: TableLayoutView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS', 'Admin', 'Waiter']
    }
  },
  {
    path: '/restaurant/reservations',
    name: 'reservations',
    component: ReservationsView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS', 'Admin']
    }
  },
  {
    path: '/restaurant/waiter',
    name: 'waiter',
    component: WaiterView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS', 'Admin', 'Waiter']
    }
  },
  {
    path: '/delivery-drivers',
    name: 'deliveryDrivers',
    component: DeliveryDriversView,
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
    path: '/customers',
    name: 'customers',
    component: CustomersView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'Admin']
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
    path: '/audit-log',
    name: 'auditLog',
    component: AuditLogView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial']
    }
  },
  {
    path: '/print-templates',
    name: 'printTemplates',
    component: PrintTemplatesView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS', 'Admin']
    }
  },
  {
    path: '/logout',
    name: 'logout',
  },
  {
    path: '/about',
    name: 'about',
    // route level code-splitting
    // this generates a separate chunk (about.[hash].js) for this route
    // which is lazy-loaded when the route is visited.
    component: function () {
      return import(/* webpackChunkName: "about" */ '../views/AboutView.vue')
    }
  }
]

const router = new VueRouter({
  // add mode: 'history' to remove # from url
  mode: 'history',
  routes
})

router.beforeEach((to, from, next) => {
  try {
    i18n.locale = localStorage.getItem('language') || 'ar';

    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');

    // If user is already logged in and tries to open login/register, send to role home.
    if ((to.path === '/login' || to.path === '/register') && token && role) {
      return next(getDefaultPathForRole(role));
    }

    // Routes that don't require authentication
    if (to.meta.requiresAuth === false) {
      return next();
    }

    // Handle logout
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

    // Check if user has required role
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
        // If Admin tries to access any page other than /users, redirect to /users
        if (role === 'Admin' && to.path !== '/users' && to.path !== '/logout' && to.path !== '/sections' && to.path !== '/customers') {
          return next('/users');
        }
        // If POS tries to access any page other than allowed paths, redirect to /pos
        if (
          role === 'POS' &&
          to.path !== '/items' &&
          to.path !== '/pos' &&
          to.path !== '/printer-settings' &&
          to.path !== '/logout' &&
          to.path !== '/sections'
        ) {
          return next('/pos');
        }
        if (role === 'LoyaltyManager' && to.path !== '/restaurant/loyalty' && to.path !== '/logout' && to.path !== '/sections') {
          return next('/restaurant/loyalty');
        }
        if (role === 'Waiter' && to.path !== '/restaurant/waiter' && to.path !== '/logout' && to.path !== '/sections') {
          return next('/restaurant/waiter');
        }
        return next();
      } else {
        return next(getDefaultPathForRole(role));
      }
    }

    // Default: redirect to login
    return next('/login');
  } catch (error) {
    console.error('Router navigation error:', error);
    return next('/login');
  }
});


export default router
