<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="users-page-container">
        <div class="users-page-content">
          <!-- Header Section -->
          <div class="users-header-section">
            <div class="users-header-content">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="printer-fill" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t('printTemplates') || 'نماذج الطباعة' }}</h1>
                  <p class="header-subtitle">{{ $t('printTemplatesDescription') || 'عرض جميع نماذج الطباعة المتاحة في النظام' }}</p>
                </div>
              </div>
            </div>
          </div>

          <!-- Print Templates Section -->
          <div class="print-templates-section">
            <div class="print-templates-grid">
              <!-- POS Print Template -->
              <div class="print-template-card">
                <div class="print-template-header">
                  <b-icon icon="cash-stack" class="template-icon"></b-icon>
                  <h3 class="template-title">{{ $t('posPrintTemplate') || 'نموذج طباعة POS' }}</h3>
                </div>
                <div class="print-template-preview" id="pos-print-preview">
                  <div class="bill-container">
                    <!-- Header Section -->
                    <div class="bill-header">
                      <img
                        src="../assets/logoarabic.png"
                        alt="logo"
                        class="bill-logo-img"
                      />
                      <h2 class="bill-store-name">نظام لايت كاشير</h2>
                      <p class="bill-store-subtitle">نظام إدارة المطاعم</p>
                    </div>

                    <!-- Invoice Info Section -->
                    <div class="bill-info-section">
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t("invoice_number") }}:</span>
                        <span class="bill-info-value">123456789</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t("from_date") }}:</span>
                        <span class="bill-info-value">2024-01-15 14:30</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t("orderType") }}:</span>
                        <span class="bill-info-value">{{ $t("dineIn") }}</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t("paymentMethod") }}:</span>
                        <span class="bill-info-value">{{ $t("cash") }}</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t("table") || "الطاولة" }}:</span>
                        <span class="bill-info-value">5</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t("employeeLabel") }}:</span>
                        <span class="bill-info-value">أحمد محمد</span>
                      </div>
                    </div>

                    <!-- Divider -->
                    <div class="bill-divider"></div>

                    <!-- Items Table -->
                    <div class="bill-items-section">
                      <table class="bill-items-table">
                        <thead>
                          <tr>
                            <th class="bill-item-name-col">{{ $t("item_name_label") }}</th>
                            <th class="bill-item-qty-col">{{ $t("quantity_label") }}</th>
                            <th class="bill-item-price-col">{{ $t("selling_price_label") }}</th>
                            <th class="bill-item-total-col">{{ $t("total_label") }}</th>
                          </tr>
                        </thead>
                        <tbody>
                          <tr>
                            <td class="bill-item-name">
                              شاورما دجاج
                              <span class="bill-discount-badge">خصم</span>
                            </td>
                            <td class="bill-item-qty">2</td>
                            <td class="bill-item-price">
                              <span class="bill-price-discounted">
                                <span class="bill-original-price">15,000</span>
                                <span class="bill-discount-price">12,000</span>
                              </span>
                            </td>
                            <td class="bill-item-total">24,000</td>
                          </tr>
                          <tr>
                            <td class="bill-item-name">بيتزا كبيرة</td>
                            <td class="bill-item-qty">1</td>
                            <td class="bill-item-price">25,000</td>
                            <td class="bill-item-total">25,000</td>
                          </tr>
                          <tr>
                            <td class="bill-item-name">كولا</td>
                            <td class="bill-item-qty">3</td>
                            <td class="bill-item-price">2,000</td>
                            <td class="bill-item-total">6,000</td>
                          </tr>
                        </tbody>
                      </table>
                    </div>

                    <!-- Divider -->
                    <div class="bill-divider"></div>

                    <!-- Summary Section -->
                    <div class="bill-summary-section">
                      <div class="bill-summary-row">
                        <span class="bill-summary-label">{{ $t("count") }}:</span>
                        <span class="bill-summary-value">6 {{ $t("items") }}</span>
                      </div>
                      <div class="bill-summary-row bill-summary-total">
                        <span class="bill-summary-label">{{ $t("total") }}:</span>
                        <span class="bill-summary-value">55,000 {{ $t("currency") }}</span>
                      </div>
                    </div>

                    <!-- Footer Section -->
                    <div class="bill-footer">
                      <p class="bill-footer-text">شكراً لزيارتكم</p>
                      <p class="bill-footer-text">Thank you for your visit</p>
                    </div>
                  </div>
                </div>
                <div class="print-template-actions">
                  <button class="template-print-btn" @click="printTemplate('pos')">
                    <b-icon icon="printer-fill" class="me-2"></b-icon>
                    {{ $t('print') }}
                  </button>
                </div>
              </div>

              <!-- Reports Print Template -->
              <div class="print-template-card">
                <div class="print-template-header">
                  <b-icon icon="file-earmark-bar-graph-fill" class="template-icon"></b-icon>
                  <h3 class="template-title">{{ $t('reportsPrintTemplate') || 'نموذج طباعة التقارير' }}</h3>
                </div>
                <div class="print-template-preview" id="reports-print-preview">
                  <div class="bill-container">
                    <!-- Header -->
                    <div class="bill-header">
                      <img src="../assets/logoarabic.png" class="bill-logo-img" />
                      <h2 class="bill-store-name">نظام لايت كاشير</h2>
                      <p class="bill-store-subtitle">نظام إدارة المطاعم</p>
                    </div>

                    <!-- Order Info -->
                    <div class="bill-info-section">
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t('invoice_number') }}:</span>
                        <span class="bill-info-value">987654321</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t('from_date') }}:</span>
                        <span class="bill-info-value">2024-01-15 16:45</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t('paymentMethod') }}:</span>
                        <span class="bill-info-value">{{ $t('card') }}</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t('orderType') }}:</span>
                        <span class="bill-info-value">{{ $t('dineIn') }}</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t('table') || 'الطاولة' }}:</span>
                        <span class="bill-info-value">3</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t('employeeLabel') }}:</span>
                        <span class="bill-info-value">سارة علي</span>
                      </div>
                    </div>

                    <!-- Divider -->
                    <div class="bill-divider"></div>

                    <!-- Items Table -->
                    <div class="bill-items-section">
                      <table class="bill-items-table">
                        <thead>
                          <tr>
                            <th class="bill-item-name-col">{{ $t('item_name_label') }}</th>
                            <th class="bill-item-qty-col">{{ $t('quantity_label') }}</th>
                            <th class="bill-item-price-col">{{ $t('selling_price_label') }}</th>
                            <th class="bill-item-total-col">{{ $t('total_label') }}</th>
                          </tr>
                        </thead>
                        <tbody>
                          <tr>
                            <td class="bill-item-name">برجر لحم</td>
                            <td class="bill-item-qty">2</td>
                            <td class="bill-item-price">18,000</td>
                            <td class="bill-item-total">36,000</td>
                          </tr>
                          <tr>
                            <td class="bill-item-name">بطاطس مقلية</td>
                            <td class="bill-item-qty">2</td>
                            <td class="bill-item-price">5,000</td>
                            <td class="bill-item-total">10,000</td>
                          </tr>
                        </tbody>
                      </table>
                    </div>

                    <!-- Divider -->
                    <div class="bill-divider"></div>

                    <!-- Summary -->
                    <div class="bill-summary-section">
                      <div class="bill-summary-row">
                        <span class="bill-summary-label">{{ $t('count') }}:</span>
                        <span class="bill-summary-value">4 {{ $t('items') }}</span>
                      </div>
                      <div class="bill-summary-row bill-summary-total">
                        <span class="bill-summary-label">{{ $t('total') }}:</span>
                        <span class="bill-summary-value">46,000 {{ $t('currency') }}</span>
                      </div>
                    </div>

                    <!-- Footer -->
                    <div class="bill-footer">
                      <p class="bill-footer-text">شكراً لزيارتكم</p>
                      <p class="bill-footer-text">Thank you for your visit</p>
                    </div>
                  </div>
                </div>
                <div class="print-template-actions">
                  <button class="template-print-btn" @click="printTemplate('reports')">
                    <b-icon icon="printer-fill" class="me-2"></b-icon>
                    {{ $t('print') }}
                  </button>
                </div>
              </div>

              <!-- Waiter Print Template -->
              <div class="print-template-card">
                <div class="print-template-header">
                  <b-icon icon="person-badge-fill" class="template-icon"></b-icon>
                  <h3 class="template-title">{{ $t('waiterPrintTemplate') || 'نموذج طباعة الويتر' }}</h3>
                </div>
                <div class="print-template-preview" id="waiter-print-preview">
                  <div class="bill-container">
                    <!-- Header Section -->
                    <div class="bill-header">
                      <img
                        src="../assets/logoarabic.png"
                        alt="logo"
                        class="bill-logo-img"
                      />
                      <h2 class="bill-store-name">نظام لايت كاشير</h2>
                      <p class="bill-store-subtitle">نظام إدارة المطاعم</p>
                    </div>

                    <!-- Order Info Section -->
                    <div class="bill-info-section">
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t("invoice_number") }}:</span>
                        <span class="bill-info-value">555666777</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t("from_date") }}:</span>
                        <span class="bill-info-value">2024-01-15 18:20</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t("orderType") }}:</span>
                        <span class="bill-info-value">{{ $t("dineIn") }}</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t("paymentMethod") }}:</span>
                        <span class="bill-info-value">{{ $t("cash") }}</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t("table") || "الطاولة" }}:</span>
                        <span class="bill-info-value">7</span>
                      </div>
                      <div class="bill-info-row">
                        <span class="bill-info-label">{{ $t("employeeLabel") }}:</span>
                        <span class="bill-info-value">خالد أحمد</span>
                      </div>
                    </div>

                    <!-- Divider -->
                    <div class="bill-divider"></div>

                    <!-- Items Table -->
                    <div class="bill-items-section">
                      <table class="bill-items-table">
                        <thead>
                          <tr>
                            <th class="bill-item-name-col">{{ $t("item_name_label") }}</th>
                            <th class="bill-item-qty-col">{{ $t("quantity_label") }}</th>
                            <th class="bill-item-price-col">{{ $t("selling_price_label") }}</th>
                            <th class="bill-item-total-col">{{ $t("total_label") }}</th>
                          </tr>
                        </thead>
                        <tbody>
                          <tr>
                            <td class="bill-item-name">سلطة خضار</td>
                            <td class="bill-item-qty">1</td>
                            <td class="bill-item-price">8,000</td>
                            <td class="bill-item-total">8,000</td>
                          </tr>
                          <tr>
                            <td class="bill-item-name">شاي</td>
                            <td class="bill-item-qty">2</td>
                            <td class="bill-item-price">1,500</td>
                            <td class="bill-item-total">3,000</td>
                          </tr>
                        </tbody>
                      </table>
                    </div>

                    <!-- Divider -->
                    <div class="bill-divider"></div>

                    <!-- Summary Section -->
                    <div class="bill-summary-section">
                      <div class="bill-summary-row">
                        <span class="bill-summary-label">{{ $t("count") }}:</span>
                        <span class="bill-summary-value">3 {{ $t("items") }}</span>
                      </div>
                      <div class="bill-summary-row bill-summary-total">
                        <span class="bill-summary-label">{{ $t("total") }}:</span>
                        <span class="bill-summary-value">11,000 {{ $t("currency") }}</span>
                      </div>
                    </div>

                    <!-- Footer Section -->
                    <div class="bill-footer">
                      <p class="bill-footer-text">شكراً لزيارتكم</p>
                      <p class="bill-footer-text">Thank you for your visit</p>
                    </div>
                  </div>
                </div>
                <div class="print-template-actions">
                  <button class="template-print-btn" @click="printTemplate('waiter')">
                    <b-icon icon="printer-fill" class="me-2"></b-icon>
                    {{ $t('print') }}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import AppHeader from '../components/Layout/AppHeader.vue';
