<template>
  <div class="public-menu-container">
    <!-- Header Section -->
    <header class="public-menu-header">
      <div class="header-content">
        <div class="logo-section">
          <img 
            v-if="restaurantLogo && !logoError" 
            :src="restaurantLogo" 
            alt="Logo" 
            class="menu-logo"
            @error="logoError = true"
          />
          <img 
            v-else-if="!restaurantLogo && !logoError"
            src="../assets/logoarabicdark.png" 
            alt="Logo" 
            class="menu-logo"
            @error="logoError = true"
          />
          <div v-else class="logo-placeholder">
            <b-icon icon="shop" class="logo-icon"></b-icon>
          </div>
        </div>
        <h1 class="restaurant-name">{{ restaurantName || 'قائمة الطعام' }}</h1>
        <p class="restaurant-subtitle">استمتع بأشهى المأكولات</p>
      </div>
    </header>

    <!-- Loading State -->
    <div v-if="loading" class="loading-container">
      <div class="loading-spinner"></div>
      <p class="loading-text">جاري تحميل القائمة...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-container">
      <b-icon icon="exclamation-triangle-fill" class="error-icon"></b-icon>
      <p class="error-text">{{ error }}</p>
    </div>

    <!-- Menu Content -->
    <div v-else class="menu-content">
      <!-- Category Filter -->
      <div v-if="categories.length > 0" class="category-filter-wrapper">
        <div class="category-filter">
          <button 
            class="category-btn" 
            :class="{ active: selectedCategory === null }"
            @click="selectedCategory = null"
          >
            <b-icon icon="grid-fill" class="me-2"></b-icon>
            الكل
          </button>
          <button 
            v-for="category in sortedCategories" 
            :key="category"
            class="category-btn"
            :class="{ active: selectedCategory === category }"
            @click="selectedCategory = category"
          >
            {{ category }}
          </button>
        </div>
      </div>

      <!-- Menu Items Grid -->
      <div class="menu-items-grid">
        <div 
          v-for="item in filteredItems" 
          :key="item.id"
          class="menu-item-card"
        >
          <div class="item-image-container">
            <img 
              v-if="item.image && !item.imageError" 
              :src="item.image" 
              :alt="item.name"
              class="item-image"
              @error="item.imageError = true"
            />
            <div v-else class="item-image-placeholder">
              <b-icon icon="image" class="placeholder-icon"></b-icon>
            </div>
            <div v-if="item.discountPrice" class="discount-badge">
              <span>خصم</span>
            </div>
          </div>
          
          <div class="item-content">
            <div class="item-header">
              <h3 class="item-name">{{ item.name }}</h3>
              <span v-if="item.tags" class="item-category">{{ item.tags }}</span>
            </div>
            
            <p v-if="item.description" class="item-description">{{ item.description }}</p>
            
            <div class="item-footer">
              <div class="item-price">
                <span v-if="item.discountPrice" class="original-price">
                  {{ formatPrice(item.sellingPrice) }} د.ع
                </span>
                <span class="current-price">
                  {{ formatPrice(item.discountPrice || item.sellingPrice) }} د.ع
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div v-if="filteredItems.length === 0" class="empty-state">
        <b-icon icon="inbox" class="empty-icon"></b-icon>
        <p class="empty-text">لا توجد عناصر في هذه الفئة</p>
      </div>
    </div>

    <!-- Footer -->
    <footer class="public-menu-footer">
      <p class="footer-text">© 2024 جميع الحقوق محفوظة</p>
    </footer>
  </div>
</template>

<script>
import { HTTP } from '../http/api.js';

