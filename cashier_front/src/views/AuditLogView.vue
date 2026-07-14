<template>
    <b-overlay :show="show" spinner-variant="primary" spinner-type="grow" spinner-large rounded="sm">
        <AppHeader />
        <div class="main-content-wrapper">
            <div class="app-page-container">
                <div class="app-page-content audit-log-page">
                    <div class="users-header-section">
                        <div class="users-header-content app-header-row">
                            <div class="header-title-wrapper">
                                <div class="header-icon-wrapper">
                                    <b-icon icon="journal-text" class="header-icon"></b-icon>
                                </div>
                                <div>
                                    <h1 class="users-page-title">{{ $t('auditLog') || 'سجل العمليات' }}</h1>
                                    <p class="header-subtitle">{{ $t('auditLogSubtitle') || 'تتبع عمليات التعديل والحذف في النظام' }}</p>
                                </div>
                            </div>
                            <div class="app-header-actions">
                                <button
                                    type="button"
                                    class="btn-refresh"
                                    @click="getAuditLogs"
                                    :disabled="show"
                                >
                                    <b-icon
                                        icon="arrow-clockwise"
                                        class="button-icon"
                                        :class="{ spinning: show }"
                                    ></b-icon>
                                    <span class="button-text">{{ $t('refresh') || 'تحديث' }}</span>
                                </button>
                            </div>
                        </div>
                    </div>

                    <div class="app-overview-grid">
                        <div class="app-overview-stat">
                            <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                                <b-icon icon="list-check"></b-icon>
                            </span>
                            <div>
                                <div class="app-overview-stat-value">
                                    <b-spinner small v-if="show"></b-spinner>
                                    <template v-else>{{ totalLogs }}</template>
                                </div>
                                <div class="app-overview-stat-label">{{ $t('totalCount') || 'العدد الإجمالي' }}</div>
                            </div>
                        </div>
                        <div class="app-overview-stat">
                            <span class="app-overview-stat-icon app-overview-stat-icon--info">
                                <b-icon icon="pencil-square"></b-icon>
                            </span>
                            <div>
                                <div class="app-overview-stat-value">{{ pageUpdateCount }}</div>
                                <div class="app-overview-stat-label">{{ $t('update') || 'تعديل' }}</div>
                            </div>
                        </div>
                        <div class="app-overview-stat">
                            <span class="app-overview-stat-icon app-overview-stat-icon--danger">
                                <b-icon icon="trash"></b-icon>
                            </span>
                            <div>
                                <div class="app-overview-stat-value">{{ pageDeleteCount }}</div>
                                <div class="app-overview-stat-label">{{ $t('delete') || 'حذف' }}</div>
                            </div>
                        </div>
                        <div class="app-overview-stat">
                            <span class="app-overview-stat-icon app-overview-stat-icon--success">
                                <b-icon icon="tags"></b-icon>
                            </span>
                            <div>
                                <div class="app-overview-stat-value">{{ entityTypes.length }}</div>
                                <div class="app-overview-stat-label">{{ $t('entityType') || 'نوع الكيان' }}</div>
                            </div>
                        </div>
                    </div>

                    <div class="app-filters-panel">
                        <div class="app-filters-panel-head">
                            <div class="app-filters-panel-title">
                                <span class="app-filters-panel-icon"><b-icon icon="funnel-fill"></b-icon></span>
                                <div>
                                    <h3>{{ $t('filters') || 'الفلاتر' }}</h3>
                                    <p>{{ $t('auditLogFiltersHint') || 'تصفية السجلات حسب العملية والنوع والتاريخ' }}</p>
                                </div>
                            </div>
                            <div class="app-filters-panel-actions" v-if="hasActiveFilters">
                                <button
                                    type="button"
                                    class="users-filter-clear-btn app-filters-clear-btn"
                                    @click="clearFilters"
                                >
                                    <b-icon icon="x-circle" class="me-1"></b-icon>
                                    {{ $t('clearFilters') || 'مسح الفلاتر' }}
                                </button>
                            </div>
                        </div>
                        <div class="app-filters-fields">
                            <label class="app-filter-field">
                                <span class="app-filter-label">{{ $t('action') || 'العملية' }}</span>
                                <div class="users-search-container">
                                    <b-icon icon="filter" class="search-icon"></b-icon>
                                    <select v-model="filters.action" class="users-search-input reports-filter-select">
                                        <option value="">{{ $t('allActions') || 'جميع العمليات' }}</option>
                                        <option value="Update">{{ $t('update') || 'تعديل' }}</option>
                                        <option value="Delete">{{ $t('delete') || 'حذف' }}</option>
                                    </select>
                                </div>
                            </label>
                            <label class="app-filter-field">
                                <span class="app-filter-label">{{ $t('entityType') || 'نوع الكيان' }}</span>
                                <div class="users-search-container">
                                    <b-icon icon="tags" class="search-icon"></b-icon>
                                    <select v-model="filters.entityType" class="users-search-input reports-filter-select">
                                        <option value="">{{ $t('allEntityTypes') || 'جميع الأنواع' }}</option>
                                        <option v-for="type in entityTypes" :key="type" :value="type">{{ type }}</option>
                                    </select>
                                </div>
                            </label>
                            <label class="app-filter-field">
                                <span class="app-filter-label">{{ $t('startDate') || 'من تاريخ' }}</span>
                                <div class="users-search-container">
                                    <b-icon icon="calendar" class="search-icon"></b-icon>
                                    <input v-model="filters.startDate" type="date" class="users-search-input" />
                                </div>
                            </label>
                            <label class="app-filter-field">
                                <span class="app-filter-label">{{ $t('endDate') || 'إلى تاريخ' }}</span>
                                <div class="users-search-container">
                                    <b-icon icon="calendar-check" class="search-icon"></b-icon>
                                    <input v-model="filters.endDate" type="date" class="users-search-input" />
                                </div>
                            </label>
                            <label class="app-filter-field app-filter-field--grow">
                                <span class="app-filter-label">{{ $t('search') || 'بحث' }}</span>
                                <div class="users-search-container">
                                    <b-icon icon="search" class="search-icon"></b-icon>
                                    <input
                                        v-model="filters.search"
                                        type="search"
                                        :placeholder="$t('searchByEntityName') || 'ابحث باسم الكيان...'"
                                        class="users-search-input"
                                        autocomplete="off"
                                    />
                                </div>
                            </label>
                        </div>
                    </div>

                    <div class="app-section-card">
                        <div class="app-section-header app-section-header--toolbar">
                            <div class="app-section-title-wrap">
                                <div class="app-section-icon-wrap">
                                    <b-icon icon="table"></b-icon>
                                </div>
                                <div>
                                    <h3 class="app-section-title">{{ $t('auditLog') || 'سجل العمليات' }}</h3>
                                </div>
                            </div>
                        </div>
                        <div class="app-section-body app-section-body--no-padding">
                    <div class="audit-log-table-container report-table-container">
                        <b-table
                            :items="filteredAuditLogs"
                            :fields="auditLogFields"
                            striped
                            hover
                            responsive
                            class="audit-log-table"
                            :empty-text="$t('noAuditLogs') || 'لا توجد سجلات عمليات'"
                            :empty-filtered-text="$t('noAuditLogs') || 'لا توجد سجلات عمليات'"
                        >
                            <template #cell(insertDate)="row">
                                <span class="audit-log-date">
                                    {{ formatDateTime(row.item.insertDate) }}
                                </span>
                            </template>

                            <template #cell(user)="row">
                                <div class="user-info-cell">
                                    <b-icon icon="person-circle" class="me-1"></b-icon>
                                    {{ row.item.user?.name || '---' }}
                                </div>
                            </template>

                            <template #cell(action)="row">
                                <span 
                                    class="action-badge" 
                                    :class="{
                                        'action-badge-update': row.item.action === 'Update',
                                        'action-badge-delete': row.item.action === 'Delete'
                                    }"
                                >
                                    {{ row.item.action === 'Update' ? ($t('update') || 'تعديل') : ($t('delete') || 'حذف') }}
                                </span>
                            </template>

                            <template #cell(entityType)="row">
                                <span class="audit-log-entity-type">
                                    {{ row.item.entityType }}
                                </span>
                            </template>

                            <template #cell(entityName)="row">
                                <span class="audit-log-entity-name">
                                    {{ row.item.entityName || '---' }}
                                </span>
                            </template>

                            <template #cell(description)="row">
                                <span class="audit-log-description">
                                    {{ row.item.description || '---' }}
                                </span>
                            </template>

                            <template #cell(details)="row">
                                <div v-if="row.item.oldValues || row.item.newValues" class="actions-cell" role="group">
                                    <button
                                        type="button"
                                        class="action-btn action-btn--icon action-btn--view"
                                        @click="showDetails(row.item)"
                                        :title="$t('viewDetails') || 'عرض التفاصيل'"
                                        :aria-label="$t('viewDetails') || 'عرض التفاصيل'"
                                    >
                                        <b-icon icon="eye" class="action-icon"></b-icon>
                                    </button>
                                </div>
                                <span v-else>---</span>
                            </template>
                        </b-table>
                    </div>

                    <div class="audit-log-pagination-section">
                        <b-pagination 
                            v-model="pageNumber" 
                            :total-rows="totalLogs" 
                            :per-page="pageSize"
                            aria-controls="audit-logs-table"
                            class="audit-log-pagination"
                        ></b-pagination>
                    </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Details Modal -->
        <b-modal id="modal-details" :title="$t('auditLogDetails') || 'تفاصيل العملية'" hide-header hide-footer class="audit-log-modal" size="lg">
            <div class="modal-content-wrapper">
                <div class="audit-log-details-content" v-if="selectedLog">
                    <div class="details-section">
                        <h3 class="details-section-title">{{ $t('basicInfo') || 'المعلومات الأساسية' }}</h3>
                        <div class="details-info-grid">
                            <div class="details-info-item">
                                <span class="details-info-label">{{ $t('dateTime') || 'التاريخ والوقت' }}:</span>
                                <span class="details-info-value">{{ formatDateTime(selectedLog.insertDate) }}</span>
                            </div>
                            <div class="details-info-item">
                                <span class="details-info-label">{{ $t('user') || 'المستخدم' }}:</span>
                                <span class="details-info-value">{{ selectedLog.user?.name || '---' }}</span>
                            </div>
                            <div class="details-info-item">
                                <span class="details-info-label">{{ $t('action') || 'العملية' }}:</span>
                                <span class="details-info-value">
                                    <span 
                                        class="action-badge" 
                                        :class="{
                                            'action-badge-update': selectedLog.action === 'Update',
                                            'action-badge-delete': selectedLog.action === 'Delete'
                                        }"
                                    >
                                        {{ selectedLog.action === 'Update' ? ($t('update') || 'تعديل') : ($t('delete') || 'حذف') }}
                                    </span>
                                </span>
                            </div>
                            <div class="details-info-item">
                                <span class="details-info-label">{{ $t('entityType') || 'نوع الكيان' }}:</span>
                                <span class="details-info-value">{{ selectedLog.entityType }}</span>
                            </div>
                            <div class="details-info-item">
                                <span class="details-info-label">{{ $t('entityName') || 'اسم الكيان' }}:</span>
                                <span class="details-info-value">{{ selectedLog.entityName || '---' }}</span>
                            </div>
                            <div class="details-info-item" v-if="selectedLog.description">
                                <span class="details-info-label">{{ $t('description') || 'الوصف' }}:</span>
                                <span class="details-info-value">{{ selectedLog.description }}</span>
                            </div>
                        </div>
                    </div>

                    <div class="details-section" v-if="selectedLog.action === 'Update' && (selectedLog.oldValues || selectedLog.newValues)">
                        <h3 class="details-section-title">{{ $t('changes') || 'التغييرات' }}</h3>
                        <div class="changes-comparison">
                            <div class="changes-column" v-if="selectedLog.oldValues">
                                <h4 class="changes-column-title">{{ $t('oldValues') || 'القيم القديمة' }}</h4>
                                <pre class="changes-content">{{ formatJson(selectedLog.oldValues) }}</pre>
                            </div>
                            <div class="changes-column" v-if="selectedLog.newValues">
                                <h4 class="changes-column-title">{{ $t('newValues') || 'القيم الجديدة' }}</h4>
                                <pre class="changes-content">{{ formatJson(selectedLog.newValues) }}</pre>
                            </div>
                        </div>
                    </div>

                    <div class="modal-actions">
                        <button class="modal-close-button" @click="closeDetailsModal">
                            <b-icon icon="x-circle-fill" class="me-2"></b-icon>
                            {{ $t('close') || 'إغلاق' }}
                        </button>
                    </div>
                </div>
            </div>
        </b-modal>
    </b-overlay>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from '../http/api.js';

