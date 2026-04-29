<template>
  <div class="public-order-container">
    <!-- Header Section -->
    <header class="public-order-header">
      <div class="header-content">
        <div class="logo-section">
          <img 
            v-if="restaurantLogo && !logoError" 
            :src="restaurantLogo" 
            alt="Logo" 
            class="order-logo"
            @error="logoError = true"
          />
          <img 
            v-else-if="!restaurantLogo && !logoError"
            src="../assets/logoarabicdark.png" 
            alt="Logo" 
            class="order-logo"
            @error="logoError = true"
          />
          <div v-else class="logo-placeholder">
            <b-icon icon="shop" class="logo-icon"></b-icon>
          </div>
        </div>
        <h1 class="restaurant-name">{{ restaurantName || 'الطلب' }}</h1>
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

    <!-- Order Content -->
    <div v-else class="order-content">
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
          @click="addToCart(item)"
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
              <button class="add-btn">
                <b-icon icon="plus-circle-fill"></b-icon>
              </button>
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

    <!-- Cart Section (Fixed at Bottom) -->
    <div v-if="cartItems.length > 0" class="cart-section">
      <div class="cart-header" @click="showCart = !showCart">
        <div class="cart-info">
          <b-icon icon="cart-fill" class="cart-icon"></b-icon>
          <span class="cart-count">{{ totalItems }}</span>
          <span class="cart-label">عنصر</span>
        </div>
        <div class="cart-total">
          <span class="total-label">المجموع:</span>
          <span class="total-amount">{{ formatPrice(cartTotal) }} د.ع</span>
        </div>
        <b-icon :icon="showCart ? 'chevron-up' : 'chevron-down'" class="cart-toggle-icon"></b-icon>
      </div>

      <div v-if="showCart" class="cart-content">
        <div class="cart-items">
          <div 
            v-for="(cartItem, index) in cartItems" 
            :key="index"
            class="cart-item"
          >
            <div class="cart-item-info">
              <h4 class="cart-item-name">{{ cartItem.name }}</h4>
              <p class="cart-item-price">{{ formatPrice(cartItem.price * cartItem.quantity) }} د.ع</p>
            </div>
            <div class="cart-item-controls">
              <button class="quantity-btn" @click="decreaseQuantity(index)">
                <b-icon icon="dash"></b-icon>
              </button>
              <span class="quantity-value">{{ cartItem.quantity }}</span>
              <button class="quantity-btn" @click="increaseQuantity(index)">
                <b-icon icon="plus"></b-icon>
              </button>
              <button class="remove-btn" @click="removeFromCart(index)">
                <b-icon icon="x"></b-icon>
              </button>
            </div>
          </div>
        </div>

        <!-- Payment Method Selection -->
        <div class="payment-section">
          <h3 class="payment-title">طريقة الدفع</h3>
          <div class="payment-options">
            <button 
              class="payment-option"
              :class="{ active: paymentMethod === 'Cash' }"
              @click="paymentMethod = 'Cash'"
            >
              <b-icon icon="cash-coin" class="payment-icon"></b-icon>
              <span>كاش</span>
            </button>
            <button 
              class="payment-option"
              :class="{ active: paymentMethod === 'Card' }"
              @click="paymentMethod = 'Card'"
            >
              <b-icon icon="credit-card" class="payment-icon"></b-icon>
              <span>بطاقة</span>
            </button>
          </div>
        </div>

        <!-- Notes Section -->
        <div class="notes-section">
          <label class="notes-label">ملاحظات (اختياري)</label>
          <textarea 
            v-model="orderNotes" 
            class="notes-input"
            placeholder="أضف ملاحظات للطلب..."
            rows="2"
          ></textarea>
        </div>

        <!-- Order Button -->
        <button 
          class="order-btn"
          :disabled="submitting"
          @click="submitOrder"
        >
          <b-icon v-if="!submitting" icon="check-circle-fill" class="me-2"></b-icon>
          <span v-if="submitting">جاري إرسال الطلب...</span>
          <span v-else>تأكيد الطلب</span>
        </button>
      </div>
    </div>

    <!-- Success Modal -->
    <b-modal 
      v-model="showSuccessModal" 
      title="تم بنجاح" 
      ok-only
      @ok="resetOrder"
      centered
      modal-class="success-modal"
      header-class="success-modal-header"
      body-class="success-modal-body"
      footer-class="success-modal-footer"
      ok-title="حسناً"
      ok-variant="primary"
      hide-header-close
    >
      <div class="success-content">
        <div class="success-icon-wrapper">
          <b-icon icon="check-circle-fill" class="success-icon"></b-icon>
          <div class="success-icon-ring"></div>
        </div>
        <h3 class="success-title">تم إرسال الطلب بنجاح!</h3>
        <div class="order-code-wrapper">
          <span class="order-code-label">رقم الطلب:</span>
          <span class="order-code-value">{{ orderCode }}</span>
        </div>
        <p class="success-message">شكراً لك، سيتم تحضير طلبك قريباً</p>
      </div>
    </b-modal>
  </div>