export default {
  name: 'PublicMenuView',
  data() {
    return {
      loading: true,
      error: null,
      items: [],
      categories: [],
      selectedCategory: null,
      restaurantName: '',
      restaurantLogo: null,
      logoError: false,
      commercialUserId: null
    };
  },
  computed: {
    filteredItems() {
      if (!this.selectedCategory) {
        return this.items;
      }
      return this.items.filter(item => item.tags === this.selectedCategory);
    },
    sortedCategories() {
      // Sort categories alphabetically
      return [...this.categories].sort((a, b) => {
        return a.localeCompare(b, 'ar');
      });
    }
  },
  mounted() {
    // Get commercialUserId from route params or query
    this.commercialUserId = this.$route.params.commercialUserId || this.$route.query.commercialUserId;
    
    if (!this.commercialUserId) {
      this.error = 'معرف المطعم غير موجود';
      this.loading = false;
      return;
    }

    this.loadMenu();
    this.loadCategories();
  },
  methods: {
    async loadMenu() {
      try {
        this.loading = true;
        this.error = null;
        
        const response = await HTTP.get(`PublicMenu/${this.commercialUserId}`);
        
        if (response.data && response.data.data) {
          const menuData = response.data.data;
          this.restaurantName = menuData.restaurantName || '';
          this.restaurantLogo = menuData.logo || null;
          this.items = (menuData.items || []).map(item => ({
            ...item,
            imageError: false
          }));
        } else {
          this.error = 'فشل تحميل القائمة';
        }
      } catch (err) {
        console.error('Error loading menu:', err);
        this.error = err.response?.data?.message || 'حدث خطأ أثناء تحميل القائمة';
      } finally {
        this.loading = false;
      }
    },
    async loadCategories() {
      // Only load categories if commercialUserId is available
      if (!this.commercialUserId) {
        return;
      }
      
      try {
        const response = await HTTP.get(`PublicMenu/${this.commercialUserId}/categories`);
        
        if (response.data && response.data.data) {
          this.categories = response.data.data;
        }
      } catch (err) {
        console.error('Error loading categories:', err);
        // Don't show error for categories, it's optional
        this.categories = [];
      }
    },
    formatPrice(price) {
      return new Intl.NumberFormat('ar-IQ').format(price);
    }
  }
};
</script>

<style scoped>
.public-menu-container {
  min-height: 100vh;
  background: linear-gradient(135deg, #0f172a 0%, #1e293b 50%, #334155 100%);
  color: #ffffff;
}

/* Header Styles */
.public-menu-header {
  background: linear-gradient(135deg, var(--bg-primary) 0%, var(--bg-tertiary) 50%, var(--bg-primary) 100%);
  padding: 2.5rem 2rem;
  text-align: center;
  box-shadow: var(--shadow-lg);
  position: relative;
  overflow: hidden;
  border-bottom: 2px solid var(--border-color);
}

.public-menu-header::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: radial-gradient(circle at 50% 0%, rgba(129, 140, 248, 0.15) 0%, transparent 60%);
  pointer-events: none;
}

.public-menu-header::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 1px;
  background: linear-gradient(90deg, transparent, var(--primary-color), transparent);
  opacity: 0.5;
}

.header-content {
  position: relative;
  z-index: 1;
  max-width: 600px;
  margin: 0 auto;
}

.logo-section {
  margin-bottom: 1.25rem;
}

.menu-logo {
  max-width: 120px;
  max-height: 120px;
  height: auto;
  width: auto;
  object-fit: contain;
  filter: drop-shadow(0 4px 12px rgba(0, 0, 0, 0.4));
  transition: transform 0.3s ease;
}

.menu-logo:hover {
  transform: scale(1.05);
}

.logo-placeholder {
  width: 100px;
  height: 100px;
  margin: 0 auto;
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.2) 0%, rgba(99, 102, 241, 0.2) 100%);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 2px solid var(--primary-color);
}

.logo-icon {
  font-size: 2.5rem;
  color: var(--primary-color);
}

.restaurant-name {
  font-size: 2.25rem;
  font-weight: 800;
  margin-bottom: 0.75rem;
  background: linear-gradient(135deg, #ffffff 0%, var(--primary-color) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  text-shadow: 0 2px 8px rgba(129, 140, 248, 0.3);
  line-height: 1.2;
}

.restaurant-subtitle {
  font-size: 1rem;
  color: var(--text-secondary);
  margin: 0;
  font-weight: 500;
  opacity: 0.9;
}

/* Loading & Error States */
.loading-container,
.error-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 50vh;
  padding: 2rem;
}