import HTTP from '../http/api';

export default {
  name: 'PrintTemplatesView',
  components: {
    AppHeader
  },
  methods: {
    printTemplate(templateType) {
      const previewElement = document.getElementById(`${templateType}-print-preview`);
      if (!previewElement) {
        console.error('Preview element not found');
        return;
      }

      const prtHtml = previewElement.innerHTML;
      
      // Professional POS printer styles (80mm thermal printer) - Unified design
      const stylesHtml = `
        <style>
          @page {
            size: 80mm auto;
            margin: 0;
          }
          
          * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
          }
          
          body {
            font-family: 'Cairo', 'Arial', sans-serif;
            direction: rtl;
            font-size: 11px;
            line-height: 1.3;
            color: #000;
            background: #fff;
            padding: 5mm;
            width: 80mm;
          }
          
          .bill-container {
            width: 100%;
            max-width: 80mm;
            margin: 0 auto;
          }
          
          .bill-header {
            text-align: center;
            margin-bottom: 8px;
            padding-bottom: 8px;
            border-bottom: 1px dashed #000;
          }
          
          .bill-logo-img {
            max-width: 50px;
            height: auto;
            margin-bottom: 4px;
          }
          
          .bill-store-name {
            font-size: 16px;
            font-weight: 800;
            margin: 4px 0 2px 0;
            color: #000;
          }
          
          .bill-store-subtitle {
            font-size: 9px;
            color: #666;
            margin: 0;
          }
          
          .bill-info-section {
            margin: 8px 0;
            font-size: 10px;
          }
          
          .bill-info-row {
            display: flex;
            justify-content: space-between;
            margin-bottom: 3px;
          }
          
          .bill-info-label {
            font-weight: 600;
          }
          
          .bill-info-value {
            font-weight: 400;
          }
          
          .bill-divider {
            border-top: 1px dashed #000;
            margin: 8px 0;
          }
          
          .bill-items-section {
            margin: 8px 0;
          }
          
          .bill-items-table {
            width: 100%;
            border-collapse: collapse;
            font-size: 10px;
          }
          
          .bill-items-table thead {
            border-bottom: 1px solid #000;
          }
          
          .bill-items-table th {
            padding: 4px 2px;
            text-align: right;
            font-weight: 700;
            font-size: 9px;
          }
          
          .bill-item-name-col {
            width: 40%;
          }
          
          .bill-item-qty-col {
            width: 15%;
            text-align: center;
          }
          
          .bill-item-price-col {
            width: 20%;
            text-align: left;
          }
          
          .bill-item-total-col {
            width: 25%;
            text-align: left;
          }
          
          .bill-items-table td {
            padding: 3px 2px;
            vertical-align: top;
          }
          
          .bill-item-name {
            font-weight: 500;
            word-break: break-word;
          }
          
          .bill-discount-badge {
            display: block;
            font-size: 7px;
            color: #dc2626;
            font-weight: 600;
            margin-top: 2px;
          }
          
          .bill-item-qty {
            text-align: center;
            font-weight: 600;
          }
          
          .bill-item-price {
            text-align: left;
            font-size: 9px;
          }
          
          .bill-price-discounted {
            display: block;
          }
          
          .bill-original-price {
            display: block;
            text-decoration: line-through;
            color: #999;
            font-size: 8px;
          }
          
          .bill-discount-price {
            display: block;
            color: #dc2626;
            font-weight: 600;
          }
          
          .bill-item-total {
            text-align: left;
            font-weight: 700;
          }
          
          .bill-summary-section {
            margin: 8px 0;
            font-size: 11px;
          }
          
          .bill-summary-row {
            display: flex;
            justify-content: space-between;
            margin-bottom: 4px;
          }
          
          .bill-summary-label {
            font-weight: 600;
          }
          
          .bill-summary-value {
            font-weight: 400;
          }
          
          .bill-summary-total {
            border-top: 1px solid #000;
            padding-top: 4px;
            margin-top: 4px;
            font-size: 12px;
          }
          
          .bill-summary-total .bill-summary-label {
            font-weight: 700;
            font-size: 13px;
          }
          
          .bill-summary-total .bill-summary-value {
            font-weight: 800;
            font-size: 13px;
          }
          
          .bill-footer {
            text-align: center;
            margin-top: 12px;
            padding-top: 8px;
            border-top: 1px dashed #000;
          }
          
          .bill-footer-text {
            font-size: 9px;
            margin: 2px 0;
            color: #666;
          }
          
          @media print {
            body {
              padding: 0;
            }
            
            .bill-container {
              width: 80mm;
            }
          }
        </style>
      `;
      
      const WinPrint = window.open('', '', 'left=0,top=0,width=400,height=600,toolbar=0,scrollbars=0,status=0');
      WinPrint.document.write(`<!DOCTYPE html>
        <html>
        <head>
          <meta charset="UTF-8">
          ${stylesHtml}
        </head>
        <body>
          ${prtHtml}
        </body>
        </html>`);

      WinPrint.document.close();
      WinPrint.focus();
      
      // Wait a bit before printing to ensure content is loaded
      setTimeout(() => {
        WinPrint.print();
        setTimeout(() => {
          WinPrint.close();
        }, 100);
      }, 250);
    }
  }
};
</script>

