<template>
  <div>
    <!-- Mobile Overlay - خارج Sidebar -->
    <div 
      class="sidebar-overlay" 
      v-if="isMobileMenuOpen"
      @click="closeMobileMenu"
    ></div>

    <aside class="modern-sidebar" :class="{ 'sidebar-collapsed': isCollapsed, 'mobile-open': isMobileMenuOpen }">
      <!-- Sidebar Header -->
      <div class="sidebar-header">
        <div class="sidebar-logo-container">
          <img
            src="../../assets/logoarabic.png"
            alt="logo"
            class="sidebar-logo"
          />
        </div>
      </div>

      <!-- Navigation Items -->
      <nav class="sidebar-nav">
        <div class="nav-items-list">
          <router-link 
            v-for="item in filteredNavItems" 
            :key="item.name" 
            :to="item.link" 
            class="nav-item-link"
            :class="{ 'nav-item-active': isActiveRoute(item.link) }"
            @click.native="handleNavClick"
          >
            <div class="nav-item-content">
              <div class="nav-item-icon-box">
                <b-icon :icon="item.icon" class="nav-item-icon"></b-icon>
              </div>
              <span class="nav-item-text" v-if="!isCollapsed">{{ item.label }}</span>
            </div>
            <div class="nav-item-indicator"></div>
          </router-link>
        </div>
      </nav>

      <!-- Sidebar Footer -->
      <div class="sidebar-footer">
        <div class="language-selector-wrapper">
          <select
            v-model="$i18n.locale"
            @change="changeLanguage"
            class="language-selector"
            :class="{ 'language-selector-collapsed': isCollapsed }"
          >
            <option value="en">🇺🇸 English</option>
            <option value="ar">🇶🇪 عربي</option>
          </select>
        </div>
      </div>
    </aside>

    <!-- Mobile Menu Button -->
    <button 
      class="mobile-menu-btn"
      @click="openMobileMenu"
      v-if="isMobile"
    >
      <b-icon icon="list" class="mobile-menu-icon"></b-icon>
    </button>
  </div>
</template>

<script>
export default {
  name: "SidebarView",
  data() {
    return {
      isCollapsed: false,
      isMobileMenuOpen: false,
      isMobile: false,
      navItems: [
        { name: "dashboard", label: this.$t("home"), link: "/dashboard", icon: "house-door-fill" },
        { name: "pos", label: this.$t("PointOfSale"), link: "/pos", icon: "cash-stack" },
        { name: "category", label: this.$t("itemTagsPlaceholder"), link: "/category", icon: "tags-fill" },
        { name: "items", label: this.$t("Items"), link: "/items", icon: "inbox-fill" },
        { name: "users", label: this.$t("Accounts"), link: "/users", icon: "people-fill" },
        { name: "reports", label: this.$t("Reports"), link: "/reports", icon: "file-earmark-bar-graph-fill" },
        { name: "priceReader", label: this.$t("PriceReader"), link: "/priceReader", icon: "upc-scan" },
        { name: "printServer", label: this.$t("printServerManagement") || "إدارة خادم الطباعة", link: "/print-server", icon: "server" },
        { name: "logout", label: this.$t("Logout"), link: "/logout", icon: "box-arrow-right", class: "nav-item-logout" },
      ],
    };
  },
  computed: {
    role() {
      return localStorage.getItem("role");
    },
    filteredNavItems() {
      // If role is Admin, show only Users and Logout
      if (this.role === 'Admin') {
        return this.navItems.filter(item => 
          item.name === 'users' || item.name === 'logout'
        );
      }
      // If role is POS, show only Items, POS, Print Server and Logout
      if (this.role === 'POS') {
        return this.navItems.filter(item => 
          item.name === 'items' || item.name === 'pos' || item.name === 'printServer' || item.name === 'logout'
        );
      }
      // If role is Reader, show only Price Reader and Logout
      if (this.role === 'Reader') {
        return this.navItems.filter(item => 
          item.name === 'priceReader' || item.name === 'logout'
        );
      }
      // For Commercial role, show all items except logout (logout is always shown)
      if (this.role === 'Commercial') {
        return this.navItems;
      }
      // For other roles, show only logout
      return this.navItems.filter(item => item.name === 'logout');
    }
  },
  methods: {
    changeLanguage(event) {
      const lang = event.target.value;
      localStorage.setItem("language", lang);
      this.$i18n.locale = lang;
      document.body.dir = lang === "en" ? "ltr" : "rtl";
    },
    toggleSidebar() {
      this.isCollapsed = !this.isCollapsed;
      localStorage.setItem('sidebarCollapsed', this.isCollapsed);
    },
    openMobileMenu() {
      this.isMobileMenuOpen = true;
      document.body.style.overflow = 'hidden';
    },
    closeMobileMenu() {
      this.isMobileMenuOpen = false;
      document.body.style.overflow = '';
    },
    handleNavClick() {
      if (this.isMobile) {
        this.closeMobileMenu();
      }
    },
    isActiveRoute(link) {
      return this.$route.path === link;
    },
    checkMobile() {
      this.isMobile = window.innerWidth < 1024;
      if (!this.isMobile) {
        this.isMobileMenuOpen = false;
        document.body.style.overflow = '';
      }
    }
  },
  mounted() {
    // Check if sidebar was collapsed
    const savedState = localStorage.getItem('sidebarCollapsed');
    if (savedState === 'true') {
      this.isCollapsed = true;
    }

    // Check mobile
    this.checkMobile();
    window.addEventListener('resize', this.checkMobile);
    
    // Set language direction
    const lang = localStorage.getItem('language') || 'ar';
    document.body.dir = lang === "en" ? "ltr" : "rtl";
  },
  beforeDestroy() {
    window.removeEventListener('resize', this.checkMobile);
    document.body.style.overflow = '';
  }
};
</script>