export default {
    name: "AuditLogView",
    components: {
        AppHeader,
    },
    data() {
        return {
            show: false,
            auditLogs: [],
            entityTypes: [],
            pageNumber: 1,
            pageSize: 20,
            totalLogs: 0,
            filters: {
                action: '',
                entityType: '',
                startDate: '',
                endDate: '',
                search: ''
            },
            selectedLog: null,
        };
    },
    computed: {
        filteredAuditLogs() {
            let filtered = [...this.auditLogs];
            
            if (this.filters.search) {
                const searchLower = this.filters.search.toLowerCase();
                filtered = filtered.filter(log => 
                    (log.entityName && log.entityName.toLowerCase().includes(searchLower)) ||
                    (log.description && log.description.toLowerCase().includes(searchLower)) ||
                    (log.user?.name && log.user.name.toLowerCase().includes(searchLower))
                );
            }
            
            return filtered;
        },
        hasActiveFilters() {
            return this.filters.action || 
                   this.filters.entityType || 
                   this.filters.startDate || 
                   this.filters.endDate || 
                   this.filters.search;
        },
        pageUpdateCount() {
            return this.auditLogs.filter((log) => log.action === 'Update').length;
        },
        pageDeleteCount() {
            return this.auditLogs.filter((log) => log.action === 'Delete').length;
        },
        auditLogFields() {
            return [
                {
                    key: 'insertDate',
                    label: this.$t('dateTime') || 'التاريخ والوقت',
                    sortable: true
                },
                {
                    key: 'user',
                    label: this.$t('user') || 'المستخدم',
                    sortable: false
                },
                {
                    key: 'action',
                    label: this.$t('action') || 'العملية',
                    sortable: true
                },
                {
                    key: 'entityType',
                    label: this.$t('entityType') || 'نوع الكيان',
                    sortable: true
                },
                {
                    key: 'entityName',
                    label: this.$t('entityName') || 'اسم الكيان',
                    sortable: true
                },
                {
                    key: 'description',
                    label: this.$t('description') || 'الوصف',
                    sortable: false
                },
                {
                    key: 'details',
                    label: this.$t('details') || 'التفاصيل',
                    sortable: false
                }
            ];
        }
    },
    watch: {
        'filters.action'() {
            this.pageNumber = 1;
            this.getAuditLogs();
        },
        'filters.entityType'() {
            this.pageNumber = 1;
            this.getAuditLogs();
        },
        'filters.startDate'() {
            this.pageNumber = 1;
            this.getAuditLogs();
        },
        'filters.endDate'() {
            this.pageNumber = 1;
            this.getAuditLogs();
        },
        pageNumber() {
            this.getAuditLogs();
        }
    },
    mounted() {
        this.getAuditLogs();
        this.getEntityTypes();
    },
    methods: {
        async getAuditLogs() {
            this.show = true;
            try {
                const params = new URLSearchParams();
                params.append('pageNumber', (this.pageNumber - 1).toString());
                params.append('pageSize', this.pageSize.toString());
                
                if (this.filters.action) {
                    params.append('action', this.filters.action);
                }
                if (this.filters.entityType) {
                    params.append('entityType', this.filters.entityType);
                }
                if (this.filters.startDate) {
                    params.append('startDate', this.filters.startDate);
                }
                if (this.filters.endDate) {
                    params.append('endDate', this.filters.endDate);
                }

                const response = await HTTP.get(`AuditLog?${params.toString()}`);
                
                if (response.data && response.data.data) {
                    this.auditLogs = response.data.data.items || [];
                    this.totalLogs = response.data.data.totalItems || 0;
                }
            } catch (error) {
                console.error('Error loading audit logs:', error);
                this.$notify.error(this.$i18n.t("error") || "حدث خطأ أثناء جلب سجل العمليات", {
                    position: "top-right",
                    timeout: 3000,
                });
            } finally {
                this.show = false;
            }
        },
        async getEntityTypes() {
            try {
                const response = await HTTP.get('AuditLog/EntityTypes');
                if (response.data && response.data.data) {
                    this.entityTypes = response.data.data || [];
                }
            } catch (error) {
                console.error('Error loading entity types:', error);
            }
        },
        clearFilters() {
            this.filters = {
                action: '',
                entityType: '',
                startDate: '',
                endDate: '',
                search: ''
            };
            this.pageNumber = 1;
            this.getAuditLogs();
        },
        formatDateTime(dateString) {
            if (!dateString) return '---';
            const date = new Date(dateString);
            return date.toLocaleString('ar-EG', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
            });
        },
        formatJson(jsonString) {
            if (!jsonString) return '---';
            try {
                const obj = JSON.parse(jsonString);
                return JSON.stringify(obj, null, 2);
            } catch (e) {
                return jsonString;
            }
        },
        showDetails(log) {
            this.selectedLog = log;
            this.$bvModal.show('modal-details');
        },
        closeDetailsModal() {
            this.$bvModal.hide('modal-details');
            this.selectedLog = null;
        }
    }
};
</script>