.loading-spinner {
  width: 50px;
  height: 50px;
  border: 4px solid rgba(129, 140, 248, 0.3);
  border-top-color: #818cf8;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.loading-text,
.error-text {
  margin-top: 1rem;
  font-size: 1.125rem;
  color: rgba(255, 255, 255, 0.8);
}

.error-icon {
  font-size: 3rem;
  color: #ef4444;
  margin-bottom: 1rem;
}

/* Category Filter */
.category-filter-wrapper {
  position: sticky;
  top: 0;
  z-index: 10;
  background: rgba(15, 23, 42, 0.95);
  backdrop-filter: blur(15px);
  border-bottom: 1px solid var(--border-color);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
}

.category-filter {
  display: flex;
  gap: 0.75rem;
  padding: 1.5rem 2rem;
  overflow-x: auto;
  overflow-y: hidden;
  scroll-behavior: smooth;
  -webkit-overflow-scrolling: touch;
  scrollbar-width: thin;
  scrollbar-color: var(--primary-color) transparent;
}

.category-filter::-webkit-scrollbar {
  height: 6px;
}

.category-filter::-webkit-scrollbar-track {
  background: transparent;
}

.category-filter::-webkit-scrollbar-thumb {
  background: var(--primary-color);
  border-radius: 3px;
}

.category-filter::-webkit-scrollbar-thumb:hover {
  background: var(--primary-dark);
}

.category-btn {
  padding: 0.875rem 1.75rem;
  background: var(--bg-tertiary);
  border: 2px solid var(--border-color);
  border-radius: 2rem;
  color: var(--text-primary);
  font-weight: 600;
  font-size: 0.9375rem;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  display: flex;
  align-items: center;
  white-space: nowrap;
  flex-shrink: 0;
  position: relative;
  overflow: hidden;
}

.category-btn::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.1), transparent);
  transition: left 0.5s ease;
}

.category-btn:hover::before {
  left: 100%;
}

.category-btn:hover {
  background: rgba(129, 140, 248, 0.15);
  border-color: var(--primary-color);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(129, 140, 248, 0.3);
}

.category-btn.active {
  background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-dark) 100%);
  border-color: var(--primary-color);
  color: #ffffff;
  box-shadow: 0 4px 16px rgba(129, 140, 248, 0.5);
  transform: translateY(-2px) scale(1.05);
}

.category-btn.active::before {
  display: none;
}

.category-btn .me-2 {
  margin-left: 0.5rem;
  margin-right: 0;
}

/* Menu Content */
.menu-content {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
}

.menu-items-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1.5rem;
  margin-top: 2rem;
}

/* Menu Item Card */
.menu-item-card {
  background: rgba(30, 41, 59, 0.8);
  border-radius: 1rem;
  overflow: hidden;
  transition: all 0.3s ease;
  border: 1px solid rgba(129, 140, 248, 0.2);
  backdrop-filter: blur(10px);
  display: flex;
  flex-direction: column;
}

.menu-item-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 12px 24px rgba(0, 0, 0, 0.4);
  border-color: rgba(129, 140, 248, 0.5);
}

.item-image-container {
  position: relative;
  width: 100%;
  height: 200px;
  overflow: hidden;
  background: rgba(15, 23, 42, 0.5);
}

.item-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s ease;
}

.menu-item-card:hover .item-image {
  transform: scale(1.1);
}

.item-image-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(129, 140, 248, 0.1);
}

.placeholder-icon {
  font-size: 3rem;
  color: rgba(129, 140, 248, 0.4);
}

.discount-badge {
  position: absolute;
  top: 0.75rem;
  right: 0.75rem;
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
  color: #ffffff;
  padding: 0.375rem 0.75rem;
  border-radius: 0.5rem;
  font-size: 0.75rem;
  font-weight: 700;
  box-shadow: 0 2px 8px rgba(239, 68, 68, 0.4);
}

.item-content {
  padding: 1.25rem;
  flex: 1;
  display: flex;
  flex-direction: column;
}

.item-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 0.75rem;
  gap: 0.5rem;
}

.item-name {
  font-size: 1.25rem;
  font-weight: 700;
  color: #ffffff;
  margin: 0;
  flex: 1;
}

