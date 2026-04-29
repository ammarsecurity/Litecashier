<template>
  <div class="home-page-wrapper">
    <!-- Animated Background Elements -->
    <div class="home-background-animation">
      <div class="floating-shape shape-1"></div>
      <div class="floating-shape shape-2"></div>
      <div class="floating-shape shape-3"></div>
      <div class="floating-shape shape-4"></div>
    </div>

    <div class="home-content-container">
      <!-- Header Section -->
      <header class="home-header">
        <div class="home-logo-section">
          <img src="../assets/logoarabic.png" alt="logo" class="home-logo-image" />
        </div>
        <div class="home-language-selector">
          <label for="home-language-select" class="home-language-label">{{ $t('select_language') }}:</label>
          <select 
            id="home-language-select" 
            v-model="$i18n.locale" 
            @change="changeLanguage" 
            class="home-language-dropdown"
          >
            <option value="en">🇺🇸 English</option>
            <option value="ar">🇶🇪 عربي</option>
          </select>
        </div>
      </header>

      <!-- Hero Section -->
      <section class="home-hero-section">
        <div class="home-hero-content">
          <div class="home-hero-text">
            <div class="home-badge">
              <b-icon icon="star-fill" class="badge-icon"></b-icon>
              <span>{{ $t('welcomeMessage') }}</span>
            </div>
            <h1 class="home-main-title">
              {{ $t('hello') }}
              <span class="home-brand-name">Litecashier</span>
            </h1>
            <p class="home-description-text">
              {{ $t('homePageDes') }}
            </p>
            <div class="home-action-buttons">
              <button class="home-action-button home-login-button" @click="goToLogin()">
                <span class="button-background"></span>
                <b-icon icon="box-arrow-in-right" class="button-icon"></b-icon>
                <span class="button-text">{{ $t('login') }}</span>
              </button>
              <button class="home-action-button home-register-button" @click="goToRegisterRoute()">
                <span class="button-background"></span>
                <b-icon icon="person-plus-fill" class="button-icon"></b-icon>
                <span class="button-text">{{ $t('register') }}</span>
              </button>
            </div>
          </div>
          <div class="home-hero-image">
            <div class="image-glow"></div>
            <img src="../assets/chashir.png" alt="Litecashier" class="home-feature-image" />
          </div>
        </div>
      </section>

      <!-- Stats Section - Moved up with new design -->
      <section class="home-stats-section">
        <div class="stats-container-new">
          <div class="stat-card-new" v-for="(stat, index) in stats" :key="index">
            <div class="stat-icon-wrapper-new">
              <b-icon :icon="stat.icon" class="stat-icon-new"></b-icon>
            </div>
            <div class="stat-content-new">
              <h3 class="stat-value-new">{{ stat.value }}</h3>
              <p class="stat-label-new">{{ stat.label }}</p>
            </div>
          </div>
        </div>
      </section>

      <!-- Features Section - New Layout -->
      <section class="home-features-section">
        <div class="section-header-new">
          <h2 class="section-title-new">{{ $t('featuresTitle') || 'المميزات الرئيسية' }}</h2>
          <p class="section-subtitle-new">{{ $t('featuresSubtitle') || 'نظام متكامل لإدارة متجرك بكفاءة عالية' }}</p>
        </div>
        <div class="features-container-new">
          <div class="feature-card-new" v-for="(feature, index) in features" :key="index">
            <div class="feature-number">{{ String(index + 1).padStart(2, '0') }}</div>
            <div class="feature-icon-wrapper-new">
              <b-icon :icon="feature.icon" class="feature-icon-new"></b-icon>
            </div>
            <div class="feature-content-new">
              <h3 class="feature-title-new">{{ feature.title }}</h3>
              <p class="feature-description-new">{{ feature.description }}</p>
            </div>
          </div>
        </div>
      </section>

      <!-- Additional Features Section - New List Design -->
      <section class="home-additional-features-new">
        <div class="section-header-new">
          <h2 class="section-title-new">{{ $t('additionalFeatures') || 'مميزات إضافية' }}</h2>
          <p class="section-subtitle-new">{{ $t('additionalFeaturesSubtitle') || 'كل ما تحتاجه لإدارة متجرك بشكل احترافي' }}</p>
        </div>
        <div class="additional-features-list">
          <div class="additional-feature-row" v-for="(feature, index) in additionalFeatures" :key="index">
            <div class="additional-feature-icon-new">
              <b-icon :icon="feature.icon" class="feature-icon-small-new"></b-icon>
            </div>
            <div class="additional-feature-content-new">
              <h4 class="additional-feature-title-new">{{ feature.title }}</h4>
              <p class="additional-feature-desc-new">{{ feature.description }}</p>
            </div>
            <div class="additional-feature-arrow">
              <b-icon icon="arrow-right" class="arrow-icon"></b-icon>
            </div>
          </div>
        </div>
      </section>

      <!-- Footer -->
      <footer class="home-footer-section">
        <div class="footer-content">
          <p class="home-footer-text">{{ $t('contactSupport') }}</p>
          <div class="footer-developer">
            <span>{{ $t('developedBy') }}</span>
            <a :href="$t('companyWebsite')" target="_blank" class="developer-link">
              {{ $t('companyName') }}
            </a>
          </div>
        </div>
      </footer>
    </div>
  </div>