<style scoped>
/* Using users-page-container, users-page-content, users-header-section, users-header-content, and users-page-title from main.css */

.audit-log-filters-section {
    background: var(--bg-primary);
    border-radius: 0.75rem;
    padding: 1.5rem;
    margin-bottom: 1.5rem;
    box-shadow: var(--shadow-sm);
}

.audit-log-filters-container {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 1rem;
    align-items: end;
}

.audit-log-filter-group {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.audit-log-filter-label {
    display: flex;
    align-items: center;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--text-primary);
}

.audit-log-filter-select,
.audit-log-filter-input {
    padding: 0.625rem 0.875rem;
    border: 2px solid var(--border-color);
    border-radius: 0.5rem;
    font-size: 0.9375rem;
    background: var(--bg-primary);
    color: var(--text-primary);
    transition: all 0.3s ease;
}

.audit-log-filter-select:focus,
.audit-log-filter-input:focus {
    border-color: var(--primary-color);
    box-shadow: 0 0 0 4px rgba(129, 140, 248, 0.1);
    outline: none;
}

.audit-log-filter-clear {
    padding: 0.625rem 1rem;
    border: 2px solid var(--danger-color);
    background: transparent;
    color: var(--danger-color);
    border-radius: 0.5rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.3s ease;
    display: flex;
    align-items: center;
    justify-content: center;
    height: fit-content;
}

