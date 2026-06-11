<template>
  <b-overlay
    :show="show"
    spinner-variant="primary"
    spinner-type="grow"
    spinner-large
    rounded="sm"
  >
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="users-page-container">
        <div class="users-page-content">
          <!-- Header Section -->
          <div class="users-header-section">
            <div class="users-header-content">
              <h1 class="users-page-title">{{ $t("allItemsLabel") }}</h1>
              <button class="users-add-button" v-b-modal.modal-addItem>
                <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                <span class="button-text">{{ $t("addItemLabel") }}</span>
              </button>
            </div>
          </div>

          <!-- Search Section -->
          <div class="users-search-section">
            <div class="users-search-container">
              <b-icon icon="search" class="search-icon"></b-icon>
              <input 
                v-model="search.info" 
                type="search" 
                :placeholder="$t('searchPlaceholder')"
                class="users-search-input"
              />
            </div>
          </div>

          <!-- Items Table -->
          <div class="items-table-container report-table-container">
            <b-table
              :items="Items"
              :fields="itemFields"
              striped
              hover
              responsive
              class="items-table"
            >
              <template #cell(image)="row">
                <div class="item-image-cell">
                  <img 
                    v-if="row.item.image && !row.item.imageError" 
                    :src="row.item.image" 
                    :alt="row.item.name"
                    class="item-table-image"
                    @error="row.item.imageError = true"
                  />
                  <div v-else class="item-image-placeholder-small">
                    <b-icon icon="box-fill" class="item-placeholder-icon-small"></b-icon>
                  </div>
                </div>
              </template>

              <template #cell(name)="row">
                <span class="item-name-text">{{ row.item.name }}</span>
              </template>

              <template #cell(sellingPrice)="row">
                <span class="item-price-text">{{ formatPrice(row.item.sellingPrice) }} {{ $t("currency") }}</span>
              </template>

              <template #cell(tags)="row">
                <span class="item-tags-text">{{ row.item.tags }}</span>
              </template>

              <template #cell(actions)="row">
                <div class="actions-cell">
                  <button
                    type="button"
                    class="action-btn action-btn--icon action-btn--edit"
                    @click="getItemInfo(row.item)"
                    :title="$t('editButtonLabel')"
                  >
                    <b-icon icon="pencil-fill" class="action-icon"></b-icon>
                  </button>
                  <button
                    type="button"
                    class="action-btn action-btn--icon action-btn--print"
                    @click="printListOfCode(row.item, 30)"
                    :title="$t('printCodeButtonLabel')"
                  >
                    <b-icon icon="printer-fill" class="action-icon"></b-icon>
                  </button>
                  <button
                    type="button"
                    class="action-btn action-btn--icon action-btn--delete"
                    @click="deleteItemModel(row.item.id)"
                    :title="$t('deleteButtonLabel')"
                  >
                    <b-icon icon="trash-fill" class="action-icon"></b-icon>
                  </button>
                </div>
              </template>
            </b-table>

            <!-- Pagination -->
            <div class="pagination-container" v-if="totalPages > 1">
              <b-pagination
                v-model="pageNumber"
                :total-rows="totalItems"
                :per-page="pageSize"
                :limit="7"
                first-number
                last-number
                @change="onPageChange"
                class="items-pagination"
              ></b-pagination>
              <div class="pagination-info">
                <span>{{ $t('showing') || 'عرض' }} {{ ((pageNumber - 1) * pageSize) + 1 }} - {{ Math.min(pageNumber * pageSize, totalItems) }} {{ $t('of') || 'من' }} {{ totalItems }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Add Item Modal -->
      <b-modal id="modal-addItem" :title="$t('addItemModalTitle')" hide-header hide-footer class="users-modal" size="lg" scrollable>
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t("addItemModalTitle") }}</h2>
          <form @submit.prevent="addItem" class="users-form">
            <!-- Image Upload Section -->
            <div class="text-center mb-3" style="margin-bottom: 1rem;">
              <input type="file" ref="uploadPhoto" @change="uploadFile" hidden />
              <div @click="getFile" style="cursor: pointer; display: inline-block;">
                <img
                  v-if="!imagePreview"
                  @click="getFile"
                  src="../assets/upload.png"
                  alt="upload"
                  width="120"
                  style="cursor: pointer;"
                />
                <b-avatar v-if="imagePreview" :src="imagePreview" size="6rem"></b-avatar>
              </div>
            </div>

            <!-- Form Fields Grid -->
            <div class="modal-form-grid">
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="tag-fill" class="form-label-icon"></b-icon>
                  {{ $t("itemNamePlaceholder") }}
                </label>
                <input 
                  id="inputName"
                  v-model="addForm.name" 
                  type="text"
                  :placeholder="$t('itemNamePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="tags" class="form-label-icon"></b-icon>
                  {{ $t("categoryPlaceholder") }}
                </label>
                <select v-model="addForm.tags" class="users-form-select">
                  <option v-for="item in tags" :value="item.name">{{ item.name }}</option>
                </select>
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="currency-dollar" class="form-label-icon"></b-icon>
                  {{ $t("sellingPricePlaceholder") }}
                </label>
                <input 
                  id="inputSellingPrice"
                  v-model="addForm.sellingPrice" 
                  type="number"
                  :placeholder="$t('sellingPricePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="percent" class="form-label-icon"></b-icon>
                  {{ $t("disCountPricePlaceholder") }}
                </label>
                <input 
                  id="inputDisCountPrice"
                  v-model="addForm.disCountPrice" 
                  type="number"
                  :placeholder="$t('disCountPricePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="cart" class="form-label-icon"></b-icon>
                  {{ $t("purchasingPricePlaceholder") }}
                </label>
                <input 
                  id="inputPurchasingPrice"
                  v-model="addForm.purchasingPrice" 
                  type="number"
                  :placeholder="$t('purchasingPricePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="upc-scan" class="form-label-icon"></b-icon>
                  {{ $t("codePlaceholder") }}
                </label>
                <input 
                  id="inputCode"
                  v-model="addForm.code" 
                  type="text"
                  :placeholder="$t('codePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="box" class="form-label-icon"></b-icon>
                  {{ $t("quantityPlaceholder") || "الكمية" }}
                </label>
                <input 
                  id="inputQuantity"
                  v-model="addForm.quantity" 
                  type="number"
                  :placeholder="$t('quantityPlaceholder') || 'الكمية'" 
                  required 
                  min="0"
                  class="users-form-input"
                />
              </div>
            </div>

            <!-- Description Full Width -->
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="file-text" class="form-label-icon"></b-icon>
                {{ $t("descriptionPlaceholder") }}
              </label>
              <input 
                id="inputDescription"
                v-model="addForm.description" 
                type="text"
                :placeholder="$t('descriptionPlaceholder')" 
                class="users-form-input"
              />
            </div>

            <!-- Barcode Preview -->
            <div class="text-center mb-3" v-if="addForm.code.toString()" style="margin-top: 0.5rem;">
              <vue-barcode
                ref="BarImg"
                v-if="addForm.code.toString()"
                tag="img"
                :value="addForm.code.toString()"
                :options="{ displayValue: true, lineColor: '#2B2B2C', width: 2, height: 60 }"
                style="max-width: 200px;"
              />
            </div>

            <!-- Form Actions -->
            <div class="users-form-actions">
              <button type="submit" class="users-form-submit-button" :disabled="show == true">
                <b-spinner small v-if="show == true" class="me-2"></b-spinner>
                <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                {{ $t("addButton") }}
              </button>
              <button type="button" class="users-form-cancel-button" @click="closeModel('modal-addItem')">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("closeButton") }}
              </button>
            </div>
          </form>
        </div>
      </b-modal>

      <!-- Edit Item Modal -->
      <b-modal id="modal-editItem" :title="$t('editItemModalTitle')" hide-header hide-footer class="users-modal" size="lg" scrollable>
        <div class="modal-content-wrapper">
          <h2 class="modal-title">{{ $t("editItemModalTitle") }}</h2>
          <form @submit.prevent="EditItem" class="users-form">
            <!-- Image Upload Section -->
            <div class="text-center mb-3" style="margin-bottom: 1rem;">
              <input type="file" ref="uploadPhotoEdit" @change="uploadFileEdit" hidden />
              <div @click="getFileEdit" style="cursor: pointer; display: inline-block;">
                <img
                  v-if="!imagePreview && !itemImage"
                  @click="getFileEdit"
                  src="../assets/upload.png"
                  alt="upload"
                  width="120"
                  style="cursor: pointer;"
                />
                <b-avatar v-if="imagePreview || itemImage" :src="imagePreview || itemImage" size="6rem"></b-avatar>
              </div>
            </div>

            <!-- Form Fields Grid -->
            <div class="modal-form-grid">
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="tag-fill" class="form-label-icon"></b-icon>
                  {{ $t("itemNamePlaceholder") }}
                </label>
                <input 
                  id="editInputName"
                  v-model="editForm.name" 
                  type="text"
                  :placeholder="$t('itemNamePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="tags" class="form-label-icon"></b-icon>
                  {{ $t("categoryPlaceholder") }}
                </label>
                <select v-model="editForm.tags" class="users-form-select">
                  <option v-for="item in tags" :value="item.name">{{ item.name }}</option>
                </select>
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="currency-dollar" class="form-label-icon"></b-icon>
                  {{ $t("sellingPricePlaceholder") }}
                </label>
                <input 
                  id="editInputSellingPrice"
                  v-model="editForm.sellingPrice" 
                  type="number"
                  :placeholder="$t('sellingPricePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="percent" class="form-label-icon"></b-icon>
                  {{ $t("disCountPricePlaceholder") }}
                </label>
                <input 
                  id="editInputDisCountPrice"
                  v-model="editForm.disCountPrice" 
                  type="number"
                  :placeholder="$t('disCountPricePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="cart" class="form-label-icon"></b-icon>
                  {{ $t("purchasingPricePlaceholder") }}
                </label>
                <input 
                  id="editInputPurchasingPrice"
                  v-model="editForm.purchasingPrice" 
                  type="number"
                  :placeholder="$t('purchasingPricePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="upc-scan" class="form-label-icon"></b-icon>
                  {{ $t("codePlaceholder") }}
                </label>
                <input 
                  id="editInputCode"
                  v-model="editForm.code" 
                  type="text"
                  :placeholder="$t('codePlaceholder')" 
                  required 
                  class="users-form-input"
                />
              </div>
              <div class="users-form-group">
                <label class="users-form-label">
                  <b-icon icon="box" class="form-label-icon"></b-icon>
                  {{ $t("quantityPlaceholder") || "الكمية" }}
                </label>
                <input 
                  id="editInputQuantity"
                  v-model="editForm.quantity" 
                  type="number"
                  :placeholder="$t('quantityPlaceholder') || 'الكمية'" 
                  required 
                  min="0"
                  class="users-form-input"
                />
              </div>
            </div>

            <!-- Description Full Width -->
            <div class="users-form-group">
              <label class="users-form-label">
                <b-icon icon="file-text" class="form-label-icon"></b-icon>
                {{ $t("descriptionPlaceholder") }}
              </label>
              <input 
                id="editInputDescription"
                v-model="editForm.description" 
                type="text"
                :placeholder="$t('descriptionPlaceholder')" 
                class="users-form-input"
              />
            </div>

            <!-- Barcode Preview -->
            <div class="text-center mb-3" v-if="editForm.code && editForm.code.toString()" style="margin-top: 0.5rem;">
              <vue-barcode
                ref="BarImgEdit"
                v-if="editForm.code.toString()"
                tag="img"
                :value="editForm.code.toString()"
                :options="{ displayValue: true, lineColor: '#2B2B2C', width: 2, height: 60 }"
                style="max-width: 200px;"
              />
            </div>

            <!-- Form Actions -->
            <div class="users-form-actions">
              <button type="submit" class="users-form-submit-button" :disabled="show == true">
                <b-spinner small v-if="show == true" class="me-2"></b-spinner>
                <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                {{ $t("editItemButtonLabel") }}
              </button>
              <button type="button" class="users-form-cancel-button" @click="closeModel('modal-editItem')">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("closeButton") }}
              </button>
            </div>
          </form>
        </div>
      </b-modal>

      <!-- Delete Confirmation Modal -->
      <b-modal id="modal-delete" :title="$t('deleteConfirmationModalTitle')" hide-header hide-footer class="users-modal">
        <div class="modal-content-wrapper">
          <div class="delete-confirmation-content">
            <div class="delete-icon-wrapper">
              <b-icon icon="exclamation-triangle-fill" class="delete-warning-icon"></b-icon>
            </div>
            <h3 class="delete-confirmation-title">{{ $t("deleteConfirmationMessage") }}</h3>
            <p class="delete-confirmation-text">{{ $t("areYouSureDeleteUser") || 'هل أنت متأكد من حذف هذا المنتج؟' }}</p>
            <div class="delete-confirmation-actions">
              <button class="delete-confirm-button" @click="deleteItem('modal-delete')">
                <b-icon icon="check-circle-fill" class="me-2"></b-icon>
                {{ $t("deleteButtonLabel") }}
              </button>
              <button class="delete-cancel-button" @click="closeModel('modal-delete')">
                <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                {{ $t("cancelButtonLabel") }}
              </button>
            </div>
          </div>
        </div>
      </b-modal>

      <!-- Print Barcode (Hidden) -->
      <div id="printMe" class="text-align-center" style="display: none;">
        <b-row>
          <b-col
            class="text-align-center"
            sm="3"
            md="3"
            lg="3"
            v-for="item in barCodeList"
            :key="item.code"
          >
            <vue-barcode
              ref="BarImg"
              v-if="item.code.toString()"
              tag="img"
              :value="item.code.toString()"
              :options="{ displayValue: true, lineColor: '#2B2B2C' }"
            />
            <p class="item-name-center">{{ item.name }}</p>
          </b-col>
        </b-row>
      </div>
    </div>
  </b-overlay>