</template>

<script>
import { HTTP } from '../http/api.js';

export default {
  name: 'PublicOrderView',
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
      commercialUserId: null,
      cartItems: [],
      showCart: true,
      paymentMethod: 'Cash',
      orderNotes: '',
      submitting: false,
      showSuccessModal: false,
      orderCode: ''
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
      return [...this.categories].sort((a, b) => {
        return a.localeCompare(b, 'ar');
      });
    },
    totalItems() {
      return this.cartItems.reduce((sum, item) => sum + item.quantity, 0);
    },
    cartTotal() {
      return this.cartItems.reduce((sum, item) => sum + (item.price * item.quantity), 0);
    }
  },
  mounted() {
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
        this.categories = [];
      }
    },
    addToCart(item) {
      const existingItem = this.cartItems.find(cartItem => cartItem.id === item.id);
      const price = item.discountPrice || item.sellingPrice;
      
      if (existingItem) {
        existingItem.quantity++;
      } else {
        this.cartItems.push({
          id: item.id,
          name: item.name,
          price: price,
          quantity: 1
        });
      }
      
      // Don't auto-open cart - let user decide when to view it
    },
    increaseQuantity(index) {
      this.cartItems[index].quantity++;
    },
    decreaseQuantity(index) {
      if (this.cartItems[index].quantity > 1) {
        this.cartItems[index].quantity--;
      } else {
        this.removeFromCart(index);
      }
    },
    removeFromCart(index) {
      this.cartItems.splice(index, 1);
    },
    async submitOrder() {
      if (this.cartItems.length === 0) {
        return;
      }

      try {
        this.submitting = true;

        const orderItems = this.cartItems.map(item => ({
          ItemId: item.id,
          Quantity: item.quantity
        }));

        const orderRequest = {
          PaymentMethod: this.paymentMethod,
          CustomerOrderItem: orderItems,
          OrderType: 'Takeaway',
          Notes: this.orderNotes || null
        };

        const response = await HTTP.post(`PublicMenu/${this.commercialUserId}/order`, orderRequest);

        if (response.data && !response.data.errorStatus) {
          // Handle both OrderCode and orderCode formats
          this.orderCode = response.data.data?.OrderCode || response.data.data?.orderCode || '';
          this.showSuccessModal = true;
        } else {
          this.$bvToast.toast(response.data?.message || 'حدث خطأ أثناء إرسال الطلب', {
            title: 'خطأ',
            variant: 'danger',
            solid: true
          });
        }
      } catch (err) {
        console.error('Error submitting order:', err);
        this.$bvToast.toast(err.response?.data?.message || 'حدث خطأ أثناء إرسال الطلب', {
          title: 'خطأ',
          variant: 'danger',
          solid: true
        });
      } finally {
        this.submitting = false;
      }
    },
    resetOrder() {
      this.cartItems = [];
      this.orderNotes = '';
      this.paymentMethod = 'Cash';
      this.showSuccessModal = false;
      this.orderCode = '';
    },
    formatPrice(price) {
      return new Intl.NumberFormat('ar-IQ').format(price);
    }
  }
};
</script>

