<template>
  <b-overlay
    :show="loading"
    spinner-variant="primary"
    spinner-type="grow"
    spinner-large
    rounded="sm"
  >
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content order-queue-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="list-task" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("orderQueue") || "طابور الطلبات" }}</h1>
                  <p class="header-subtitle">{{ $t("orderQueueDescription") || "إدارة ومتابعة الطلبات حسب الحالة" }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button type="button" class="btn-refresh" @click="loadOrders({ silent: false })" :disabled="loading">
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loading }"></b-icon>
                  <span class="button-text">{{ $t("refresh") || "تحديث" }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-overview-grid">
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                <b-icon icon="clock-history"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ pendingOrders.length }}</div>
                <div class="app-overview-stat-label">{{ $t("pending") || "قيد الانتظار" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="gear"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ processingOrders.length }}</div>
                <div class="app-overview-stat-label">{{ $t("processing") || "قيد المعالجة" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="check-circle"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ readyOrders.length }}</div>
                <div class="app-overview-stat-label">{{ $t("ready") || "جاهز" }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                <b-icon icon="list-check"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">{{ activeOrdersCount }}</div>
                <div class="app-overview-stat-label">{{ $t("activeOrders") || "طلبات نشطة" }}</div>
              </div>
            </div>
          </div>

          <div class="app-section-card app-section-card--flush">
            <div class="app-section-body">
              <div class="users-search-container order-queue-filter-wrap">
                <b-icon icon="filter" class="search-icon"></b-icon>
                <select
                  v-model="orderTypeFilter"
                  class="users-search-input order-queue-filter-select"
                  @change="loadOrders({ silent: true })"
                >
                  <option value="">{{ $t("allOrderTypes") || "جميع الأنواع" }}</option>
                  <option value="DineIn">{{ $t("dineIn") || "داخل المطعم" }}</option>
                  <option value="Takeaway">{{ $t("takeaway") || "خارجي" }}</option>
                  <option value="Delivery">{{ $t("delivery") || "توصيل" }}</option>
                </select>
              </div>
            </div>
          </div>

          <div class="queue-board order-queue-board">
            <!-- Pending Column -->
            <div class="queue-column">
              <div class="queue-column-header pending">
                <div class="column-header-content">
                  <b-icon icon="clock-history" class="column-icon"></b-icon>
                  <h3 class="column-title">{{ $t("pending") || "قيد الانتظار" }}</h3>
                  <span class="column-count">{{ pendingOrders.length }}</span>
                </div>
              </div>
              <div class="queue-column-body">
                <div 
                  class="queue-card" 
                  v-for="order in pendingOrders" 
                  :key="order.id"
                  @click="selectOrder(order)"
                >
                  <div class="queue-card-header">
                    <div class="order-code-badge">{{ order.orderCode }}</div>
                    <div class="order-type-badge" :class="getOrderTypeClass(order.orderType)">
                      {{ getOrderTypeText(order.orderType) }}
                    </div>
                  </div>
                  <div v-if="order.hiddenFromQueueDisplay" class="queue-hidden-badge">
                    <b-icon icon="eye-slash" class="me-1"></b-icon>
                    {{ $t('hiddenFromQueueDisplay') || 'مخفي من الشاشة' }}
                  </div>
                  <div class="queue-card-body">
                    <div class="order-info-item">
                      <b-icon icon="hash" class="info-icon"></b-icon>
                      <span>{{ $t("orderNumber") || "رقم الطلب" }}: {{ order.dailySequenceNumber || order.id }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="box-seam" class="info-icon"></b-icon>
                      <span>{{ $t("itemsCount") || "عدد العناصر" }}: {{ order.itemsCount || 0 }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="currency-dollar" class="info-icon"></b-icon>
                      <span>{{ formatPrice(order.orderTotalAfterDiscount ?? order.orderPrice ?? 0) }} {{ $t("currency") }}</span>
                    </div>
                    <div v-if="order.deliveryDriver" class="order-info-item">
                      <b-icon icon="truck" class="info-icon"></b-icon>
                      <span>{{ order.deliveryDriver.name }}</span>
                    </div>
                    <div v-if="order.notes" class="order-info-item">
                      <b-icon icon="chat-left-text" class="info-icon"></b-icon>
                      <span class="order-notes">{{ order.notes }}</span>
                    </div>
                  </div>
                  <div class="queue-card-footer">
                    <button 
                      class="queue-action-btn processing-btn" 
                      @click.stop="updateOrderStatus(order.id, 'Processing')"
                    >
                      <b-icon icon="play-circle" class="me-1"></b-icon>
                      {{ $t("startProcessing") || "بدء المعالجة" }}
                    </button>
                    <button
                      v-if="canPrintOrder(order.orderStatus)"
                      type="button"
                      class="queue-action-btn print-order-btn"
                      :disabled="printingOrderId === order.id"
                      @click.stop="printOrder(order)"
                    >
                      <b-icon icon="printer-fill" class="me-1"></b-icon>
                      {{ $t('printOrder') || 'طباعة' }}
                    </button>
                    <button
                      v-if="!order.hiddenFromQueueDisplay"
                      type="button"
                      class="queue-action-btn hide-display-btn"
                      @click.stop="hideFromQueueDisplay(order.id)"
                    >
                      <b-icon icon="eye-slash" class="me-1"></b-icon>
                      {{ $t('removeFromQueueDisplay') || 'إزالة من شاشة الانتظار' }}
                    </button>
                  </div>
                </div>
                <div v-if="pendingOrders.length === 0" class="queue-empty-state">
                  <b-icon icon="inbox" class="empty-icon"></b-icon>
                  <p class="empty-text">{{ $t("noPendingOrders") || "لا توجد طلبات قيد الانتظار" }}</p>
                </div>
              </div>
            </div>

            <!-- Processing Column -->
            <div class="queue-column">
              <div class="queue-column-header processing">
                <div class="column-header-content">
                  <b-icon icon="gear" class="column-icon"></b-icon>
                  <h3 class="column-title">{{ $t("processing") || "قيد المعالجة" }}</h3>
                  <span class="column-count">{{ processingOrders.length }}</span>
                </div>
              </div>
              <div class="queue-column-body">
                <div 
                  class="queue-card" 
                  v-for="order in processingOrders" 
                  :key="order.id"
                  @click="selectOrder(order)"
                >
                  <div class="queue-card-header">
                    <div class="order-code-badge">{{ order.orderCode }}</div>
                    <div class="order-type-badge" :class="getOrderTypeClass(order.orderType)">
                      {{ getOrderTypeText(order.orderType) }}
                    </div>
                  </div>
                  <div v-if="order.hiddenFromQueueDisplay" class="queue-hidden-badge">
                    <b-icon icon="eye-slash" class="me-1"></b-icon>
                    {{ $t('hiddenFromQueueDisplay') || 'مخفي من الشاشة' }}
                  </div>
                  <div class="queue-card-body">
                    <div class="order-info-item">
                      <b-icon icon="hash" class="info-icon"></b-icon>
                      <span>{{ $t("orderNumber") || "رقم الطلب" }}: {{ order.dailySequenceNumber || order.id }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="box-seam" class="info-icon"></b-icon>
                      <span>{{ $t("itemsCount") || "عدد العناصر" }}: {{ order.itemsCount || 0 }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="currency-dollar" class="info-icon"></b-icon>
                      <span>{{ formatPrice(order.orderTotalAfterDiscount ?? order.orderPrice ?? 0) }} {{ $t("currency") }}</span>
                    </div>
                    <div v-if="order.deliveryDriver" class="order-info-item">
                      <b-icon icon="truck" class="info-icon"></b-icon>
                      <span>{{ order.deliveryDriver.name }}</span>
                    </div>
                    <div v-if="order.notes" class="order-info-item">
                      <b-icon icon="chat-left-text" class="info-icon"></b-icon>
                      <span class="order-notes">{{ order.notes }}</span>
                    </div>
                  </div>
                  <div class="queue-card-footer">
                    <button 
                      class="queue-action-btn ready-btn" 
                      @click.stop="updateOrderStatus(order.id, 'Ready')"
                    >
                      <b-icon icon="check-circle" class="me-1"></b-icon>
                      {{ $t("markReady") || "تحديد كجاهز" }}
                    </button>
                    <button
                      v-if="canPrintOrder(order.orderStatus)"
                      type="button"
                      class="queue-action-btn print-order-btn"
                      :disabled="printingOrderId === order.id"
                      @click.stop="printOrder(order)"
                    >
                      <b-icon icon="printer-fill" class="me-1"></b-icon>
                      {{ $t('printOrder') || 'طباعة' }}
                    </button>
                    <button
                      v-if="!order.hiddenFromQueueDisplay"
                      type="button"
                      class="queue-action-btn hide-display-btn"
                      @click.stop="hideFromQueueDisplay(order.id)"
                    >
                      <b-icon icon="eye-slash" class="me-1"></b-icon>
                      {{ $t('removeFromQueueDisplay') || 'إزالة من شاشة الانتظار' }}
                    </button>
                  </div>
                </div>
                <div v-if="processingOrders.length === 0" class="queue-empty-state">
                  <b-icon icon="inbox" class="empty-icon"></b-icon>
                  <p class="empty-text">{{ $t("noProcessingOrders") || "لا توجد طلبات قيد المعالجة" }}</p>
                </div>
              </div>
            </div>

            <!-- Ready Column -->
            <div class="queue-column">
              <div class="queue-column-header ready">
                <div class="column-header-content">
                  <b-icon icon="check-circle" class="column-icon"></b-icon>
                  <h3 class="column-title">{{ $t("ready") || "جاهز" }}</h3>
                  <span class="column-count">{{ readyOrders.length }}</span>
                </div>
              </div>
              <div class="queue-column-body">
                <div 
                  class="queue-card" 
                  v-for="order in readyOrders" 
                  :key="order.id"
                  @click="selectOrder(order)"
                >
                  <div class="queue-card-header">
                    <div class="order-code-badge">{{ order.orderCode }}</div>
                    <div class="order-type-badge" :class="getOrderTypeClass(order.orderType)">
                      {{ getOrderTypeText(order.orderType) }}
                    </div>
                  </div>
                  <div v-if="order.hiddenFromQueueDisplay" class="queue-hidden-badge">
                    <b-icon icon="eye-slash" class="me-1"></b-icon>
                    {{ $t('hiddenFromQueueDisplay') || 'مخفي من الشاشة' }}
                  </div>
                  <div class="queue-card-body">
                    <div class="order-info-item">
                      <b-icon icon="hash" class="info-icon"></b-icon>
                      <span>{{ $t("orderNumber") || "رقم الطلب" }}: {{ order.dailySequenceNumber || order.id }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="box-seam" class="info-icon"></b-icon>
                      <span>{{ $t("itemsCount") || "عدد العناصر" }}: {{ order.itemsCount || 0 }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="currency-dollar" class="info-icon"></b-icon>
                      <span>{{ formatPrice(order.orderTotalAfterDiscount ?? order.orderPrice ?? 0) }} {{ $t("currency") }}</span>
                    </div>
                    <div v-if="order.deliveryDriver" class="order-info-item">
                      <b-icon icon="truck" class="info-icon"></b-icon>
                      <span>{{ order.deliveryDriver.name }}</span>
                    </div>
                    <div v-if="order.notes" class="order-info-item">
                      <b-icon icon="chat-left-text" class="info-icon"></b-icon>
                      <span class="order-notes">{{ order.notes }}</span>
                    </div>
                  </div>
                  <div class="queue-card-footer">
                    <button 
                      class="queue-action-btn completed-btn" 
                      @click.stop="updateOrderStatus(order.id, 'Completed')"
                    >
                      <b-icon icon="check2-circle" class="me-1"></b-icon>
                      {{ $t("markCompleted") || "تحديد كمكتمل" }}
                    </button>
                    <button
                      v-if="canPrintOrder(order.orderStatus)"
                      type="button"
                      class="queue-action-btn print-order-btn"
                      :disabled="printingOrderId === order.id"
                      @click.stop="printOrder(order)"
                    >
                      <b-icon icon="printer-fill" class="me-1"></b-icon>
                      {{ $t('printOrder') || 'طباعة' }}
                    </button>
                    <button
                      v-if="!order.hiddenFromQueueDisplay"
                      type="button"
                      class="queue-action-btn hide-display-btn"
                      @click.stop="hideFromQueueDisplay(order.id)"
                    >
                      <b-icon icon="eye-slash" class="me-1"></b-icon>
                      {{ $t('removeFromQueueDisplay') || 'إزالة من شاشة الانتظار' }}
                    </button>
                  </div>
                </div>
                <div v-if="readyOrders.length === 0" class="queue-empty-state">
                  <b-icon icon="inbox" class="empty-icon"></b-icon>
                  <p class="empty-text">{{ $t("noReadyOrders") || "لا توجد طلبات جاهزة" }}</p>
                </div>
              </div>
            </div>

            <!-- Completed Column -->
            <div class="queue-column">
              <div class="queue-column-header completed">
                <div class="column-header-content">
                  <b-icon icon="check2-circle" class="column-icon"></b-icon>
                  <h3 class="column-title">{{ $t("completed") || "مكتمل" }}</h3>
                  <span class="column-count">{{ completedOrders.length }}</span>
                </div>
              </div>
              <div class="queue-column-body">
                <div 
                  class="queue-card completed-card" 
                  v-for="order in completedOrders" 
                  :key="order.id"
                  @click="selectOrder(order)"
                >
                  <div class="queue-card-header">
                    <div class="order-code-badge">{{ order.orderCode }}</div>
                    <div class="order-type-badge" :class="getOrderTypeClass(order.orderType)">
                      {{ getOrderTypeText(order.orderType) }}
                    </div>
                  </div>
                  <div v-if="order.hiddenFromQueueDisplay" class="queue-hidden-badge">
                    <b-icon icon="eye-slash" class="me-1"></b-icon>
                    {{ $t('hiddenFromQueueDisplay') || 'مخفي من الشاشة' }}
                  </div>
                  <div class="queue-card-body">
                    <div class="order-info-item">
                      <b-icon icon="hash" class="info-icon"></b-icon>
                      <span>{{ $t("orderNumber") || "رقم الطلب" }}: {{ order.dailySequenceNumber || order.id }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="box-seam" class="info-icon"></b-icon>
                      <span>{{ $t("itemsCount") || "عدد العناصر" }}: {{ order.itemsCount || 0 }}</span>
                    </div>
                    <div class="order-info-item">
                      <b-icon icon="currency-dollar" class="info-icon"></b-icon>
                      <span>{{ formatPrice(order.orderTotalAfterDiscount ?? order.orderPrice ?? 0) }} {{ $t("currency") }}</span>
                    </div>
                    <div v-if="order.deliveryDriver" class="order-info-item">
                      <b-icon icon="truck" class="info-icon"></b-icon>
                      <span>{{ order.deliveryDriver.name }}</span>
                    </div>
                  </div>
                  <div class="queue-card-footer">
                    <button
                      v-if="canPrintOrder(order.orderStatus)"
                      type="button"
                      class="queue-action-btn print-order-btn"
                      :disabled="printingOrderId === order.id"
                      @click.stop="printOrder(order)"
                    >
                      <b-icon icon="printer-fill" class="me-1"></b-icon>
                      {{ $t('printOrder') || 'طباعة' }}
                    </button>
                    <button
                      v-if="!order.hiddenFromQueueDisplay"
                      type="button"
                      class="queue-action-btn hide-display-btn"
                      @click.stop="hideFromQueueDisplay(order.id)"
                    >
                      <b-icon icon="eye-slash" class="me-1"></b-icon>
                      {{ $t('removeFromQueueDisplay') || 'إزالة من شاشة الانتظار' }}
                    </button>
                  </div>
                </div>
                <div v-if="completedOrders.length === 0" class="queue-empty-state">
                  <b-icon icon="inbox" class="empty-icon"></b-icon>
                  <p class="empty-text">{{ $t("noCompletedOrders") || "لا توجد طلبات مكتملة" }}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Order Details Modal -->
    <b-modal
      v-model="showOrderModal"
      hide-header
      hide-footer
      size="lg"
      centered
      scrollable
      content-class="order-queue-modal-content"
      body-class="order-queue-modal-body"
      @hidden="selectedOrder = null"
    >
      <div v-if="selectedOrder" class="oq-detail">
        <header class="oq-detail-hero" :class="`oq-detail-hero--${(selectedOrder.orderStatus || 'Pending').toLowerCase()}`">
          <button type="button" class="oq-detail-close" @click="showOrderModal = false" :aria-label="$t('close') || 'إغلاق'">
            <b-icon icon="x-lg"></b-icon>
          </button>
          <div class="oq-detail-hero-main">
            <span class="oq-detail-hero-label">{{ $t("orderDetails") || "تفاصيل الطلب" }}</span>
            <div class="oq-detail-hero-title-row">
              <h2 class="oq-detail-hero-code">#{{ selectedOrder.dailySequenceNumber || selectedOrder.id }}</h2>
              <span class="oq-detail-hero-sep">·</span>
              <span class="oq-detail-hero-subcode">{{ selectedOrder.orderCode }}</span>
            </div>
            <p v-if="selectedOrder.insertDate" class="oq-detail-hero-time">
              <b-icon icon="clock" class="me-1"></b-icon>
              {{ formatDate(selectedOrder.insertDate) }}
            </p>
          </div>
          <div class="oq-detail-hero-badges">
            <span class="oq-detail-status-pill" :class="getStatusClass(selectedOrder.orderStatus)">
              {{ getStatusText(selectedOrder.orderStatus) }}
            </span>
            <span class="order-type-badge oq-detail-type-pill" :class="getOrderTypeClass(selectedOrder.orderType)">
              {{ getOrderTypeText(selectedOrder.orderType) }}
            </span>
          </div>
        </header>

        <div class="oq-detail-body">
          <section class="oq-detail-card">
            <h3 class="oq-detail-card-title">
              <b-icon icon="info-circle" class="me-2"></b-icon>
              {{ $t("orderInfo") || "معلومات الطلب" }}
            </h3>
            <div class="oq-detail-meta-grid">
              <div class="oq-detail-meta">
                <span class="oq-detail-meta-label">{{ $t("paymentMethod") || "طريقة الدفع" }}</span>
                <span class="oq-detail-meta-value">{{ getPaymentMethodText(selectedOrder.paymentMethod) }}</span>
              </div>
              <div class="oq-detail-meta">
                <span class="oq-detail-meta-label">{{ $t("paymentStatus") || "حالة الدفع" }}</span>
                <span class="oq-detail-meta-value oq-detail-meta-value--pill" :class="getPaymentStatusClass(selectedOrder.paymentStatus)">
                  {{ getPaymentStatusText(selectedOrder.paymentStatus) }}
                </span>
              </div>
              <div class="oq-detail-meta">
                <span class="oq-detail-meta-label">{{ $t("itemsCount") || "عدد العناصر" }}</span>
                <span class="oq-detail-meta-value">{{ selectedOrderItems.length }}</span>
              </div>
              <div class="oq-detail-meta oq-detail-meta--highlight">
                <span class="oq-detail-meta-label">{{ $t("total") || "المجموع" }}</span>
                <span class="oq-detail-meta-value oq-detail-meta-value--total">
                  {{ formatPrice(selectedOrderTotal) }} {{ $t("currency") }}
                </span>
              </div>
              <div v-if="Number(selectedOrder.discountAmount || 0) > 0" class="oq-detail-meta">
                <span class="oq-detail-meta-label">{{ $t("discountLabel") || "الخصم" }}</span>
                <span class="oq-detail-meta-value oq-detail-meta-value--discount">
                  - {{ formatPrice(selectedOrder.discountAmount || 0) }} {{ $t("currency") }}
                </span>
              </div>
            </div>
            <div v-if="selectedOrder.notes" class="oq-detail-notes">
              <b-icon icon="chat-left-text" class="oq-detail-notes-icon"></b-icon>
              <div>
                <span class="oq-detail-notes-label">{{ $t("notes") || "ملاحظات" }}</span>
                <p class="oq-detail-notes-text">{{ selectedOrder.notes }}</p>
              </div>
            </div>
          </section>

          <section v-if="selectedOrder.orderType === 'Delivery'" class="oq-detail-card oq-detail-card--delivery">
            <h3 class="oq-detail-card-title">
              <b-icon icon="truck" class="me-2"></b-icon>
              {{ $t("deliveryInfo") || "معلومات التوصيل" }}
            </h3>
            <div class="oq-detail-meta-grid">
              <div v-if="selectedOrder.deliveryCustomerName" class="oq-detail-meta">
                <span class="oq-detail-meta-label">{{ $t("customerName") || "اسم العميل" }}</span>
                <span class="oq-detail-meta-value">{{ selectedOrder.deliveryCustomerName }}</span>
              </div>
              <div v-if="selectedOrder.deliveryPhoneNumber" class="oq-detail-meta">
                <span class="oq-detail-meta-label">{{ $t("phoneNumber") || "رقم الهاتف" }}</span>
                <span class="oq-detail-meta-value">{{ selectedOrder.deliveryPhoneNumber }}</span>
              </div>
              <div v-if="selectedOrder.deliveryDriver" class="oq-detail-meta">
                <span class="oq-detail-meta-label">{{ $t("driverName") || "اسم السائق" }}</span>
                <span class="oq-detail-meta-value">{{ selectedOrder.deliveryDriver.name }}</span>
              </div>
              <div v-if="selectedOrder.deliveryAddress" class="oq-detail-meta oq-detail-meta--wide">
                <span class="oq-detail-meta-label">{{ $t("address") || "العنوان" }}</span>
                <span class="oq-detail-meta-value">{{ selectedOrder.deliveryAddress }}</span>
              </div>
            </div>
          </section>

          <section class="oq-detail-card">
            <h3 class="oq-detail-card-title">
              <b-icon icon="basket3" class="me-2"></b-icon>
              {{ $t("orderItems") || "عناصر الطلب" }}
            </h3>
            <div v-if="selectedOrderItems.length === 0" class="oq-detail-items-empty">
              {{ $t("noItems") || "لا توجد عناصر" }}
            </div>
            <div v-else class="oq-detail-items-table-wrap">
              <table class="oq-detail-items-table">
                <thead>
                  <tr>
                    <th>{{ $t("itemName") || "الصنف" }}</th>
                    <th class="text-center">{{ $t("quantity") || "الكمية" }}</th>
                    <th class="text-center">{{ $t("unitPrice") || "السعر" }}</th>
                    <th class="text-end">{{ $t("total") || "المجموع" }}</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="item in selectedOrderItems" :key="item.id">
                    <td class="oq-detail-item-name">{{ item.itemName }}</td>
                    <td class="text-center">
                      <span class="oq-detail-qty-badge">× {{ item.quantity }}</span>
                    </td>
                    <td class="text-center oq-detail-item-unit">{{ formatPrice(item.sellingPrice) }}</td>
                    <td class="text-end oq-detail-item-line-total">{{ formatPrice(lineItemTotal(item)) }} {{ $t("currency") }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div class="oq-detail-summary">
              <div v-if="Number(selectedOrder.orderSubTotal || 0) > 0 && Number(selectedOrder.discountAmount || 0) > 0" class="oq-detail-summary-row">
                <span>{{ $t("subtotal") || "المجموع الفرعي" }}</span>
                <span>{{ formatPrice(selectedOrder.orderSubTotal) }} {{ $t("currency") }}</span>
              </div>
              <div v-if="Number(selectedOrder.discountAmount || 0) > 0" class="oq-detail-summary-row oq-detail-summary-row--discount">
                <span>{{ $t("discountLabel") || "الخصم" }}</span>
                <span>- {{ formatPrice(selectedOrder.discountAmount || 0) }} {{ $t("currency") }}</span>
              </div>
              <div class="oq-detail-summary-row oq-detail-summary-row--grand">
                <span>{{ $t("total") || "المجموع" }}</span>
                <span>{{ formatPrice(selectedOrderTotal) }} {{ $t("currency") }}</span>
              </div>
            </div>
          </section>
        </div>

        <footer class="oq-detail-footer">
          <button
            v-if="selectedOrder.orderStatus === 'Pending'"
            type="button"
            class="oq-detail-action oq-detail-action--processing"
            @click="updateOrderStatus(selectedOrder.id, 'Processing')"
          >
            <b-icon icon="play-circle" class="me-2"></b-icon>
            {{ $t("startProcessing") || "بدء المعالجة" }}
          </button>
          <button
            v-if="selectedOrder.orderStatus === 'Processing'"
            type="button"
            class="oq-detail-action oq-detail-action--ready"
            @click="updateOrderStatus(selectedOrder.id, 'Ready')"
          >
            <b-icon icon="check-circle" class="me-2"></b-icon>
            {{ $t("markReady") || "تحديد كجاهز" }}
          </button>
          <button
            v-if="selectedOrder.orderStatus === 'Ready'"
            type="button"
            class="oq-detail-action oq-detail-action--done"
            @click="updateOrderStatus(selectedOrder.id, 'Completed')"
          >
            <b-icon icon="check2-circle" class="me-2"></b-icon>
            {{ $t("markCompleted") || "تحديد كمكتمل" }}
          </button>
          <button
            v-if="selectedOrder && canPrintOrder(selectedOrder.orderStatus)"
            type="button"
            class="oq-detail-action oq-detail-action--print"
            :disabled="printingOrderId === selectedOrder.id"
            @click="printOrder(selectedOrder)"
          >
            <b-icon icon="printer-fill" class="me-2"></b-icon>
            {{ $t('printOrder') || 'طباعة الطلب' }}
          </button>
          <button
            v-if="selectedOrder && !selectedOrder.hiddenFromQueueDisplay"
            type="button"
            class="oq-detail-action oq-detail-action--hide"
            @click="hideFromQueueDisplay(selectedOrder.id)"
          >
            <b-icon icon="eye-slash" class="me-2"></b-icon>
            {{ $t('removeFromQueueDisplay') || 'إزالة من شاشة الانتظار' }}
          </button>
          <button type="button" class="oq-detail-action oq-detail-action--ghost" @click="showOrderModal = false">
            {{ $t("close") || "إغلاق" }}
          </button>
        </footer>
      </div>
    </b-modal>
  </b-overlay>
</template>

<script>
import AppHeader from '../components/Layout/AppHeader.vue';
import { HTTP } from '../http/api.js';
import signalRService from '../services/signalr.js';
import {
  resolveCommercialUserIdFromStorage,
  buildTodayOrdersQueryParams,
  filterQueuePending,
  filterQueueProcessing,
  filterQueueReady,
  filterQueueCompleted,
  filterQueueForAdminBoard,
  filterQueueActive,
} from '../utils/queueOrders.js';
import {
  printPublicOrderLikePos,
  canPrintOrderStatus,
  shouldAutoPrintOnStatusChange,
  notifyPrintOrderResult,
  resolvePrintFailureMessage,
} from '../utils/orderPrintService.js';
import notify from '../utils/notify.js';

export default {
  name: 'OrderQueueView',
  components: {
    AppHeader
  },
  data() {
    return {
      Orders: [],
      orderTypeFilter: '',
      showOrderModal: false,
      selectedOrder: null,
      commercialUserId: null,
      refreshInterval: null,
      loading: false,
      printingOrderId: null,
    };
  },
  computed: {
    pendingOrders() {
      return filterQueuePending(this.Orders);
    },
    processingOrders() {
      return filterQueueProcessing(this.Orders);
    },
    readyOrders() {
      return filterQueueReady(this.Orders);
    },
    completedOrders() {
      return filterQueueCompleted(this.Orders).slice(0, 10);
    },
    activeOrdersCount() {
      return filterQueueActive(this.Orders).length;
    },
    selectedOrderItems() {
      return this.selectedOrder?.customerOrderItem || [];
    },
    selectedOrderTotal() {
      if (!this.selectedOrder) return 0;
      const after = Number(this.selectedOrder.orderTotalAfterDiscount);
      if (Number.isFinite(after) && after > 0) return after;
      const price = Number(this.selectedOrder.orderPrice);
      if (Number.isFinite(price) && price > 0) return price;
      return this.selectedOrderItems.reduce((sum, item) => sum + this.lineItemTotal(item), 0);
    },
  },
  mounted() {
    this.commercialUserId = resolveCommercialUserIdFromStorage();
    
    if (!this.commercialUserId) {
      this.$bvToast.toast('معرف المطعم غير موجود', {
        title: 'خطأ',
        variant: 'danger',
        solid: true
      });
      return;
    }

    this.loadOrders({ silent: false });
    this.initializeSignalR();
    
    // Auto refresh every 10 seconds
    this.refreshInterval = setInterval(() => {
      this.loadOrders({ silent: true });
    }, 10000);
  },
  beforeDestroy() {
    this.cleanupSignalR();
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  },
  methods: {
    async loadOrders(options = {}) {
      const silent = options.silent === true;
      if (!this.commercialUserId) return;
      if (!silent) this.loading = true;
      try {
        const extra = {};
        if (this.orderTypeFilter) {
          extra.orderType = this.orderTypeFilter;
        }
        const params = buildTodayOrdersQueryParams(extra);

        const response = await HTTP.get(`PublicMenu/${this.commercialUserId}/orders?${params.toString()}`);
        
        if (response.data && !response.data.errorStatus) {
          const allOrders = response.data.data.items || [];
          this.Orders = filterQueueForAdminBoard(allOrders);
        } else {
          this.$bvToast.toast(response.data?.message || 'حدث خطأ أثناء جلب الطلبات', {
            title: 'خطأ',
            variant: 'danger',
            solid: true
          });
        }
      } catch (error) {
        console.error('Error loading orders:', error);
        this.$bvToast.toast(error.response?.data?.message || 'حدث خطأ أثناء جلب الطلبات', {
          title: 'خطأ',
          variant: 'danger',
          solid: true
        });
      } finally {
        if (!silent) this.loading = false;
      }
    },
    async hideFromQueueDisplay(orderId) {
      try {
        const response = await HTTP.put(
          `PublicMenu/${this.commercialUserId}/orders/${orderId}/status`,
          { HiddenFromQueueDisplay: true }
        );

        if (response.data && !response.data.errorStatus) {
          const orderIndex = this.Orders.findIndex(o => o.id === orderId);
          if (orderIndex !== -1) {
            this.Orders[orderIndex].hiddenFromQueueDisplay = true;
          }

          if (this.selectedOrder && this.selectedOrder.id === orderId) {
            this.selectedOrder.hiddenFromQueueDisplay = true;
          }

          this.$bvToast.toast(
            this.$t('removedFromQueueDisplay') || 'تمت الإزالة من شاشة الانتظار',
            {
              title: this.$t('success') || 'نجاح',
              variant: 'success',
              solid: true,
            }
          );
        } else {
          this.$bvToast.toast(response.data?.message || 'حدث خطأ أثناء الإزالة من الشاشة', {
            title: this.$t('error') || 'خطأ',
            variant: 'danger',
            solid: true,
          });
        }
      } catch (error) {
        console.error('Error hiding order from queue display:', error);
        this.$bvToast.toast(error.response?.data?.message || 'حدث خطأ أثناء الإزالة من الشاشة', {
          title: this.$t('error') || 'خطأ',
          variant: 'danger',
          solid: true,
        });
      }
    },
    async updateOrderStatus(orderId, status) {
      const orderIndex = this.Orders.findIndex(o => o.id === orderId);
      const previousStatus =
        orderIndex !== -1
          ? this.Orders[orderIndex].orderStatus
          : this.selectedOrder?.id === orderId
            ? this.selectedOrder.orderStatus
            : null;

      try {
        const response = await HTTP.put(
          `PublicMenu/${this.commercialUserId}/orders/${orderId}/status`,
          { OrderStatus: status }
        );

        if (response.data && !response.data.errorStatus) {
          if (orderIndex !== -1) {
            this.Orders[orderIndex].orderStatus = status;
          }

          if (this.selectedOrder && this.selectedOrder.id === orderId) {
            this.selectedOrder.orderStatus = status;
          }

          this.$bvToast.toast('تم تحديث الحالة بنجاح', {
            title: 'نجاح',
            variant: 'success',
            solid: true
          });

          if (shouldAutoPrintOnStatusChange(previousStatus, status)) {
            const orderToPrint =
              this.selectedOrder?.id === orderId
                ? this.selectedOrder
                : this.Orders[orderIndex] || null;
            if (orderToPrint) {
              await this.printOrder(orderToPrint, { silent: false });
            }
          }
        } else {
          this.$bvToast.toast(response.data?.message || 'حدث خطأ أثناء تحديث الحالة', {
            title: 'خطأ',
            variant: 'danger',
            solid: true
          });
        }
      } catch (error) {
        console.error('Error updating order status:', error);
        this.$bvToast.toast(error.response?.data?.message || 'حدث خطأ أثناء تحديث الحالة', {
          title: 'خطأ',
          variant: 'danger',
          solid: true
        });
      }
    },
    selectOrder(order) {
      this.selectedOrder = order;
      this.showOrderModal = true;
    },
    canPrintOrder(status) {
      return canPrintOrderStatus(status);
    },
    async printOrder(order, options = {}) {
      if (!order || !this.commercialUserId) return;
      const silent = options.silent === true;
      this.printingOrderId = order.id;
      if (!silent) {
        notify.info(this.$t('printingOrder') || 'جاري الطباعة...', {
          timeout: 1500,
          maxToasts: 1,
        });
      }
      try {
        const result = await printPublicOrderLikePos(order, {
          http: HTTP,
          commercialUserId: this.commercialUserId,
          t: (key) => this.$t(key),
        });

        notifyPrintOrderResult(result, notify, (key) => this.$t(key), options);
      } catch (error) {
        console.error('Error printing order:', error);
        notify.error(
          error.response?.data?.message ||
            resolvePrintFailureMessage({ errors: ['unknown'] }, (key) => this.$t(key)),
          { timeout: 4500, maxToasts: 1 }
        );
      } finally {
        this.printingOrderId = null;
      }
    },
    getOrderTypeClass(type) {
      const classes = {
        'DineIn': 'dinein-badge',
        'Takeaway': 'takeaway-badge',
        'Delivery': 'delivery-badge'
      };
      return classes[type] || '';
    },
    getOrderTypeText(type) {
      const texts = {
        'DineIn': this.$t('dineIn') || 'داخل المطعم',
        'Takeaway': this.$t('takeaway') || 'خارجي',
        'Delivery': this.$t('delivery') || 'توصيل'
      };
      return texts[type] || type;
    },
    getStatusText(status) {
      const texts = {
        'Pending': this.$t('pending') || 'قيد الانتظار',
        'Processing': this.$t('processing') || 'قيد المعالجة',
        'Ready': this.$t('ready') || 'جاهز',
        'Completed': this.$t('completed') || 'مكتمل'
      };
      return texts[status] || status;
    },
    getStatusClass(status) {
      const classes = {
        'Pending': 'oq-detail-status-pill--pending',
        'Processing': 'oq-detail-status-pill--processing',
        'Ready': 'oq-detail-status-pill--ready',
        'Completed': 'oq-detail-status-pill--completed'
      };
      return classes[status] || 'oq-detail-status-pill--pending';
    },
    getPaymentMethodText(method) {
      const texts = {
        'Cash': this.$t('cash') || 'كاش',
        'Card': this.$t('card') || 'بطاقة',
        'Credit': this.$t('credit') || 'آجل'
      };
      return texts[method] || method;
    },
    getPaymentStatusText(status) {
      const texts = {
        'Pending': this.$t('pending') || 'قيد الانتظار',
        'Paid': this.$t('paid') || 'مدفوع',
        'Refunded': this.$t('refunded') || 'مسترد'
      };
      return texts[status] || status;
    },
    getPaymentStatusClass(status) {
      const classes = {
        'Pending': 'oq-detail-pay--pending',
        'Paid': 'oq-detail-pay--paid',
        'Refunded': 'oq-detail-pay--refunded'
      };
      return classes[status] || '';
    },
    formatPrice(price) {
      return new Intl.NumberFormat('ar-IQ').format(Number(price) || 0);
    },
    formatDate(dateString) {
      if (!dateString) return '';
      const date = new Date(dateString);
      return date.toLocaleString('ar-IQ', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
      });
    },
    lineItemTotal(item) {
      const total = Number(item?.total ?? item?.Total);
      if (Number.isFinite(total) && total >= 0) return total;
      return (Number(item?.sellingPrice) || 0) * (Number(item?.quantity) || 0);
    },
    initializeSignalR() {
      signalRService.startConnection().then(() => {
        signalRService.on('PublicOrderUpdated', (data) => {
          // Reload orders when an order is updated
          this.loadOrders({ silent: true });
        });
      });
    },
    cleanupSignalR() {
      signalRService.off('PublicOrderUpdated');
    }
  }
};
</script>

<style scoped>
.order-queue-filter-wrap {
  max-width: 100%;
  width: 100%;
}

.order-queue-filter-select {
  padding-inline-start: 2.5rem;
  min-width: 220px;
  max-width: 100%;
}

.order-queue-board {
  margin-top: 0;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 1.25rem;
}

.queue-column {
  background: var(--bg-primary);
  border-radius: 0.75rem;
  overflow: hidden;
  border: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  box-shadow: none;
}

.queue-column-header {
  padding: 0.875rem 1rem;
  background: var(--bg-secondary);
  color: var(--text-primary);
  font-weight: 700;
  border-bottom: 1px solid var(--border-color);
}

.queue-column-header.pending {
  border-bottom: 3px solid #f59e0b;
}

.queue-column-header.pending .column-icon {
  color: #d97706;
}

.queue-column-header.processing {
  border-bottom: 3px solid #3b82f6;
}

.queue-column-header.processing .column-icon {
  color: #2563eb;
}

.queue-column-header.ready {
  border-bottom: 3px solid #10b981;
}

.queue-column-header.ready .column-icon {
  color: #059669;
}

.queue-column-header.completed {
  border-bottom: 3px solid #64748b;
}

.queue-column-header.completed .column-icon {
  color: #64748b;
}

.column-header-content {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.column-icon {
  font-size: 1.35rem;
  flex-shrink: 0;
}

.column-title {
  margin: 0;
  font-size: 1rem;
  flex: 1;
  font-weight: 700;
  color: var(--text-primary);
}

.column-count {
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
  color: var(--text-primary);
  padding: 0.2rem 0.65rem;
  border-radius: 999px;
  font-size: 0.8125rem;
  font-weight: 700;
}

.queue-column-body {
  flex: 1;
  overflow-y: auto;
  max-height: min(62vh, 720px);
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.875rem;
  background: var(--bg-primary);
}

.queue-card {
  background: var(--bg-secondary);
  border-radius: 0.65rem;
  padding: 0.875rem 1rem;
  cursor: pointer;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
  border: 1px solid var(--border-color);
}

.queue-card:hover {
  border-color: rgba(129, 140, 248, 0.45);
  box-shadow: 0 4px 14px rgba(15, 23, 42, 0.06);
}

.queue-card.completed-card {
  opacity: 0.92;
}

.queue-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.65rem;
  gap: 0.5rem;
}

.order-code-badge {
  background: var(--bg-tertiary);
  color: var(--text-primary);
  padding: 0.35rem 0.65rem;
  border-radius: 0.5rem;
  font-weight: 800;
  font-size: 0.75rem;
  border: 1px solid var(--border-color);
}

.order-type-badge {
  padding: 0.2rem 0.55rem;
  border-radius: 0.375rem;
  font-size: 0.75rem;
  font-weight: 700;
}

.dinein-badge {
  background: rgba(99, 102, 241, 0.12);
  color: var(--primary-color);
  border: 1px solid rgba(99, 102, 241, 0.22);
}

.takeaway-badge {
  background: rgba(34, 197, 94, 0.1);
  color: var(--success-color);
  border: 1px solid rgba(34, 197, 94, 0.22);
}

.delivery-badge {
  background: rgba(249, 115, 22, 0.1);
  color: #ea580c;
  border: 1px solid rgba(249, 115, 22, 0.25);
}

.queue-card-body {
  margin-bottom: 0.65rem;
}

.order-info-item {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  margin-bottom: 0.4rem;
  font-size: 0.8125rem;
  color: var(--text-secondary);
}

.info-icon {
  color: var(--primary-color);
  font-size: 0.9rem;
  flex-shrink: 0;
  margin-top: 0.1rem;
}

.order-notes {
  font-style: italic;
  color: var(--text-secondary);
}

.queue-card-footer {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding-top: 0.25rem;
}

.queue-hidden-badge {
  display: inline-flex;
  align-items: center;
  align-self: flex-start;
  margin: 0 0 0.35rem;
  padding: 0.2rem 0.55rem;
  border-radius: 999px;
  background: rgba(100, 116, 139, 0.12);
  color: var(--text-secondary);
  font-size: 0.6875rem;
  font-weight: 700;
}

.queue-action-btn {
  flex: 1;
  padding: 0.5rem 0.65rem;
  border: none;
  border-radius: 0.65rem;
  color: #fff;
  font-size: 0.8125rem;
  font-weight: 700;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
  transition: filter 0.2s ease, box-shadow 0.2s ease;
  font-family: inherit;
}

.queue-action-btn:hover {
  filter: brightness(1.05);
}

.processing-btn {
  background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.25);
}

.ready-btn {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.22);
}

.completed-btn {
  background: linear-gradient(135deg, #64748b 0%, #475569 100%);
  box-shadow: 0 4px 12px rgba(71, 85, 105, 0.2);
}

.hide-display-btn {
  background: rgba(100, 116, 139, 0.12);
  color: var(--text-secondary);
  border: 1px solid rgba(100, 116, 139, 0.28);
  box-shadow: none;
}

.hide-display-btn:hover {
  background: rgba(100, 116, 139, 0.2);
  filter: none;
}

.print-order-btn {
  background: rgba(184, 134, 74, 0.12);
  color: var(--primary-color, #b8864a);
  border: 1px solid rgba(184, 134, 74, 0.35);
  box-shadow: none;
}

.print-order-btn:hover {
  background: rgba(184, 134, 74, 0.2);
  filter: none;
}

.oq-detail-action--print {
  background: rgba(184, 134, 74, 0.14);
  color: #966b35;
  border: 1px solid rgba(184, 134, 74, 0.35);
  box-shadow: none;
}

.oq-detail-action--print:hover {
  background: rgba(184, 134, 74, 0.22);
  filter: none;
}

.queue-empty-state {
  text-align: center;
  padding: 2.25rem 1rem;
  color: var(--text-secondary);
}

.empty-icon {
  font-size: 2.25rem;
  color: var(--text-secondary);
  opacity: 0.35;
  margin-bottom: 0.75rem;
}

.empty-text {
  margin: 0;
  font-size: 0.875rem;
  color: var(--text-secondary);
}


.oq-detail {
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.oq-detail-hero {
  position: relative;
  padding: 1.25rem 1.35rem 1.1rem;
  margin: -1rem -1rem 0;
  border-radius: 0.75rem 0.75rem 0 0;
  background: linear-gradient(135deg, var(--bg-secondary) 0%, var(--bg-tertiary) 100%);
  border-bottom: 1px solid var(--border-color);
}

.oq-detail-hero--pending {
  border-bottom: 3px solid #f59e0b;
}

.oq-detail-hero--processing {
  border-bottom: 3px solid #3b82f6;
}

.oq-detail-hero--ready {
  border-bottom: 3px solid #10b981;
}

.oq-detail-hero--completed {
  border-bottom: 3px solid #64748b;
}

.oq-detail-close {
  position: absolute;
  top: 0.85rem;
  inset-inline-end: 0.85rem;
  width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  background: var(--bg-primary);
  color: var(--text-secondary);
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease;
}

.oq-detail-close:hover {
  background: var(--bg-secondary);
  color: var(--text-primary);
}

.oq-detail-hero-main {
  padding-inline-start: 0.5rem;
  padding-inline-end: 2.5rem;
}

.oq-detail-hero-label {
  display: block;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--text-secondary);
  margin-bottom: 0.35rem;
}

.oq-detail-hero-title-row {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 0.35rem 0.5rem;
}

.oq-detail-hero-code {
  margin: 0;
  font-size: 1.65rem;
  font-weight: 800;
  color: var(--text-primary);
  line-height: 1.15;
}

.oq-detail-hero-sep {
  color: var(--text-secondary);
  font-weight: 600;
}

.oq-detail-hero-subcode {
  font-size: 0.9375rem;
  font-weight: 700;
  color: var(--text-secondary);
  font-family: ui-monospace, monospace;
}

.oq-detail-hero-time {
  margin: 0.5rem 0 0;
  font-size: 0.8125rem;
  color: var(--text-secondary);
  display: flex;
  align-items: center;
}

.oq-detail-hero-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: 0.85rem;
}

.oq-detail-status-pill {
  display: inline-flex;
  align-items: center;
  padding: 0.28rem 0.7rem;
  border-radius: 999px;
  font-size: 0.8125rem;
  font-weight: 700;
  border: 1px solid transparent;
}

.oq-detail-status-pill--pending {
  background: rgba(245, 158, 11, 0.14);
  color: #d97706;
  border-color: rgba(245, 158, 11, 0.35);
}

.oq-detail-status-pill--processing {
  background: rgba(59, 130, 246, 0.12);
  color: #2563eb;
  border-color: rgba(59, 130, 246, 0.3);
}

.oq-detail-status-pill--ready {
  background: rgba(16, 185, 129, 0.12);
  color: #059669;
  border-color: rgba(16, 185, 129, 0.3);
}

.oq-detail-status-pill--completed {
  background: rgba(100, 116, 139, 0.12);
  color: #64748b;
  border-color: rgba(100, 116, 139, 0.3);
}

.oq-detail-type-pill {
  font-size: 0.8125rem !important;
}

.oq-detail-body {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 1.15rem 0 0.5rem;
}

.oq-detail-card {
  padding: 1rem 1.1rem;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: 0.75rem;
}

.oq-detail-card--delivery {
  border-color: rgba(249, 115, 22, 0.25);
  background: linear-gradient(180deg, rgba(249, 115, 22, 0.04) 0%, var(--bg-secondary) 100%);
}

.oq-detail-card-title {
  display: flex;
  align-items: center;
  margin: 0 0 0.85rem;
  font-size: 0.9375rem;
  font-weight: 800;
  color: var(--text-primary);
}

.oq-detail-meta-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.75rem 1rem;
}

.oq-detail-meta {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  min-width: 0;
}

.oq-detail-meta--wide {
  grid-column: 1 / -1;
}

.oq-detail-meta--highlight {
  grid-column: 1 / -1;
  padding: 0.65rem 0.75rem;
  border-radius: 0.55rem;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
}

.oq-detail-meta-label {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-secondary);
}

.oq-detail-meta-value {
  font-size: 0.9375rem;
  font-weight: 700;
  color: var(--text-primary);
  word-break: break-word;
}

.oq-detail-meta-value--total {
  font-size: 1.125rem;
  color: var(--primary-color);
}

.oq-detail-meta-value--discount {
  color: var(--danger-color, #dc2626);
}

.oq-detail-meta-value--pill.oq-detail-pay--pending {
  color: #d97706;
}

.oq-detail-meta-value--pill.oq-detail-pay--paid {
  color: var(--success-color, #059669);
}

.oq-detail-meta-value--pill.oq-detail-pay--refunded {
  color: var(--danger-color, #dc2626);
}

.oq-detail-notes {
  display: flex;
  gap: 0.65rem;
  margin-top: 0.85rem;
  padding: 0.75rem 0.85rem;
  border-radius: 0.55rem;
  background: rgba(129, 140, 248, 0.08);
  border: 1px solid rgba(129, 140, 248, 0.2);
}

.oq-detail-notes-icon {
  flex-shrink: 0;
  color: var(--primary-color);
  font-size: 1.1rem;
  margin-top: 0.15rem;
}

.oq-detail-notes-label {
  display: block;
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--text-secondary);
  margin-bottom: 0.2rem;
}

.oq-detail-notes-text {
  margin: 0;
  font-size: 0.875rem;
  color: var(--text-primary);
  line-height: 1.45;
}

.oq-detail-items-empty {
  text-align: center;
  padding: 1.5rem;
  color: var(--text-secondary);
  font-size: 0.875rem;
}

.oq-detail-items-table-wrap {
  overflow-x: auto;
  border-radius: 0.55rem;
  border: 1px solid var(--border-color);
}

.oq-detail-items-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.8125rem;
}

.oq-detail-items-table thead {
  background: var(--bg-tertiary);
}

.oq-detail-items-table th {
  padding: 0.55rem 0.65rem;
  font-weight: 700;
  color: var(--text-secondary);
  text-align: start;
  border-bottom: 1px solid var(--border-color);
  white-space: nowrap;
}

.oq-detail-items-table td {
  padding: 0.6rem 0.65rem;
  border-bottom: 1px solid var(--border-color);
  color: var(--text-primary);
  vertical-align: middle;
}

.oq-detail-items-table tbody tr:last-child td {
  border-bottom: none;
}

.oq-detail-items-table tbody tr:hover {
  background: rgba(129, 140, 248, 0.04);
}

.oq-detail-item-name {
  font-weight: 700;
  max-width: 12rem;
}

.oq-detail-qty-badge {
  display: inline-block;
  min-width: 2rem;
  padding: 0.15rem 0.45rem;
  border-radius: 0.35rem;
  background: var(--bg-tertiary);
  font-weight: 700;
}

.oq-detail-item-unit {
  color: var(--text-secondary);
  font-weight: 600;
}

.oq-detail-item-line-total {
  font-weight: 800;
  color: var(--primary-color);
  white-space: nowrap;
}

.oq-detail-summary {
  margin-top: 0.85rem;
  padding-top: 0.75rem;
  border-top: 1px dashed var(--border-color);
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.oq-detail-summary-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.875rem;
  color: var(--text-secondary);
  font-weight: 600;
}

.oq-detail-summary-row--discount {
  color: var(--danger-color, #dc2626);
}

.oq-detail-summary-row--grand {
  margin-top: 0.25rem;
  padding-top: 0.5rem;
  border-top: 1px solid var(--border-color);
  font-size: 1rem;
  font-weight: 800;
  color: var(--text-primary);
}

.oq-detail-summary-row--grand span:last-child {
  color: var(--primary-color);
  font-size: 1.125rem;
}

.oq-detail-footer {
  display: flex;
  flex-wrap: wrap;
  gap: 0.55rem;
  justify-content: flex-end;
  margin: 0.75rem -1rem -1rem;
  padding: 0.85rem 1rem 1rem;
  border-top: 1px solid var(--border-color);
  background: var(--bg-secondary);
  border-radius: 0 0 0.75rem 0.75rem;
}

.oq-detail-action {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.6rem 1.1rem;
  border: none;
  border-radius: 0.6rem;
  font-size: 0.875rem;
  font-weight: 700;
  font-family: inherit;
  cursor: pointer;
  color: #fff;
  transition: filter 0.15s ease, box-shadow 0.15s ease;
}

.oq-detail-action:hover {
  filter: brightness(1.06);
}

.oq-detail-action--processing {
  background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.28);
}

.oq-detail-action--ready,
.oq-detail-action--done {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.28);
}

.oq-detail-action--hide {
  background: rgba(100, 116, 139, 0.12);
  color: var(--text-secondary);
  border: 1px solid rgba(100, 116, 139, 0.28);
  box-shadow: none;
}

.oq-detail-action--hide:hover {
  background: rgba(100, 116, 139, 0.2);
  filter: none;
}

.oq-detail-action--ghost {
  background: var(--bg-primary);
  color: var(--text-primary);
  border: 1px solid var(--border-color);
  box-shadow: none;
}

.oq-detail-action--ghost:hover {
  background: var(--bg-tertiary);
  filter: none;
}

.status-pending {
  color: #d97706;
}

.status-processing {
  color: #2563eb;
}

.status-ready {
  color: #059669;
}

.status-completed {
  color: #64748b;
}

.status-paid {
  color: var(--success-color);
}

.status-refunded {
  color: var(--danger-color);
}

@media (max-width: 768px) {
  .order-queue-board {
    grid-template-columns: 1fr;
  }

  .oq-detail-meta-grid {
    grid-template-columns: 1fr;
  }

  .oq-detail-footer {
    flex-direction: column;
    align-items: stretch;
  }

  .oq-detail-action {
    width: 100%;
  }

  .oq-detail-hero-code {
    font-size: 1.35rem;
  }
}
</style>

<style>
/* مربوط بنافذة b-modal (تُعرض خارج شجرة المكوّن) */
.order-queue-modal-content.modal-content {
  background: var(--bg-primary);
  color: var(--text-primary);
  border: 1px solid var(--border-color);
  border-radius: 0.75rem;
  overflow: hidden;
}

.order-queue-modal-body.modal-body {
  padding: 1rem;
}
</style>