</template>
<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import ClockVue from "@/components/ClockVue.vue";
import VueBarcode from "@chenfengyuan/vue-barcode";

import { HTTP } from "../http/api.js";
export default {
  name: "ItemsView",
  components: {
    AppHeader,
    ClockVue,
    "vue-barcode": VueBarcode,
  },
  data() {
    return {
      selected: null,
      options: ["list", "of", "options"],
      show: false,
      search: "",
      Items: [],
      pageNumber: 1,
      totalItems: 0,
      pageSize: 12,
      search: {
        info: "",
      },
      SearchItems: [],
      totalCardItems: 0,
      userInfo: {},
      editForm: {
        name: "",
        description: "",
        sellingPrice: 0,
        purchasingPrice: 0,
        disCountPrice: 0,
        tags: "مواد اخرى",
        code: "",
        id: "",
        quantity: 0,
      },
      imagePreview: "",
      itemImage: "",
      showUpload: false,
      addForm: {
        name: "",
        description: "",
        sellingPrice: 0,
        purchasingPrice: 0,
        disCountPrice : 0,
        tags: "مواد اخرى",
        code: "",
        quantity: 0,
      },
      barCodeList: [],
      itemId: "",
      tags: [],
    };
  },

  watch: {
    search: {
      handler() {
        this.GetAllItems();
      },
      deep: true,
    },

    pageNumber() {
      this.GetAllItems();
    },
    
    // if disCountPrice 0 make it equal to sellingPrice
    "addForm.sellingPrice": {
      handler() {
          this.addForm.disCountPrice = this.addForm.sellingPrice;
      },
      deep: true,
    },

    
  },

  mounted() {
    this.getTags();
    this.GetAllItems();
    this.addForm.code = Math.floor(Math.random() * 1000000000).toString();
    this.userInfo = JSON.parse(localStorage.getItem("info"));
  },

  computed: {
    itemFields() {
      return [
        {
          key: 'image',
          label: '',
          sortable: false,
          thClass: 'item-header-cell',
          tdClass: 'item-image-column'
        },
        {
          key: 'name',
          label: this.$t('itemNamePlaceholder') || 'اسم المنتج',
          sortable: true,
          thClass: 'item-header-cell'
        },
        {
          key: 'sellingPrice',
          label: this.$t('itemPriceLabel') || 'السعر',
          sortable: true,
          thClass: 'item-header-cell'
        },
        {
          key: 'tags',
          label: this.$t('categoryPlaceholder') || 'القسم',
          sortable: true,
          thClass: 'item-header-cell'
        },
        {
          key: 'actions',
          label: this.$t('actions') || 'الإجراءات',
          sortable: false,
          thClass: 'item-header-cell'
        }
      ];
    },
    totalPages() {
      return Math.ceil(this.totalItems / this.pageSize);
    }
  },

  methods: {
    getTags() {
      HTTP.get(`Admin/GetTags?pageNumber=0&pageSize=10000`)
        .then((response) => {
          this.tags = response.data.data.items;
        })
        .catch((error) => {
          this.$notify.error(this.$i18n.t("error"), {
            position: "top-right",
            timeout: 4000,
          });
        });
    },

    getFile() {
      this.$refs.uploadPhoto.click();
    },

    getFileEdit() {
      this.$refs.uploadPhotoEdit.click();
    },

    uploadFile(event) {
      const selectedFile = event.target.files[0];
      this.itemPhoto = selectedFile;
      if (selectedFile) {
        this.imagePreview = URL.createObjectURL(selectedFile);
        this.showUpload = false;
      }
    },

    uploadFileEdit(event) {
      const selectedFile = event.target.files[0];
      this.itemPhoto = selectedFile;
      if (selectedFile) {
        this.imagePreview = URL.createObjectURL(selectedFile);
        this.showUpload = false;
      }
    },

    printListOfCode(code, count) {
      this.barCodeList = [];
      for (let index = 0; index < count; index++) {
        this.barCodeList.push({ code: code.code, name: code.name });
      }
      this.$nextTick(() => {
        this.print();
      });
    },
    print() {
      const printContents = document.getElementById("printMe").innerHTML;
      const printWindow = window.open("", "_blank");
      const originalHead = document.head.innerHTML;

      // Create the content for the new window
      const newContent = `
    <html>
      <head>
        ${originalHead}
      </head>
      <body dir="rtl">
        ${printContents}
      </body>
    </html>
  `;

      printWindow.document.open();
      printWindow.document.write(newContent);
      printWindow.document.close();

      // Wait for the window to load its content before printing
      printWindow.onload = () => {
        printWindow.print();
        printWindow.close();
      };
    },

    deleteItemModel(id) {
      this.itemId = id;
      this.$bvModal.show("modal-delete");
    },
    getItemInfo(item) {
      this.itemPhoto = null;
      this.itemImage = item.image || "";
      this.imagePreview = "";
      this.editForm = {
        id: item.id,
        name: item.name || "",
        description: item.description || "",
        sellingPrice: item.sellingPrice || 0,
        purchasingPrice: item.purchasingPrice || 0,
        disCountPrice: item.disCountPrice || 0,
        tags: item.tags || "مواد اخرى",
        code: item.code || "",
        quantity: item.quantity || 0,
      };
      this.$bvModal.show("modal-editItem");
    },
    addItem() {
      this.show = true;
      var formData = new FormData();
      formData.append("Name", this.addForm.name);
      formData.append("Description", this.addForm.description);
      formData.append("SellingPrice", this.addForm.sellingPrice);
      formData.append("PurchasingPrice", this.addForm.purchasingPrice);
      formData.append("Tags", this.addForm.tags);
      formData.append("Code", this.addForm.code);
      formData.append("Image", this.itemPhoto);
      formData.append("DisCountPrice", this.addForm.disCountPrice);
      formData.append("Quantity", this.addForm.quantity);

      HTTP.post(`Admin/AddItem`, formData)
        .then((response) => {
          this.$notify.success(this.$i18n.t("addItemToOrderSucsses"), {
            position: "top-right",
            timeout: 4000,
            closeOnClick: true,
            pauseOnFocusLoss: true,
            pauseOnHover: true,
            draggable: true,
            draggablePercent: 0.6,
            showCloseButtonOnHover: false,
            hideProgressBar: true,
            closeButton: "button",
            icon: true,
          });
          this.addForm.name = "";
          this.addForm.description = "";
          this.addForm.sellingPrice = 0;
          this.addForm.purchasingPrice = 0;
          this.addForm.code = Math.floor(
            Math.random() * 1000000000000
          ).toString();
          this.addForm.disCountPrice = 0;
          this.addForm.quantity = 0;
          this.imagePreview = "";
          this.itemPhoto = null;
          this.GetAllItems();
          this.$bvModal.hide("modal-addItem");
        })
        .catch((error) => {
          this.show = false;
          this.$notify.error(this.$i18n.t("error"), {
            position: "top-right",
            timeout: 4000,
            closeOnClick: true,
            pauseOnFocusLoss: true,
            pauseOnHover: true,
            draggable: true,
            draggablePercent: 0.6,
            showCloseButtonOnHover: false,
            hideProgressBar: true,
            closeButton: "button",
            icon: true,
          });
        });
    },

    EditItem() {
      var formData = new FormData();
      formData.append("Name", this.editForm.name);
      formData.append("Description", this.editForm.description);
      formData.append("SellingPrice", this.editForm.sellingPrice);
      formData.append("PurchasingPrice", this.editForm.purchasingPrice);
      formData.append("Tags", this.editForm.tags);
      formData.append("Code", this.editForm.code);
      formData.append("Image", this.itemPhoto);
      formData.append("DisCountPrice", this.editForm.disCountPrice);
      formData.append("Quantity", this.editForm.quantity);

      this.show = true;
      HTTP.put(`Admin/UpdateItem?id=${this.editForm.id}`, formData)
        .then((response) => {
          this.show = false;
          this.$notify.success(this.$i18n.t("itemHadbeenEditSuccessfully"), {
            position: "top-right",
            timeout: 4000,
            closeOnClick: true,
            pauseOnFocusLoss: true,
            pauseOnHover: true,
            draggable: true,
            draggablePercent: 0.6,
            showCloseButtonOnHover: false,
            hideProgressBar: true,
            closeButton: "button",
            icon: true,
          });
          this.GetAllItems();
          this.$bvModal.hide("modal-editItem");
          this.imagePreview = "";
          this.itemImage = "";
          this.itemPhoto = null;
        })
        .catch((error) => {
          this.show = false;
          this.$notify.error(this.$i18n.t("somethingWrong"), {
            position: "top-right",
            timeout: 4000,
            closeOnClick: true,
            pauseOnFocusLoss: true,
            pauseOnHover: true,
            draggable: true,
            draggablePercent: 0.6,
            showCloseButtonOnHover: false,
            hideProgressBar: true,
            closeButton: "button",
            icon: true,
          });
        });
    },

    deleteItem(modelId) {
      this.show = true;
      HTTP.delete(`Admin/DeleteItem?id=${this.itemId}`)
        .then((response) => {
          this.show = false;
          this.$notify.success(this.$i18n.t("somethingWrong"), {
            position: "top-right",
            timeout: 4000,
            closeOnClick: true,
            pauseOnFocusLoss: true,
            pauseOnHover: true,
            draggable: true,
            draggablePercent: 0.6,
            showCloseButtonOnHover: false,
            hideProgressBar: true,
            closeButton: "button",
            icon: true,
          });
          this.GetAllItems();
          this.$bvModal.hide(modelId);
        })
        .catch((error) => {
          this.show = false;
          this.$notify.error(this.$i18n.t("somethingWrong"), {
            position: "top-right",
            timeout: 4000,
            closeOnClick: true,
            pauseOnFocusLoss: true,
            pauseOnHover: true,
            draggable: true,
            draggablePercent: 0.6,
            showCloseButtonOnHover: false,
            hideProgressBar: true,
            closeButton: "button",
            icon: true,
          });
        });
    },

    formatPrice(price) {
      if (price) {
        return price.toLocaleString("en-EG"); // Use the "ar-EG" locale for Arabic formatting
      }
      return "";
    },
    closeModel(id) {
      this.$bvModal.hide(id);
      if (id === 'modal-editItem') {
        this.imagePreview = "";
        this.itemImage = "";
        this.itemPhoto = null;
      }
    },

    GetAllItems() {
      this.show = true;
      HTTP.get(
        `Admin/GetItems?pageNumber=${this.pageNumber - 1}&pageSize=${
          this.pageSize
        }&info=${this.search.info}`
      )
        .then((response) => {
          this.Items = response.data.data.items.map(item => ({
            ...item,
            imageError: false
          }));
          this.totalItems = response.data.data.totalItems;
          this.show = false;
        })
        .catch((error) => {
          this.show = false;
        });
    },
    onPageChange(page) {
      this.pageNumber = page;
      this.GetAllItems();
    },
  },
};
</script>