<style scoped>
.public-order-container {
  min-height: 100vh;
  background: linear-gradient(135deg, #0f172a 0%, #1e293b 50%, #334155 100%);
  color: #ffffff;
  padding-bottom: 200px; /* Space for cart */
}

/* Header Styles */
.public-order-header {
  background: linear-gradient(135deg, var(--bg-primary) 0%, var(--bg-tertiary) 50%, var(--bg-primary) 100%);
  padding: 1.5rem 1rem;
  text-align: center;
  box-shadow: var(--shadow-lg);
  position: sticky;
  top: 0;
  z-index: 100;
  border-bottom: 2px solid var(--border-color);
}

.header-content {
  max-width: 100%;
  margin: 0 auto;
}

.logo-section {
  margin-bottom: 0.75rem;
}

.order-logo {
  max-width: 80px;
  max-height: 80px;
  height: auto;
  width: auto;
  object-fit: contain;
  filter: drop-shadow(0 4px 12px rgba(0, 0, 0, 0.4));
}

.logo-placeholder {
  width: 70px;
  height: 70px;
  margin: 0 auto;
  background: linear-gradient(135deg, rgba(129, 140, 248, 0.2) 0%, rgba(99, 102, 241, 0.2) 100%);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 2px solid var(--primary-color);
}

.logo-icon {
  font-size: 2rem;
  color: var(--primary-color);
}

.restaurant-name {
  font-size: 1.5rem;
  font-weight: 800;
  margin: 0;
  background: linear-gradient(135deg, #ffffff 0%, var(--primary-color) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
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
  gap: 0.5rem;
  padding: 1rem;
  overflow-x: auto;
  overflow-y: hidden;
  scroll-behavior: smooth;
  -webkit-overflow-scrolling: touch;
}

.category-btn {
  padding: 0.75rem 1.25rem;
  background: var(--bg-tertiary);
  border: 2px solid var(--border-color);
  border-radius: 1.5rem;
  color: var(--text-primary);
  font-weight: 600;
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.3s ease;
  white-space: nowrap;
  flex-shrink: 0;
  display: flex;
  align-items: center;
}

.category-btn:hover {
  background: rgba(129, 140, 248, 0.15);
  border-color: var(--primary-color);
}

.category-btn.active {
  background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-dark) 100%);
  border-color: var(--primary-color);
  color: #ffffff;
}

/* Menu Content */
.order-content {
  padding: 1rem;
  max-width: 100%;
  margin: 0 auto;
}

.menu-items-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 0.75rem;
  margin-top: 1rem;
}

/* Menu Item Card */
.menu-item-card {
  background: rgba(30, 41, 59, 0.8);
  border-radius: 0.75rem;
  overflow: hidden;
  transition: all 0.3s ease;
  border: 1px solid rgba(129, 140, 248, 0.2);
  backdrop-filter: blur(10px);
  display: flex;
  flex-direction: column;
  cursor: pointer;
}

.menu-item-card:active {
  transform: scale(0.98);
}

.item-image-container {
  position: relative;
  width: 100%;
  height: 120px;
  overflow: hidden;
  background: rgba(15, 23, 42, 0.5);
}

.item-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
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
  font-size: 2rem;
  color: rgba(129, 140, 248, 0.4);
}

.discount-badge {
  position: absolute;
  top: 0.5rem;
  right: 0.5rem;
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
  color: #ffffff;
  padding: 0.25rem 0.5rem;
  border-radius: 0.375rem;
  font-size: 0.625rem;
  font-weight: 700;
}

.item-content {
  padding: 0.75rem;
  flex: 1;
  display: flex;
  flex-direction: column;
}

.item-header {
  margin-bottom: 0.5rem;
}

.item-name {
  font-size: 0.9375rem;
  font-weight: 700;
  color: #ffffff;
  margin: 0;
  line-height: 1.3;
}