<style scoped>
.print-templates-section {
  padding: 2rem;
}

.print-templates-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
  gap: 2rem;
  margin-top: 2rem;
}

.print-template-card {
  background: var(--card-bg, #fff);
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  transition: transform 0.2s, box-shadow 0.2s;
}

.print-template-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
}

.print-template-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1.5rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.template-icon {
  font-size: 2rem;
}

.template-title {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 600;
}

.print-template-preview {
  padding: 1.5rem;
  background: #f8f9fa;
  max-height: 600px;
  overflow-y: auto;
  border: 1px solid #e0e0e0;
}

.print-template-preview .bill-container {
  background: white;
  padding: 1rem;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  max-width: 80mm;
  margin: 0 auto;
}

.print-template-actions {
  padding: 1.5rem;
  background: #f8f9fa;
  border-top: 1px solid #e0e0e0;
  display: flex;
  justify-content: center;
}

.template-print-btn {
  display: flex;
  align-items: center;
  padding: 0.75rem 2rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.template-print-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.template-print-btn:active {
  transform: translateY(0);
}

/* Print Template Styles - Unified Design */
.bill-container {
  width: 100%;
  max-width: 80mm;
  margin: 0 auto;
  font-family: 'Cairo', 'Arial', sans-serif;
  direction: rtl;
}

.bill-header {
  text-align: center;
  margin-bottom: 8px;
  padding-bottom: 8px;
  border-bottom: 1px dashed #000;
}

.bill-logo-img {
  max-width: 50px;
  height: auto;
  margin-bottom: 4px;
}

.bill-store-name {
  font-size: 16px;
  font-weight: 800;
  margin: 4px 0 2px 0;
  color: #000;
}

.bill-store-subtitle {
  font-size: 9px;
  color: #666;
  margin: 0;
}

.bill-info-section {
  margin: 8px 0;
  font-size: 10px;
}

.bill-info-row {
  display: flex;
  justify-content: space-between;
  margin-bottom: 3px;
}

.bill-info-label {
  font-weight: 600;
}

.bill-info-value {
  font-weight: 400;
}

.bill-divider {
  border-top: 1px dashed #000;
  margin: 8px 0;
}

.bill-items-section {
  margin: 8px 0;
}

.bill-items-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 10px;
}