<style scoped>
.items-table-container {
  margin-top: 1.5rem;
}

.items-table {
  margin: 0;
}

.items-table >>> thead th .sr-only,
.items-table >>> thead th .visually-hidden {
  display: none !important;
}

.item-image-column {
  width: 80px;
}

.item-image-cell {
  display: flex;
  align-items: center;
  justify-content: center;
}

.item-table-image {
  width: 60px;
  height: 60px;
  object-fit: cover;
  border-radius: 0.5rem;
}

.item-image-placeholder-small {
  width: 60px;
  height: 60px;
  background-color: #f3f4f6;
  border-radius: 0.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #9ca3af;
}

.item-placeholder-icon-small {
  font-size: 1.5rem;
}

.item-name-text {
  font-weight: 600;
  font-size: 0.9375rem;
  color: #111827;
}

.item-price-text {
  font-weight: 600;
  font-size: 0.9375rem;
  color: var(--primary-color);
}

.item-tags-text {
  color: var(--text-muted);
  font-size: 0.875rem;
}

.pagination-container {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  background-color: var(--bg-primary);
  border-top: 1px solid var(--border-color);
}

.pagination-info {
  color: var(--text-muted);
  font-size: 0.875rem;
}

.items-pagination >>> .page-link {
  color: var(--text-primary);
  border-color: var(--border-color);
  background-color: var(--bg-tertiary);
}

.items-pagination >>> .page-item.active .page-link {
  background-color: var(--primary-color);
  border-color: var(--primary-color);
  color: #ffffff;
}

.items-pagination >>> .page-link:hover {
  background-color: rgba(99, 102, 241, 0.1);
  border-color: var(--border-dark);
  color: var(--primary-color);
}
</style>
