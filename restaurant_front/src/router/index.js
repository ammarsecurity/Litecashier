import Vue from 'vue'
import VueRouter from 'vue-router'
import HomeView from '../views/HomeView.vue'
import LoginView from '../views/Auth/LoginView.vue'
import DashboardView from '../views/DashboardView.vue'
import ItemsView from '../views/ItemsView.vue'
import UsersView from '../views/UsersView.vue'
import CategoryView from '../views/CategoryView.vue'
import ReporstView from '../views/ReporstView.vue'
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
import ExpensesView from '../views/ExpensesView.vue'
import InventoryView from '../views/InventoryView.vue'
import AuditLogView from '../views/AuditLogView.vue'
import PrintTemplatesView from '../views/PrintTemplatesView.vue'
import { i18n } from '../main'
Vue.use(VueRouter)

const routes = [
  {
    path: '/',
    name: 'home',
    component: HomeView,
    meta: {
      requiresAuth: false
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
      roles: ['Commercial', 'Admin', 'POS', 'Waiter', 'TablesManager', 'ReservationsManager', 'LoyaltyManager']
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
      roles: ['Commercial', 'POS', 'Admin', 'TablesManager']
    }
  },
  {
    path: '/restaurant/table-layout',
    name: 'tableLayout',
    component: TableLayoutView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS', 'Admin', 'TablesManager', 'Waiter']
    }
  },
  {
    path: '/restaurant/reservations',
    name: 'reservations',
    component: ReservationsView,
    meta: {
      requiresAuth: true,
      roles: ['Commercial', 'POS', 'Admin', 'ReservationsManager']
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

    // Routes that don't require authentication
    if (to.meta.requiresAuth === false) {
      return next();
    }

    // Handle logout
    if (to.path === '/logout') {
      localStorage.removeItem('token');
      localStorage.removeItem('role');
      localStorage.removeItem('info');
      return next('/login');
    }

    // Check if user is authenticated
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');

    if (to.meta.requiresAuth === true && !token) {
      return next('/login');
    }

    // Check if user has required role
    if (to.meta.requiresAuth === true && token && role) {
      if (to.meta.roles && Array.isArray(to.meta.roles) && to.meta.roles.includes(role)) {
        // If Admin tries to access any page other than /users, redirect to /users
        if (role === 'Admin' && to.path !== '/users' && to.path !== '/logout' && to.path !== '/sections') {
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
        // Handle new restaurant roles
        if (role === 'TablesManager' && to.path !== '/restaurant/tables' && to.path !== '/logout' && to.path !== '/sections') {
          return next('/restaurant/tables');
        }
        if (role === 'ReservationsManager' && to.path !== '/restaurant/reservations' && to.path !== '/logout' && to.path !== '/sections') {
          return next('/restaurant/reservations');
        }
        if (role === 'LoyaltyManager' && to.path !== '/restaurant/loyalty' && to.path !== '/logout' && to.path !== '/sections') {
          return next('/restaurant/loyalty');
        }
        if (role === 'Waiter' && to.path !== '/restaurant/waiter' && to.path !== '/logout' && to.path !== '/sections') {
          return next('/restaurant/waiter');
        }
        return next();
      } else {
        // User doesn't have required role, redirect to appropriate page
        if (role === 'Admin') {
          return next('/users');
        } else if (role === 'POS') {
          return next('/pos');
        } else if (role === 'TablesManager') {
          return next('/restaurant/tables');
        } else if (role === 'ReservationsManager') {
          return next('/restaurant/reservations');
        } else if (role === 'LoyaltyManager') {
          return next('/restaurant/loyalty');
        } else if (role === 'Waiter') {
          return next('/restaurant/waiter');
        } else {
          return next('/dashboard');
        }
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