.audit-log-filter-clear:hover {
    background: var(--danger-color);
    color: #ffffff;
}

.audit-log-table-container {
    margin-bottom: 1.5rem;
}

.audit-log-table >>> thead th .sr-only,
.audit-log-table >>> thead th .visually-hidden {
    display: none !important;
}

.audit-log-table {
    width: 100%;
    font-size: 0.9375rem;
}

.audit-log-date {
    white-space: nowrap;
    font-size: 0.875rem;
}

.audit-log-user {
    white-space: nowrap;
}

.user-info-cell {
    display: flex;
    align-items: center;
}

.audit-log-action {
    white-space: nowrap;
}

.action-badge {
    display: inline-block;
    padding: 0.375rem 0.75rem;
    border-radius: 0.375rem;
    font-size: 0.8125rem;
    font-weight: 600;
    text-transform: uppercase;
}

.action-badge-update {
    background: var(--warning-light);
    color: var(--warning-color);
}

.action-badge-delete {
    background: var(--danger-light);
    color: var(--danger-color);
}

.audit-log-entity-type {
    font-weight: 600;
    color: var(--text-primary);
}

.audit-log-entity-name {
    max-width: 200px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.audit-log-description {
    max-width: 300px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.audit-log-empty {
    text-align: center;
    padding: 3rem 1rem;
}

.empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1rem;
}

.empty-icon {
    font-size: 4rem;
    color: var(--text-secondary);
}

.empty-text {
    font-size: 1.125rem;
    color: var(--text-secondary);
    margin: 0;
}

.audit-log-pagination-section {
    display: flex;
    justify-content: center;
    padding: 1rem 0;
}

.audit-log-details-content {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.details-section {
    padding: 1rem;
    background: var(--bg-secondary);
    border-radius: 0.5rem;
}

.details-section-title {
    font-size: 1.25rem;
    font-weight: 700;
    color: var(--text-primary);
    margin: 0 0 1rem 0;
    padding-bottom: 0.75rem;
    border-bottom: 2px solid var(--border-color);
}

.details-info-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 1rem;
}

.details-info-item {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.details-info-label {
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--text-secondary);
}

.details-info-value {
    font-size: 1rem;
    color: var(--text-primary);
}

.changes-comparison {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    gap: 1rem;
}

.changes-column {
    padding: 1rem;
    background: var(--bg-primary);
    border-radius: 0.5rem;
    border: 1px solid var(--border-color);
}

.changes-column-title {
    font-size: 1rem;
    font-weight: 700;
    color: var(--text-primary);
    margin: 0 0 0.75rem 0;
    padding-bottom: 0.5rem;
    border-bottom: 1px solid var(--border-color);
}

.changes-content {
    font-size: 0.875rem;
    color: var(--text-primary);
    background: var(--bg-secondary);
    padding: 1rem;
    border-radius: 0.375rem;
    overflow-x: auto;
    margin: 0;
    font-family: 'Courier New', monospace;
    white-space: pre-wrap;
    word-wrap: break-word;
}

.modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 1rem;
    padding-top: 1rem;
    border-top: 2px solid var(--border-color);
}

.modal-close-button {
    padding: 0.75rem 1.5rem;
    border: 2px solid var(--border-color);
    background: var(--bg-secondary);
    color: var(--text-primary);
    border-radius: 0.5rem;
    font-size: 1rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.3s ease;
    display: flex;
    align-items: center;
}

.modal-close-button:hover {
    background: var(--bg-tertiary);
    border-color: var(--danger-color);
    color: var(--danger-color);
}

@media (max-width: 768px) {
    .audit-log-filters-container {
        grid-template-columns: 1fr;
    }
    
    .audit-log-table {
        font-size: 0.875rem;
    }
    
    .audit-log-table th,
    .audit-log-table td {
        padding: 0.75rem 0.5rem;
    }
    
    .changes-comparison {
        grid-template-columns: 1fr;
    }
}
</style>