</template>

<script>
export default {
  name: 'HomeView',
  data() {
    return {
      features: [
        {
          icon: 'cash-stack',
          title: this.$t('PointOfSale') || 'نقطة بيع',
          description: this.$t('pointOfSale') || 'نظام بيع سريع وسهل'
        },
        {
          icon: 'file-earmark-bar-graph-fill',
          title: this.$t('Reports') || 'التقارير',
          description: this.$t('reports') || 'تقارير تفصيلية وتحليلية'
        },
        {
          icon: 'inbox-fill',
          title: this.$t('Items') || 'المواد',
          description: this.$t('items') || 'الرقم'
        },
        {
          icon: 'people-fill',
          title: this.$t('Accounts') || 'الحسابات',
          description: this.$t('accounts') || 'إدارة المستخدمين بكفاءة'
        }
      ],
      stats: [
        {
          icon: 'speedometer2',
          value: this.$t('fastPerformance') || 'سريع',
          label: this.$t('fastPerformanceDesc') || 'أداء سريع وموثوق'
        },
        {
          icon: 'shield-check',
          value: this.$t('secure') || 'آمن',
          label: this.$t('secureDesc') || 'حماية عالية للبيانات'
        },
        {
          icon: 'phone',
          value: this.$t('responsive') || 'متجاوب',
          label: this.$t('responsiveDesc') || 'يعمل على جميع الأجهزة'
        },
        {
          icon: 'headset',
          value: this.$t('support') || 'دعم فني',
          label: this.$t('supportDesc') || 'دعم فني متواصل'
        }
      ],
      additionalFeatures: [
        {
          icon: 'upc',
          title: this.$t('barcodeScanning') || 'مسح الباركود',
          description: this.$t('barcodeScanningDesc') || 'مسح سريع ودقيق للباركود'
        },
        {
          icon: 'printer',
          title: this.$t('printing') || 'الطباعة',
          description: this.$t('printingDesc') || 'طباعة الفواتير والإيصالات'
        },
        {
          icon: 'credit-card',
          title: this.$t('paymentMethods') || 'طرق الدفع',
          description: this.$t('paymentMethodsDesc') || 'دعم طرق دفع متعددة'
        },
        {
          icon: 'archive',
          title: this.$t('inventoryManagement') || 'إدارة المخزون',
          description: this.$t('inventoryManagementDesc') || 'تتبع المخزون تلقائياً'
        },
        {
          icon: 'pie-chart',
          title: this.$t('analytics') || 'التحليلات',
          description: this.$t('analyticsDesc') || 'تقارير وإحصائيات مفصلة'
        },
        {
          icon: 'key',
          title: this.$t('multiUser') || 'متعدد المستخدمين',
          description: this.$t('multiUserDesc') || 'إدارة صلاحيات متقدمة'
        }
      ]
    }
  },
  methods: {
    goToRegisterRoute() {
      this.$router.push('/register');
    },
    goToLogin() {
      this.$router.push('/login');
    },
    changeLanguage(obj) {
      localStorage.setItem('language', obj.target.value);
      const currentLang = this.$i18n.locale;
      document.body.dir = currentLang === 'en' ? 'ltr' : 'rtl';
    }
  }
}
</script>