.item-description {
  color: rgba(255, 255, 255, 0.6);
  font-size: 0.75rem;
  line-height: 1.4;
  margin-bottom: 0.5rem;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.item-footer {
  margin-top: auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.5rem;
}

.item-price {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.original-price {
  color: rgba(255, 255, 255, 0.5);
  text-decoration: line-through;
  font-size: 0.625rem;
}

.current-price {
  font-size: 1rem;
  font-weight: 700;
  color: #818cf8;
}

.add-btn {
  background: var(--primary-color);
  border: none;
  border-radius: 50%;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #ffffff;
  cursor: pointer;
  transition: all 0.3s ease;
  flex-shrink: 0;
}

.add-btn:active {
  transform: scale(0.9);
}

/* Cart Section */
.cart-section {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: rgba(15, 23, 42, 0.98);
  backdrop-filter: blur(20px);
  border-top: 2px solid var(--primary-color);
  box-shadow: 0 -4px 20px rgba(0, 0, 0, 0.5);
  z-index: 1000;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  animation: slideUpCart 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);
  transform-origin: bottom;
}

@keyframes slideUpCart {
  0% {
    transform: translateY(100%);
    opacity: 0;
  }
  60% {
    transform: translateY(-5px);
  }
  100% {
    transform: translateY(0);
    opacity: 1;
  }
}

.cart-section::before {
  content: '';
  position: absolute;
  top: -2px;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(90deg, 
    transparent, 
    var(--primary-color), 
    var(--primary-color), 
    transparent
  );
  animation: shimmer 2s infinite;
}

@keyframes shimmer {
  0%, 100% {
    opacity: 0.5;
    transform: scaleX(0.8);
  }
  50% {
    opacity: 1;
    transform: scaleX(1);
  }
}

.cart-header {
  padding: 1rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  cursor: pointer;
  border-bottom: 1px solid var(--border-color);
  transition: all 0.3s ease;
  position: relative;
}

.cart-header:hover {
  background: rgba(129, 140, 248, 0.05);
}

.cart-header:active {
  transform: scale(0.98);
}

.cart-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.cart-icon {
  font-size: 1.5rem;
  color: var(--primary-color);
}

.cart-count {
  background: var(--primary-color);
  color: #ffffff;
  padding: 0.25rem 0.5rem;
  border-radius: 1rem;
  font-weight: 700;
  font-size: 0.875rem;
  animation: pulseCount 2s infinite;
  box-shadow: 0 0 10px rgba(129, 140, 248, 0.5);
}

@keyframes pulseCount {
  0%, 100% {
    transform: scale(1);
    box-shadow: 0 0 10px rgba(129, 140, 248, 0.5);
  }
  50% {
    transform: scale(1.1);
    box-shadow: 0 0 20px rgba(129, 140, 248, 0.8);
  }
}

.cart-label {
  font-size: 0.875rem;
  color: rgba(255, 255, 255, 0.8);
}

.cart-total {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
}

.total-label {
  font-size: 0.75rem;
  color: rgba(255, 255, 255, 0.6);
}

.total-amount {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--primary-color);
  animation: glowText 2s ease-in-out infinite;
}

@keyframes glowText {
  0%, 100% {
    text-shadow: 0 0 5px rgba(129, 140, 248, 0.5);
  }
  50% {
    text-shadow: 0 0 15px rgba(129, 140, 248, 0.8), 0 0 25px rgba(129, 140, 248, 0.5);
  }
}

.cart-toggle-icon {
  font-size: 1.25rem;
  color: rgba(255, 255, 255, 0.8);
}

.cart-content {
  max-height: calc(90vh - 80px);
  overflow-y: auto;
  padding: 1rem;
  animation: fadeInContent 0.3s ease-in;
}

@keyframes fadeInContent {
  0% {
    opacity: 0;
    transform: translateY(10px);
  }
  100% {
    opacity: 1;
    transform: translateY(0);
  }
}

.cart-items {
  margin-bottom: 1rem;
}

.cart-item {
  background: rgba(30, 41, 59, 0.8);
  border-radius: 0.75rem;
  padding: 1rem;
  margin-bottom: 0.75rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
}

.cart-item-info {
  flex: 1;
}

.cart-item-name {
  font-size: 0.9375rem;
  font-weight: 600;
  color: #ffffff;
  margin: 0 0 0.25rem 0;
}

.cart-item-price {
  font-size: 0.875rem;
  color: var(--primary-color);
  font-weight: 600;
  margin: 0;
}