.bill-items-table thead {
  border-bottom: 1px solid #000;
}

.bill-items-table th {
  padding: 4px 2px;
  text-align: right;
  font-weight: 700;
  font-size: 9px;
}

.bill-item-name-col {
  width: 40%;
}

.bill-item-qty-col {
  width: 15%;
  text-align: center;
}

.bill-item-price-col {
  width: 20%;
  text-align: left;
}

.bill-item-total-col {
  width: 25%;
  text-align: left;
}

.bill-items-table td {
  padding: 3px 2px;
  vertical-align: top;
}

.bill-item-name {
  font-weight: 500;
  word-break: break-word;
}

.bill-discount-badge {
  display: block;
  font-size: 7px;
  color: #dc2626;
  font-weight: 600;
  margin-top: 2px;
}

.bill-item-qty {
  text-align: center;
  font-weight: 600;
}

.bill-item-price {
  text-align: left;
  font-size: 9px;
}

.bill-price-discounted {
  display: block;
}

.bill-original-price {
  display: block;
  text-decoration: line-through;
  color: #999;
  font-size: 8px;
}

.bill-discount-price {
  display: block;
  color: #dc2626;
  font-weight: 600;
}

.bill-item-total {
  text-align: left;
  font-weight: 700;
}

.bill-summary-section {
  margin: 8px 0;
  font-size: 11px;
}

.bill-summary-row {
  display: flex;
  justify-content: space-between;
  margin-bottom: 4px;
}

.bill-summary-label {
  font-weight: 600;
}

.bill-summary-value {
  font-weight: 400;
}

.bill-summary-total {
  border-top: 1px solid #000;
  padding-top: 4px;
  margin-top: 4px;
  font-size: 12px;
}

.bill-summary-total .bill-summary-label {
  font-weight: 700;
  font-size: 13px;
}

.bill-summary-total .bill-summary-value {
  font-weight: 800;
  font-size: 13px;
}

.bill-footer {
  text-align: center;
  margin-top: 12px;
  padding-top: 8px;
  border-top: 1px dashed #000;
}

.bill-footer-text {
  font-size: 9px;
  margin: 2px 0;
  color: #666;
}

@media (max-width: 768px) {
  .print-templates-grid {
    grid-template-columns: 1fr;
  }
  
  .print-template-preview {
    max-height: 500px;
  }
}
</style>

