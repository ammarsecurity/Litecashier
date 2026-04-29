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
import PriceReaderView from '../views/PriceReaderView.vue'
import RegisterView from '../views/Auth/RegisterView.vue'
import PrintServerManagementView from '../views/PrintServerManagementView.vue'
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
    path: '/print-server',
    name: 'printServerManagement',
    component: PrintServerManagementView,
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
        if (role === 'Admin' && to.path !== '/users' && to.path !== '/logout') {
          return next('/users');
        }
        // If POS tries to access any page other than /items or /pos or /print-server, redirect to /pos
        if (role === 'POS' && to.path !== '/items' && to.path !== '/pos' && to.path !== '/print-server' && to.path !== '/logout') {
          return next('/pos');
        }
        return next();
      } else {
        // User doesn't have required role, redirect to appropriate page
        if (role === 'Admin') {
          return next('/users');
        } else if (role === 'POS') {
          return next('/pos');
        } else if (role === 'Reader') {
          return next('/priceReader');
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