.cart-item-controls {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.quantity-btn {
  background: rgba(129, 140, 248, 0.2);
  border: 1px solid var(--primary-color);
  border-radius: 0.5rem;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #ffffff;
  cursor: pointer;
  transition: all 0.3s ease;
}

.quantity-btn:active {
  transform: scale(0.9);
}

.quantity-value {
  min-width: 30px;
  text-align: center;
  font-weight: 600;
  color: #ffffff;
}

.remove-btn {
  background: rgba(239, 68, 68, 0.2);
  border: 1px solid #ef4444;
  border-radius: 0.5rem;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #ef4444;
  cursor: pointer;
  transition: all 0.3s ease;
}

.remove-btn:active {
  transform: scale(0.9);
}

/* Payment Section */
.payment-section {
  margin-bottom: 1rem;
  padding: 1rem;
  background: rgba(30, 41, 59, 0.5);
  border-radius: 0.75rem;
}

.payment-title {
  font-size: 1rem;
  font-weight: 600;
  color: #ffffff;
  margin: 0 0 0.75rem 0;
}

.payment-options {
  display: flex;
  gap: 0.75rem;
}

.payment-option {
  flex: 1;
  padding: 1rem;
  background: rgba(30, 41, 59, 0.8);
  border: 2px solid var(--border-color);
  border-radius: 0.75rem;
  color: #ffffff;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
}

.payment-option:active {
  transform: scale(0.98);
}

.payment-option.active {
  background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-dark) 100%);
  border-color: var(--primary-color);
}

.payment-icon {
  font-size: 1.5rem;
}

/* Notes Section */
.notes-section {
  margin-bottom: 1rem;
}

.notes-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.8);
  margin-bottom: 0.5rem;
}

.notes-input {
  width: 100%;
  padding: 0.75rem;
  background: rgba(30, 41, 59, 0.8);
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  color: #ffffff;
  font-size: 0.875rem;
  resize: vertical;
  font-family: inherit;
}

.notes-input:focus {
  outline: none;
  border-color: var(--primary-color);
}

.notes-input::placeholder {
  color: rgba(255, 255, 255, 0.5);
}

/* Order Button */
.order-btn {
  width: 100%;
  padding: 1rem;
  background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-dark) 100%);
  border: none;
  border-radius: 0.75rem;
  color: #ffffff;
  font-size: 1.125rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.3s ease;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  position: relative;
  overflow: hidden;
  box-shadow: 0 4px 15px rgba(129, 140, 248, 0.4);
  animation: buttonPulse 2s ease-in-out infinite;
}

@keyframes buttonPulse {
  0%, 100% {
    box-shadow: 0 4px 15px rgba(129, 140, 248, 0.4);
  }
  50% {
    box-shadow: 0 4px 25px rgba(129, 140, 248, 0.7), 0 0 30px rgba(129, 140, 248, 0.3);
  }
}

.order-btn::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent);
  transition: left 0.5s ease;
}

.order-btn:hover::before {
  left: 100%;
}

.order-btn:active:not(:disabled) {
  transform: scale(0.98);
}

.order-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  animation: none;
}

/* Success Modal - Enhanced Styles */
::v-deep .success-modal .modal-content {
  border: none;
  border-radius: 1.5rem;
  overflow: hidden;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3), 0 0 40px rgba(129, 140, 248, 0.2);
  animation: modalSlideIn 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);
}

@keyframes modalSlideIn {
  0% {
    transform: scale(0.8) translateY(-20px);
    opacity: 0;
  }
  100% {
    transform: scale(1) translateY(0);
    opacity: 1;
  }
}

::v-deep .success-modal-header {
  background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-dark) 100%);
  color: #ffffff;
  border-bottom: none;
  padding: 1.5rem;
  position: relative;
  overflow: hidden;
}

::v-deep .success-modal-header::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.1), transparent);
  animation: shimmerHeader 3s infinite;
}

@keyframes shimmerHeader {
  0% {
    left: -100%;
  }
  100% {
    left: 100%;
  }
}

::v-deep .success-modal-header .modal-title {
  font-family: 'Cairo', sans-serif;
  font-size: 1.5rem;
  font-weight: 800;
  color: #ffffff;
  text-shadow: 0 2px 10px rgba(0, 0, 0, 0.2);
}

::v-deep .success-modal-header .close {
  display: none !important;
}

::v-deep .success-modal-body {
  background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%);
  padding: 2.5rem 2rem;
  color: #ffffff;
  font-family: 'Cairo', sans-serif;
}