.item-category {
  background: rgba(129, 140, 248, 0.2);
  color: #818cf8;
  padding: 0.25rem 0.75rem;
  border-radius: 0.5rem;
  font-size: 0.75rem;
  font-weight: 600;
  white-space: nowrap;
}

.item-description {
  color: rgba(255, 255, 255, 0.7);
  font-size: 0.9375rem;
  line-height: 1.6;
  margin-bottom: 1rem;
  flex: 1;
}

.item-footer {
  margin-top: auto;
}

.item-price {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.original-price {
  color: rgba(255, 255, 255, 0.5);
  text-decoration: line-through;
  font-size: 0.875rem;
}

.current-price {
  font-size: 1.5rem;
  font-weight: 700;
  color: #818cf8;
}

/* Empty State */
.empty-state {
  text-align: center;
  padding: 4rem 2rem;
}

.empty-icon {
  font-size: 4rem;
  color: rgba(129, 140, 248, 0.4);
  margin-bottom: 1rem;
}

.empty-text {
  font-size: 1.125rem;
  color: rgba(255, 255, 255, 0.6);
}

/* Footer */
.public-menu-footer {
  background: rgba(15, 23, 42, 0.8);
  padding: 2rem;
  text-align: center;
  margin-top: 4rem;
}

.footer-text {
  color: rgba(255, 255, 255, 0.5);
  font-size: 0.875rem;
  margin: 0;
}

/* Responsive Design */
@media (max-width: 768px) {
  .public-menu-header {
    padding: 2rem 1.5rem;
  }

  .header-content {
    max-width: 100%;
  }

  .menu-logo {
    max-width: 100px;
    max-height: 100px;
  }

  .logo-placeholder {
    width: 80px;
    height: 80px;
  }

  .logo-icon {
    font-size: 2rem;
  }

  .restaurant-name {
    font-size: 1.875rem;
  }

  .restaurant-subtitle {
    font-size: 0.9375rem;
  }

  .menu-content {
    padding: 1.5rem 1rem;
  }

  .menu-items-grid {
    grid-template-columns: repeat(2, 1fr);
    gap: 1rem;
  }

  .category-filter-wrapper {
    position: sticky;
    top: 0;
  }

  .category-filter {
    padding: 1rem;
    gap: 0.625rem;
    padding-right: 1rem;
    padding-left: 1rem;
  }

  .category-btn {
    padding: 0.75rem 1.5rem;
    font-size: 0.875rem;
    border-radius: 1.5rem;
  }

  .item-image-container {
    height: 160px;
  }

  .item-content {
    padding: 1rem;
  }

  .item-name {
    font-size: 1.125rem;
  }

  .item-description {
    font-size: 0.875rem;
  }

  .current-price {
    font-size: 1.25rem;
  }
}

@media (max-width: 480px) {
  .public-menu-header {
    padding: 1.5rem 1rem;
  }

  .menu-logo {
    max-width: 80px;
    max-height: 80px;
  }

  .logo-placeholder {
    width: 70px;
    height: 70px;
  }

  .logo-icon {
    font-size: 1.75rem;
  }

  .restaurant-name {
    font-size: 1.5rem;
  }

  .restaurant-subtitle {
    font-size: 0.875rem;
  }

  .menu-content {
    padding: 1rem 0.75rem;
  }

  .menu-items-grid {
    grid-template-columns: repeat(2, 1fr);
    gap: 0.75rem;
  }

  .item-image-container {
    height: 140px;
  }

  .item-content {
    padding: 0.875rem;
  }

  .item-name {
    font-size: 1rem;
    line-height: 1.3;
  }

  .item-description {
    font-size: 0.8125rem;
    margin-bottom: 0.75rem;
  }

  .current-price {
    font-size: 1.125rem;
  }

  .original-price {
    font-size: 0.75rem;
  }

  .category-filter {
    padding: 0.875rem 0.75rem;
    gap: 0.5rem;
  }

  .category-btn {
    padding: 0.625rem 1.25rem;
    font-size: 0.8125rem;
    border-radius: 1.25rem;
  }
}
</style>