::v-deep .success-modal-footer {
  background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%);
  border-top: 1px solid rgba(129, 140, 248, 0.2);
  padding: 1rem 1.5rem;
  display: flex;
  justify-content: center;
}

::v-deep .success-modal-footer .btn-primary {
  background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-dark) 100%);
  border: none;
  border-radius: 0.75rem;
  padding: 0.875rem 2.5rem;
  font-family: 'Cairo', sans-serif;
  font-weight: 700;
  font-size: 1rem;
  box-shadow: 0 4px 15px rgba(129, 140, 248, 0.4);
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
}

::v-deep .success-modal-footer .btn-primary::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent);
  transition: left 0.5s ease;
}

::v-deep .success-modal-footer .btn-primary:hover::before {
  left: 100%;
}

::v-deep .success-modal-footer .btn-primary:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(129, 140, 248, 0.6);
}

::v-deep .success-modal-footer .btn-primary:active {
  transform: translateY(0);
}

.success-content {
  text-align: center;
  padding: 0;
  position: relative;
}

.success-icon-wrapper {
  position: relative;
  display: inline-block;
  margin-bottom: 1.5rem;
}

.success-icon {
  font-size: 5rem;
  color: #10b981;
  position: relative;
  z-index: 2;
  animation: successIconPop 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
  filter: drop-shadow(0 0 20px rgba(16, 185, 129, 0.5));
}

@keyframes successIconPop {
  0% {
    transform: scale(0);
    opacity: 0;
  }
  50% {
    transform: scale(1.2);
  }
  100% {
    transform: scale(1);
    opacity: 1;
  }
}

.success-icon-ring {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 120px;
  height: 120px;
  border: 3px solid rgba(16, 185, 129, 0.3);
  border-radius: 50%;
  animation: ringPulse 2s ease-in-out infinite;
}

@keyframes ringPulse {
  0%, 100% {
    transform: translate(-50%, -50%) scale(1);
    opacity: 0.5;
  }
  50% {
    transform: translate(-50%, -50%) scale(1.2);
    opacity: 0.2;
  }
}

.success-title {
  font-family: 'Cairo', sans-serif;
  color: #10b981;
  font-size: 1.75rem;
  font-weight: 800;
  margin: 0 0 1.5rem 0;
  animation: fadeInUp 0.5s ease 0.2s both;
  text-shadow: 0 2px 10px rgba(16, 185, 129, 0.3);
}

@keyframes fadeInUp {
  0% {
    opacity: 0;
    transform: translateY(20px);
  }
  100% {
    opacity: 1;
    transform: translateY(0);
  }
}

.order-code-wrapper {
  background: rgba(129, 140, 248, 0.1);
  border: 2px solid rgba(129, 140, 248, 0.3);
  border-radius: 1rem;
  padding: 1rem 1.5rem;
  margin: 1.5rem 0;
  display: inline-flex;
  align-items: center;
  gap: 0.75rem;
  animation: fadeInUp 0.5s ease 0.4s both;
  backdrop-filter: blur(10px);
}

.order-code-label {
  font-family: 'Cairo', sans-serif;
  font-size: 1rem;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.8);
}

.order-code-value {
  font-family: 'Cairo', sans-serif;
  font-size: 1.5rem;
  font-weight: 800;
  color: var(--primary-color);
  text-shadow: 0 0 10px rgba(129, 140, 248, 0.5);
  letter-spacing: 0.05em;
}

.success-message {
  font-family: 'Cairo', sans-serif;
  color: rgba(255, 255, 255, 0.7);
  font-size: 1.125rem;
  margin-top: 1.5rem;
  line-height: 1.6;
  animation: fadeInUp 0.5s ease 0.6s both;
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

/* Responsive - Landscape orientation */
@media (orientation: landscape) and (max-height: 600px) {
  .menu-items-grid {
    grid-template-columns: repeat(3, 1fr);
  }
  
  .item-image-container {
    height: 100px;
  }
}

/* Larger screens */
@media (min-width: 768px) {
  .menu-items-grid {
    grid-template-columns: repeat(3, 1fr);
    gap: 1rem;
  }
  
  .item-image-container {
    height: 160px;
  }
  
  .cart-section {
    max-width: 500px;
    left: 50%;
    transform: translateX(-50%);
  }
}
</style>

