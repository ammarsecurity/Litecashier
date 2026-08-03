<template>
  <div>
    <AppHeader />
    <div class="main-content-wrapper">
      <div class="app-page-container">
        <div class="app-page-content payroll-page">
          <div class="users-header-section">
            <div class="users-header-content app-header-row">
              <div class="header-title-wrapper">
                <div class="header-icon-wrapper">
                  <b-icon icon="cash-stack" class="header-icon"></b-icon>
                </div>
                <div>
                  <h1 class="users-page-title">{{ $t("payrollAndAdvances") }}</h1>
                  <p class="header-subtitle">{{ $t("payrollAndAdvancesHint") }}</p>
                </div>
              </div>
              <div class="app-header-actions app-equal-btn-group">
                <button type="button" class="btn-refresh" @click="refreshAll" :disabled="loading">
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loading }"></b-icon>
                  <span class="button-text">{{ $t("refresh") }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="app-overview-grid">
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                <b-icon icon="cash-coin"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ money(balances.totalOpenAdvances) }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("openAdvancesTotal") }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                <b-icon icon="people-fill"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ (balances.employees || []).length }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("employees") }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--info">
                <b-icon icon="calendar-month"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ runs.length }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("payrollRuns") }}</div>
              </div>
            </div>
            <div class="app-overview-stat">
              <span class="app-overview-stat-icon app-overview-stat-icon--success">
                <b-icon icon="check2-circle"></b-icon>
              </span>
              <div>
                <div class="app-overview-stat-value">
                  <b-spinner small v-if="loading"></b-spinner>
                  <template v-else>{{ activeEmployeesCount }}</template>
                </div>
                <div class="app-overview-stat-label">{{ $t("payrollActiveEmployees") }}</div>
              </div>
            </div>
          </div>

          <div class="app-section-card payroll-tabs-card">
            <div class="payroll-tabs" role="tablist">
              <button
                v-for="tab in tabs"
                :key="tab.id"
                type="button"
                class="payroll-tab"
                :class="{ active: activeTab === tab.id }"
                @click="activeTab = tab.id"
              >
                <b-icon :icon="tab.icon" class="payroll-tab-icon"></b-icon>
                <span>{{ tab.label }}</span>
              </button>
            </div>
          </div>

          <!-- Overview -->
          <div v-if="activeTab === 'overview'" class="app-section-card">
            <div class="app-section-header">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="list-ul"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("payrollBalancesTitle") }}</h3>
                  <p class="app-section-subtitle">{{ $t("payrollBalancesHint") }}</p>
                </div>
              </div>
            </div>
            <div class="app-section-body app-section-body--no-padding">
              <div v-if="loading" class="loading-state-full">
                <b-spinner variant="primary"></b-spinner>
                <span>{{ $t("loading") }}</span>
              </div>
              <div v-else class="report-table-container">
                <table class="report-table reports-table">
                  <thead>
                    <tr>
                      <th>{{ $t("payrollEmployee") }}</th>
                      <th>{{ $t("jobTitle") }}</th>
                      <th>{{ $t("salary") }}</th>
                      <th>{{ $t("openAdvanceBalance") }}</th>
                      <th>{{ $t("status") }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-if="!(balances.employees || []).length">
                      <td colspan="5" class="payroll-empty-cell">{{ $t("noEmployees") }}</td>
                    </tr>
                    <tr v-for="row in balances.employees || []" :key="row.employeeId">
                      <td>{{ row.employeeName }}</td>
                      <td>{{ row.jobTitle || "—" }}</td>
                      <td>{{ money(row.salary) }} ({{ salaryTypeLabel(row.salaryType) }})</td>
                      <td>
                        <span class="payroll-amount">{{ money(row.openAdvanceBalance) }}</span>
                      </td>
                      <td>
                        <span class="payroll-badge" :class="row.isActive ? 'is-active' : 'is-inactive'">
                          {{ row.isActive ? $t("active") : $t("inactive") }}
                        </span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <!-- Advances -->
          <div v-if="activeTab === 'advances'" class="app-section-card">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="cash"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("advances") }}</h3>
                  <p class="app-section-subtitle">{{ $t("payrollAdvancesHint") }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button type="button" class="users-add-button" @click="showAdvanceModal = true">
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addAdvance") }}</span>
                </button>
              </div>
            </div>
            <div class="app-section-body app-section-body--no-padding">
              <div class="report-table-container">
                <table class="report-table reports-table">
                  <thead>
                    <tr>
                      <th>{{ $t("payrollEmployee") }}</th>
                      <th>{{ $t("amount") }}</th>
                      <th>{{ $t("remaining") }}</th>
                      <th>{{ $t("date") }}</th>
                      <th>{{ $t("notes") }}</th>
                      <th>{{ $t("actions") }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-if="!advances.length">
                      <td colspan="6" class="payroll-empty-cell">{{ $t("payrollNoAdvances") }}</td>
                    </tr>
                    <tr v-for="a in advances" :key="a.id">
                      <td>{{ a.employee?.name || "—" }}</td>
                      <td class="payroll-amount">{{ money(a.amount) }}</td>
                      <td class="payroll-amount">{{ money(a.remainingAmount) }}</td>
                      <td>{{ formatDate(a.date) }}</td>
                      <td>{{ a.notes || "—" }}</td>
                      <td>
                        <div class="actions-cell">
                          <button
                            v-if="!a.isClosed && a.remainingAmount > 0"
                            type="button"
                            class="action-btn action-btn--icon action-btn--edit"
                            :title="$t('payrollCloseAdvance')"
                            @click="onCloseAdvance(a)"
                          >
                            <b-icon icon="check2-circle" class="action-icon"></b-icon>
                          </button>
                          <button
                            type="button"
                            class="action-btn action-btn--icon action-btn--delete"
                            :title="$t('delete')"
                            @click="onDeleteAdvance(a)"
                          >
                            <b-icon icon="trash" class="action-icon"></b-icon>
                          </button>
                        </div>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <!-- Adjustments -->
          <div v-if="activeTab === 'adjustments'" class="app-section-card">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="sliders"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("adjustments") }}</h3>
                  <p class="app-section-subtitle">{{ $t("payrollAdjustmentsHint") }}</p>
                </div>
              </div>
              <div class="app-header-actions">
                <button type="button" class="users-add-button" @click="showAdjModal = true">
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("addAdjustment") }}</span>
                </button>
              </div>
            </div>
            <div class="app-section-body app-section-body--no-padding">
              <div class="report-table-container">
                <table class="report-table reports-table">
                  <thead>
                    <tr>
                      <th>{{ $t("payrollEmployee") }}</th>
                      <th>{{ $t("type") }}</th>
                      <th>{{ $t("amount") }}</th>
                      <th>{{ $t("absenceDays") }}</th>
                      <th>{{ $t("date") }}</th>
                      <th>{{ $t("notes") }}</th>
                      <th>{{ $t("actions") }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-if="!adjustments.length">
                      <td colspan="7" class="payroll-empty-cell">{{ $t("payrollNoAdjustments") }}</td>
                    </tr>
                    <tr v-for="adj in adjustments" :key="adj.id">
                      <td>{{ adj.employee?.name || "—" }}</td>
                      <td>
                        <span class="payroll-badge" :data-type="adj.type">{{ adjTypeLabel(adj.type) }}</span>
                      </td>
                      <td class="payroll-amount">{{ money(adj.amount) }}</td>
                      <td>{{ adj.type === 2 ? adj.absenceDays : "—" }}</td>
                      <td>{{ formatDate(adj.date) }}</td>
                      <td>{{ adj.notes || "—" }}</td>
                      <td>
                        <div class="actions-cell">
                          <button
                            type="button"
                            class="action-btn action-btn--icon action-btn--delete"
                            @click="onDeleteAdj(adj)"
                          >
                            <b-icon icon="trash" class="action-icon"></b-icon>
                          </button>
                        </div>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <!-- Runs -->
          <div v-if="activeTab === 'runs'" class="app-section-card">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="calendar2-check"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("payrollRuns") }}</h3>
                  <p class="app-section-subtitle">{{ $t("payrollRunsHint") }}</p>
                </div>
              </div>
              <div class="app-header-actions payroll-create-run">
                <select v-model.number="newRun.year" class="users-form-input payroll-select">
                  <option v-for="y in yearOptions" :key="y" :value="y">{{ y }}</option>
                </select>
                <select v-model.number="newRun.month" class="users-form-input payroll-select">
                  <option v-for="m in 12" :key="m" :value="m">{{ monthLabel(m) }}</option>
                </select>
                <button type="button" class="users-add-button" :disabled="saving" @click="createRun">
                  <b-icon icon="plus-circle-fill" class="button-icon"></b-icon>
                  <span class="button-text">{{ $t("createPayrollRun") }}</span>
                </button>
              </div>
            </div>
            <div class="app-section-body">
              <div class="payroll-runs-list">
                <button
                  v-for="run in runs"
                  :key="run.id"
                  type="button"
                  class="payroll-run-card"
                  :class="{ selected: selectedRunId === run.id }"
                  @click="openRun(run.id)"
                >
                  <strong>{{ run.year }}/{{ String(run.month).padStart(2, "0") }}</strong>
                  <span class="payroll-badge" :data-status="run.status">{{ runStatusLabel(run.status) }}</span>
                </button>
                <p v-if="!runs.length" class="payroll-empty-inline">{{ $t("payrollNoRuns") }}</p>
              </div>

              <div v-if="selectedRun" class="payroll-run-detail">
                <div class="app-section-header app-section-header--toolbar payroll-run-toolbar">
                  <div class="app-section-title-wrap">
                    <div>
                      <h3 class="app-section-title">
                        {{ selectedRun.year }}/{{ String(selectedRun.month).padStart(2, "0") }}
                      </h3>
                      <p class="app-section-subtitle">{{ runStatusLabel(selectedRun.status) }}</p>
                    </div>
                  </div>
                  <div class="app-header-actions payroll-run-btns app-equal-btn-group">
                    <button
                      v-if="selectedRun.status === 0"
                      type="button"
                      class="users-form-cancel-button"
                      @click="onRegenerate"
                    >
                      {{ $t("regenerate") }}
                    </button>
                    <button
                      v-if="selectedRun.status === 0"
                      type="button"
                      class="users-add-button"
                      @click="onApprove"
                    >
                      {{ $t("approve") }}
                    </button>
                    <button
                      v-if="selectedRun.status === 1 && !hasAnyPaidLine"
                      type="button"
                      class="users-form-cancel-button"
                      @click="onUnapprove"
                    >
                      {{ $t("unapprove") }}
                    </button>
                    <button
                      v-if="selectedRun.status === 1 && hasUnpaidLines"
                      type="button"
                      class="users-add-button"
                      @click="onPay"
                    >
                      {{ $t("payPayroll") }}
                    </button>
                    <button
                      v-if="(selectedRun.status === 0 || selectedRun.status === 1) && !hasAnyPaidLine"
                      type="button"
                      class="users-form-cancel-button"
                      @click="onCancelRun"
                    >
                      {{ $t("payrollCancelRun") }}
                    </button>
                  </div>
                </div>

                <div class="report-table-container">
                  <table class="report-table reports-table">
                    <thead>
                      <tr>
                        <th>{{ $t("payrollEmployee") }}</th>
                        <th>{{ $t("workDays") }}</th>
                        <th>{{ $t("baseAmount") }}</th>
                        <th>{{ $t("overtime") }}</th>
                        <th>{{ $t("deduction") }}</th>
                        <th>{{ $t("absence") }}</th>
                        <th>{{ $t("advanceDeducted") }}</th>
                        <th>{{ $t("netAmount") }}</th>
                        <th>{{ $t("actions") }}</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="line in selectedRun.lines || []" :key="line.id">
                        <td>{{ line.employee?.name || ("#" + line.employeeId) }}</td>
                        <td>
                          <input
                            v-if="selectedRun.status === 0"
                            type="number"
                            step="0.5"
                            class="users-form-input cell-input"
                            v-model.number="line.workDays"
                          />
                          <span v-else>{{ line.workDays }}</span>
                        </td>
                        <td>
                          <input
                            v-if="selectedRun.status === 0"
                            type="number"
                            step="0.01"
                            class="users-form-input cell-input"
                            v-model.number="line.baseAmount"
                          />
                          <span v-else class="payroll-amount">{{ money(line.baseAmount) }}</span>
                        </td>
                        <td>
                          <input
                            v-if="selectedRun.status === 0"
                            type="number"
                            step="0.01"
                            class="users-form-input cell-input"
                            v-model.number="line.overtimeAmount"
                          />
                          <span v-else class="payroll-amount">{{ money(line.overtimeAmount) }}</span>
                        </td>
                        <td>
                          <input
                            v-if="selectedRun.status === 0"
                            type="number"
                            step="0.01"
                            class="users-form-input cell-input"
                            v-model.number="line.deductionAmount"
                          />
                          <span v-else class="payroll-amount">{{ money(line.deductionAmount) }}</span>
                        </td>
                        <td>
                          <input
                            v-if="selectedRun.status === 0"
                            type="number"
                            step="0.01"
                            class="users-form-input cell-input"
                            v-model.number="line.absenceAmount"
                          />
                          <span v-else class="payroll-amount">{{ money(line.absenceAmount) }}</span>
                        </td>
                        <td>
                          <input
                            v-if="selectedRun.status === 0"
                            type="number"
                            step="0.01"
                            class="users-form-input cell-input"
                            v-model.number="line.advanceDeducted"
                          />
                          <span v-else class="payroll-amount">{{ money(line.advanceDeducted) }}</span>
                        </td>
                        <td><strong class="payroll-amount">{{ money(line.netAmount) }}</strong></td>
                        <td>
                          <div class="actions-cell">
                            <button
                              v-if="selectedRun.status === 0"
                              type="button"
                              class="action-btn action-btn--icon action-btn--edit"
                              :title="$t('save')"
                              @click="saveLine(line)"
                            >
                              <b-icon icon="check-lg" class="action-icon"></b-icon>
                            </button>
                            <button
                              v-if="selectedRun.status === 1 && !line.isPaid"
                              type="button"
                              class="action-btn action-btn--icon action-btn--edit"
                              :title="$t('payPayrollLine')"
                              @click="onPayLine(line)"
                            >
                              <b-icon icon="wallet2" class="action-icon"></b-icon>
                            </button>
                            <button
                              v-if="line.isPaid && !line.isHandedOver"
                              type="button"
                              class="action-btn action-btn--icon action-btn--success"
                              :title="$t('payrollLinePaid')"
                              disabled
                            >
                              <b-icon icon="wallet2" class="action-icon"></b-icon>
                            </button>
                            <button
                              v-if="line.isPaid && !line.isHandedOver"
                              type="button"
                              class="action-btn action-btn--icon action-btn--edit"
                              :title="$t('payrollHandover')"
                              @click="onHandover(line)"
                            >
                              <b-icon icon="person-check" class="action-icon"></b-icon>
                            </button>
                            <button
                              v-if="line.isHandedOver"
                              type="button"
                              class="action-btn action-btn--icon action-btn--success"
                              :title="$t('payrollHandedOver')"
                              disabled
                            >
                              <b-icon icon="person-check" class="action-icon"></b-icon>
                            </button>
                            <button
                              v-if="line.isHandedOver"
                              type="button"
                              class="action-btn action-btn--icon action-btn--edit"
                              :title="$t('payrollReprintReceipt')"
                              @click="onHandover(line)"
                            >
                              <b-icon icon="printer" class="action-icon"></b-icon>
                            </button>
                          </div>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>

          <!-- Handovers tab -->
          <div v-if="activeTab === 'handovers'" class="app-section-card">
            <div class="app-section-header app-section-header--toolbar">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="journal-check"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("payrollHandoversTab") }}</h3>
                  <p class="app-section-subtitle">{{ $t("payrollHandoversHint") }}</p>
                </div>
              </div>
              <div class="app-header-actions app-equal-btn-group">
                <button type="button" class="btn-refresh" @click="loadHandovers" :disabled="loading">
                  <b-icon icon="arrow-clockwise" class="button-icon" :class="{ spinning: loading }"></b-icon>
                  <span class="button-text">{{ $t("refresh") }}</span>
                </button>
              </div>
            </div>
            <div class="app-section-body app-section-body--no-padding">
              <div class="app-overview-grid" style="padding: 1rem 1rem 0">
                <div class="app-overview-stat">
                  <div class="app-overview-stat-value">{{ handovers.count || 0 }}</div>
                  <div class="app-overview-stat-label">{{ $t("payrollHandoversCount") }}</div>
                </div>
                <div class="app-overview-stat">
                  <div class="app-overview-stat-value">{{ money(handovers.totalNet) }}</div>
                  <div class="app-overview-stat-label">{{ $t("payrollHandoversTotal") }}</div>
                </div>
              </div>
              <div class="report-table-container">
                <table class="report-table reports-table">
                  <thead>
                    <tr>
                      <th>{{ $t("payrollEmployee") }}</th>
                      <th>{{ $t("payrollRun") }}</th>
                      <th>{{ $t("netAmount") }}</th>
                      <th>{{ $t("payrollHandoverDate") }}</th>
                      <th>{{ $t("actions") }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-if="!(handovers.items || []).length">
                      <td colspan="5" class="payroll-empty-cell">{{ $t("payrollNoHandovers") }}</td>
                    </tr>
                    <tr v-for="item in handovers.items || []" :key="item.id">
                      <td>{{ item.employee?.name || ("#" + item.employeeId) }}</td>
                      <td>
                        {{ item.payrollRun?.year }}/{{
                          String(item.payrollRun?.month || "").padStart(2, "0")
                        }}
                      </td>
                      <td class="payroll-amount">{{ money(item.netAmount) }}</td>
                      <td>{{ formatDateTime(item.handedOverAt) }}</td>
                      <td>
                        <div class="actions-cell">
                          <button
                            type="button"
                            class="action-btn action-btn--icon action-btn--edit"
                            :title="$t('payrollReprintReceipt')"
                            @click="reprintHandover(item)"
                          >
                            <b-icon icon="printer" class="action-icon"></b-icon>
                          </button>
                        </div>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <!-- Reports -->
          <div v-if="activeTab === 'reports'" class="app-section-card">
            <div class="app-section-header">
              <div class="app-section-title-wrap">
                <div class="app-section-icon-wrap">
                  <b-icon icon="bar-chart-fill"></b-icon>
                </div>
                <div>
                  <h3 class="app-section-title">{{ $t("payrollReportsTab") }}</h3>
                  <p class="app-section-subtitle">{{ $t("payrollReportsHint") }}</p>
                </div>
              </div>
            </div>
            <div class="app-section-body payroll-reports-body">
              <div class="payroll-reports-filters">
                <div class="payroll-reports-filter">
                  <label class="users-form-label">
                    <b-icon icon="calendar2-week" class="payroll-filter-icon"></b-icon>
                    {{ $t("payrollRun") }}
                  </label>
                  <select
                    v-model.number="reportRunId"
                    class="users-form-input"
                    @change="loadRunReport"
                  >
                    <option :value="0">{{ $t("payrollSelectOption") }}</option>
                    <option v-for="run in runs" :key="'r' + run.id" :value="run.id">
                      {{ run.year }}/{{ String(run.month).padStart(2, "0") }}
                    </option>
                  </select>
                </div>
                <div class="payroll-reports-filter">
                  <label class="users-form-label">
                    <b-icon icon="person-badge" class="payroll-filter-icon"></b-icon>
                    {{ $t("payrollEmployee") }}
                  </label>
                  <select
                    v-model.number="reportEmployeeId"
                    class="users-form-input"
                    @change="loadEmployeeLedger"
                  >
                    <option :value="0">{{ $t("payrollSelectOption") }}</option>
                    <option v-for="e in employees" :key="e.id" :value="e.id">{{ e.name }}</option>
                  </select>
                </div>
              </div>

              <div
                v-if="!runReport && !employeeLedger"
                class="payroll-reports-empty"
              >
                <div class="payroll-reports-empty-icon">
                  <b-icon icon="pie-chart-fill"></b-icon>
                </div>
                <p class="payroll-reports-empty-title">{{ $t("payrollReportsHint") }}</p>
                <p class="payroll-reports-empty-text">{{ $t("payrollSelectOption") }}</p>
              </div>

              <div v-if="runReport" class="payroll-reports-block">
                <div class="payroll-reports-block-head">
                  <div class="payroll-reports-block-icon">
                    <b-icon icon="calendar2-check"></b-icon>
                  </div>
                  <div>
                    <h4 class="payroll-reports-block-title">{{ $t("payrollRun") }}</h4>
                    <p class="payroll-reports-block-sub">
                      {{ selectedReportRunLabel }}
                    </p>
                  </div>
                  <div class="payroll-reports-net-pill">
                    <span>{{ $t("netAmount") }}</span>
                    <strong>{{ money(runReport.totalNet) }}</strong>
                  </div>
                </div>
                <div class="app-overview-grid payroll-report-stats">
                  <div class="app-overview-stat">
                    <span class="app-overview-stat-icon app-overview-stat-icon--primary">
                      <b-icon icon="people-fill"></b-icon>
                    </span>
                    <div>
                      <div class="app-overview-stat-value">{{ runReport.employeeCount }}</div>
                      <div class="app-overview-stat-label">{{ $t("employees") }}</div>
                    </div>
                  </div>
                  <div class="app-overview-stat">
                    <span class="app-overview-stat-icon app-overview-stat-icon--info">
                      <b-icon icon="cash"></b-icon>
                    </span>
                    <div>
                      <div class="app-overview-stat-value">{{ money(runReport.totalBase) }}</div>
                      <div class="app-overview-stat-label">{{ $t("baseAmount") }}</div>
                    </div>
                  </div>
                  <div class="app-overview-stat">
                    <span class="app-overview-stat-icon app-overview-stat-icon--success">
                      <b-icon icon="plus-circle"></b-icon>
                    </span>
                    <div>
                      <div class="app-overview-stat-value">{{ money(runReport.totalOvertime) }}</div>
                      <div class="app-overview-stat-label">{{ $t("overtime") }}</div>
                    </div>
                  </div>
                  <div class="app-overview-stat">
                    <span class="app-overview-stat-icon app-overview-stat-icon--danger">
                      <b-icon icon="dash-circle"></b-icon>
                    </span>
                    <div>
                      <div class="app-overview-stat-value">{{ money(runReport.totalDeductions) }}</div>
                      <div class="app-overview-stat-label">{{ $t("deduction") }}</div>
                    </div>
                  </div>
                  <div class="app-overview-stat">
                    <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                      <b-icon icon="calendar-x"></b-icon>
                    </span>
                    <div>
                      <div class="app-overview-stat-value">{{ money(runReport.totalAbsence) }}</div>
                      <div class="app-overview-stat-label">{{ $t("absence") }}</div>
                    </div>
                  </div>
                  <div class="app-overview-stat">
                    <span class="app-overview-stat-icon app-overview-stat-icon--warning">
                      <b-icon icon="wallet2"></b-icon>
                    </span>
                    <div>
                      <div class="app-overview-stat-value">{{ money(runReport.totalAdvanceDeducted) }}</div>
                      <div class="app-overview-stat-label">{{ $t("advanceDeducted") }}</div>
                    </div>
                  </div>
                </div>
              </div>

              <div v-if="employeeLedger" class="payroll-reports-block">
                <div class="payroll-reports-block-head">
                  <div class="payroll-reports-block-icon payroll-reports-block-icon--accent">
                    <b-icon icon="person-vcard"></b-icon>
                  </div>
                  <div>
                    <h4 class="payroll-reports-block-title">{{ $t("employeeLedger") }}</h4>
                    <p class="payroll-reports-block-sub">{{ employeeLedger.employee?.name }}</p>
                  </div>
                </div>
                <div class="payroll-ledger-grid">
                  <div class="payroll-ledger-card payroll-ledger-card--balance">
                    <span class="payroll-ledger-card-icon">
                      <b-icon icon="cash-coin"></b-icon>
                    </span>
                    <div class="payroll-ledger-card-label">{{ $t("openAdvanceBalance") }}</div>
                    <div class="payroll-ledger-card-value">{{ money(employeeLedger.openAdvanceBalance) }}</div>
                  </div>
                  <div class="payroll-ledger-card">
                    <span class="payroll-ledger-card-icon">
                      <b-icon icon="cash"></b-icon>
                    </span>
                    <div class="payroll-ledger-card-label">{{ $t("advances") }}</div>
                    <div class="payroll-ledger-card-value">{{ (employeeLedger.advances || []).length }}</div>
                  </div>
                  <div class="payroll-ledger-card">
                    <span class="payroll-ledger-card-icon">
                      <b-icon icon="sliders"></b-icon>
                    </span>
                    <div class="payroll-ledger-card-label">{{ $t("adjustments") }}</div>
                    <div class="payroll-ledger-card-value">{{ (employeeLedger.adjustments || []).length }}</div>
                  </div>
                  <div class="payroll-ledger-card">
                    <span class="payroll-ledger-card-icon">
                      <b-icon icon="list-check"></b-icon>
                    </span>
                    <div class="payroll-ledger-card-label">{{ $t("payrollLines") }}</div>
                    <div class="payroll-ledger-card-value">{{ (employeeLedger.payrollLines || []).length }}</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <b-modal v-model="showAdvanceModal" hide-header hide-footer centered class="users-modal">
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("addAdvance") }}</h2>
        <form class="users-form" @submit.prevent="submitAdvance">
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("payrollEmployee") }} <span class="required">*</span></label>
            <select v-model.number="advanceForm.employeeId" class="users-form-input" required>
              <option value="">{{ $t("payrollSelectOption") }}</option>
              <option v-for="e in employees" :key="e.id" :value="e.id">{{ e.name }}</option>
            </select>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("amount") }} <span class="required">*</span></label>
            <input
              v-model.number="advanceForm.amount"
              type="number"
              step="0.01"
              min="0.01"
              class="users-form-input"
              required
            />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("date") }}</label>
            <input v-model="advanceForm.date" type="date" class="users-form-input" />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("notes") }}</label>
            <input v-model="advanceForm.notes" class="users-form-input" />
          </div>
          <div class="users-form-actions">
            <button type="button" class="users-form-cancel-button" @click="showAdvanceModal = false">
              {{ $t("cancel") }}
            </button>
            <button type="submit" class="users-form-submit-button" :disabled="saving">
              {{ $t("save") }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>

    <b-modal v-model="showAdjModal" hide-header hide-footer centered class="users-modal">
      <div class="modal-content-wrapper">
        <h2 class="modal-title">{{ $t("addAdjustment") }}</h2>
        <form class="users-form" @submit.prevent="submitAdj">
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("payrollEmployee") }} <span class="required">*</span></label>
            <select v-model.number="adjForm.employeeId" class="users-form-input" required>
              <option value="">{{ $t("payrollSelectOption") }}</option>
              <option v-for="e in employees" :key="e.id" :value="e.id">{{ e.name }}</option>
            </select>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("type") }}</label>
            <select v-model.number="adjForm.type" class="users-form-input">
              <option :value="0">{{ $t("overtime") }}</option>
              <option :value="1">{{ $t("deduction") }}</option>
              <option :value="2">{{ $t("absence") }}</option>
            </select>
          </div>
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("amount") }}</label>
            <input v-model.number="adjForm.amount" type="number" step="0.01" class="users-form-input" />
          </div>
          <div v-if="adjForm.type === 2" class="users-form-group">
            <label class="users-form-label">{{ $t("absenceDays") }}</label>
            <input
              v-model.number="adjForm.absenceDays"
              type="number"
              step="0.5"
              class="users-form-input"
            />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("date") }}</label>
            <input v-model="adjForm.date" type="date" class="users-form-input" />
          </div>
          <div class="users-form-group">
            <label class="users-form-label">{{ $t("notes") }}</label>
            <input v-model="adjForm.notes" class="users-form-input" />
          </div>
          <div class="users-form-actions">
            <button type="button" class="users-form-cancel-button" @click="showAdjModal = false">
              {{ $t("cancel") }}
            </button>
            <button type="submit" class="users-form-submit-button" :disabled="saving">
              {{ $t("save") }}
            </button>
          </div>
        </form>
      </div>
    </b-modal>
  </div>
</template>

<script>
import AppHeader from "@/components/Layout/AppHeader.vue";
import { HTTP } from "@/http/api.js";
import * as payrollApi from "@/http/payrollApi.js";
import { printSalaryHandoverReceipt } from "@/utils/payrollReceiptPrint.js";

export default {
  name: "PayrollView",
  components: { AppHeader },
  data() {
    const now = new Date();
    return {
      loading: false,
      saving: false,
      activeTab: "overview",
      employees: [],
      balances: { totalOpenAdvances: 0, employees: [] },
      advances: [],
      adjustments: [],
      runs: [],
      selectedRunId: null,
      selectedRun: null,
      showAdvanceModal: false,
      showAdjModal: false,
      advanceForm: { employeeId: "", amount: null, date: "", notes: "" },
      adjForm: { employeeId: "", type: 0, amount: 0, absenceDays: 0, date: "", notes: "" },
      newRun: { year: now.getFullYear(), month: now.getMonth() + 1 },
      reportRunId: 0,
      reportEmployeeId: 0,
      runReport: null,
      employeeLedger: null,
      handovers: { count: 0, totalNet: 0, items: [] },
    };
  },
  computed: {
    tabs() {
      return [
        { id: "overview", icon: "speedometer2", label: this.$t("payrollOverviewTab") },
        { id: "advances", icon: "cash", label: this.$t("advances") },
        { id: "adjustments", icon: "sliders", label: this.$t("adjustments") },
        { id: "runs", icon: "calendar2-check", label: this.$t("payrollRuns") },
        { id: "handovers", icon: "journal-check", label: this.$t("payrollHandoversTab") },
        { id: "reports", icon: "bar-chart-fill", label: this.$t("payrollReportsTab") },
      ];
    },
    yearOptions() {
      const y = new Date().getFullYear();
      return [y - 1, y, y + 1];
    },
    activeEmployeesCount() {
      return (this.balances.employees || []).filter((e) => e.isActive).length;
    },
    hasAnyPaidLine() {
      return (this.selectedRun?.lines || []).some((l) => l.isPaid);
    },
    hasUnpaidLines() {
      return (this.selectedRun?.lines || []).some((l) => !l.isPaid);
    },
    selectedReportRunLabel() {
      const run = (this.runs || []).find((r) => r.id === this.reportRunId);
      if (!run) return "—";
      return `${run.year}/${String(run.month).padStart(2, "0")}`;
    },
  },
  mounted() {
    this.refreshAll();
  },
  methods: {
    money(v) {
      const n = Number(v);
      if (!Number.isFinite(n)) return "0";
      return n.toLocaleString("en-US", { minimumFractionDigits: 0, maximumFractionDigits: 2 });
    },
    formatDate(d) {
      if (!d) return "—";
      return String(d).slice(0, 10);
    },
    formatDateTime(d) {
      if (!d) return "—";
      return String(d).slice(0, 19).replace("T", " ");
    },
    monthLabel(m) {
      return this.$t(`payrollMonth${m}`) || String(m);
    },
    salaryTypeLabel(t) {
      if (t === 0) return this.$t("salaryTypeDaily");
      if (t === 1) return this.$t("salaryTypeWeekly");
      return this.$t("salaryTypeMonthly");
    },
    adjTypeLabel(t) {
      if (t === 0) return this.$t("overtime");
      if (t === 1) return this.$t("deduction");
      return this.$t("absence");
    },
    runStatusLabel(s) {
      const map = {
        0: this.$t("draft"),
        1: this.$t("approved"),
        2: this.$t("paid"),
        3: this.$t("cancelled"),
      };
      return map[s] || String(s);
    },
    toastOk(msg) {
      this.$toast?.success?.(msg, {
        position: "top-right",
        timeout: 3000,
        rtl: this.$i18n.locale === "ar",
      });
    },
    toastErr(msg) {
      this.$toast?.error?.(msg, {
        position: "top-right",
        timeout: 4000,
        rtl: this.$i18n.locale === "ar",
      });
    },
    async confirm(message, options = {}) {
      if (this.$confirm) {
        return this.$confirm({
          message,
          title: options.title || this.$t("confirmAction") || "تأكيد العملية",
          confirmText: options.confirmText || this.$t("confirm") || "تأكيد",
          cancelText: options.cancelText,
          variant: options.variant || "warning",
          icon: options.icon,
        });
      }
      return window.confirm(message);
    },
    async confirmDanger(message) {
      if (this.$confirm) {
        return this.$confirm({
          message,
          title: this.$t("confirm_delete") || this.$t("confirmDelete"),
          confirmText: this.$t("deleteButtonLabel") || this.$t("delete"),
          variant: "danger",
        });
      }
      return window.confirm(message);
    },
    async refreshAll() {
      this.loading = true;
      try {
        await Promise.all([
          this.loadEmployees(),
          this.loadBalances(),
          this.loadAdvances(),
          this.loadAdjustments(),
          this.loadRuns(),
          this.loadHandovers(),
        ]);
        if (this.selectedRunId) await this.openRun(this.selectedRunId);
      } finally {
        this.loading = false;
      }
    },
    async loadEmployees() {
      const res = await HTTP.get("Employees");
      this.employees = res.data?.data || [];
    },
    async loadBalances() {
      const res = await payrollApi.getAdvanceBalances();
      this.balances = res.data?.data || { totalOpenAdvances: 0, employees: [] };
    },
    async loadAdvances() {
      const res = await payrollApi.getAdvances();
      this.advances = res.data?.data || [];
    },
    async loadAdjustments() {
      const res = await payrollApi.getSalaryAdjustments();
      this.adjustments = res.data?.data || [];
    },
    async loadRuns() {
      const res = await payrollApi.getPayrollRuns();
      this.runs = res.data?.data || [];
    },
    async loadHandovers() {
      try {
        const res = await payrollApi.getPayrollHandovers();
        this.handovers = res.data?.data || { count: 0, totalNet: 0, items: [] };
      } catch (e) {
        this.handovers = { count: 0, totalNet: 0, items: [] };
      }
    },
    async onHandover(line) {
      if (!this.selectedRunId) return;
      const isReprint = !!line.isHandedOver;
      if (
        !isReprint &&
        !(await this.confirm(this.$t("confirmPayrollHandover"), {
          title: this.$t("confirmPayrollHandoverTitle") || "تأكيد تسليم الراتب",
          confirmText: this.$t("handOverSalary") || "تسليم",
          variant: "info",
        }))
      ) {
        return;
      }
      try {
        const res = await payrollApi.handoverPayrollLine(this.selectedRunId, line.id);
        if (res.data?.errorStatus) {
          this.toastErr(res.data.message || this.$t("error"));
          return;
        }
        const payload = res.data?.data || {};
        const updatedLine = payload.line || line;
        const runInfo = payload.run || this.selectedRun;
        Object.assign(line, updatedLine);
        const printed = await printSalaryHandoverReceipt({
          line: updatedLine,
          run: runInfo,
          locale: this.$i18n.locale,
        });
        if (printed.ok) {
          this.toastOk(
            isReprint
              ? this.$t("payrollReceiptPrinted")
              : this.$t("payrollHandoverSuccess")
          );
        } else {
          this.toastErr(this.$t("payrollPrintFailed"));
        }
        await this.loadHandovers();
      } catch (e) {
        this.toastErr(e.response?.data?.message || e.message);
      }
    },
    async reprintHandover(item) {
      try {
        const printed = await printSalaryHandoverReceipt({
          line: item,
          run: item.payrollRun || {
            year: item.payrollRun?.year,
            month: item.payrollRun?.month,
          },
          locale: this.$i18n.locale,
        });
        if (printed.ok) this.toastOk(this.$t("payrollReceiptPrinted"));
        else this.toastErr(this.$t("payrollPrintFailed"));
      } catch (e) {
        this.toastErr(e.message);
      }
    },
    async submitAdvance() {
      this.saving = true;
      try {
        const res = await payrollApi.createAdvance({
          employeeId: Number(this.advanceForm.employeeId),
          amount: Number(this.advanceForm.amount),
          date: this.advanceForm.date || null,
          notes: this.advanceForm.notes || null,
        });
        if (res.data?.errorStatus) {
          this.toastErr(res.data.message || this.$t("error"));
          return;
        }
        this.showAdvanceModal = false;
        this.advanceForm = { employeeId: "", amount: null, date: "", notes: "" };
        this.toastOk(res.data?.message || this.$t("save"));
        await Promise.all([this.loadAdvances(), this.loadBalances()]);
      } catch (e) {
        this.toastErr(e.response?.data?.message || e.message);
      } finally {
        this.saving = false;
      }
    },
    async onCloseAdvance(a) {
      if (!(await this.confirm(this.$t("confirmCloseAdvance"), {
        title: this.$t("confirmCloseAdvanceTitle") || "إغلاق السلفة",
        confirmText: this.$t("close") || "إغلاق",
        variant: "warning",
      }))) return;
      try {
        await payrollApi.closeAdvance(a.id);
        await Promise.all([this.loadAdvances(), this.loadBalances()]);
      } catch (e) {
        this.toastErr(e.response?.data?.message || e.message);
      }
    },
    async onDeleteAdvance(a) {
      if (!(await this.confirmDanger(this.$t("confirmDelete")))) return;
      try {
        await payrollApi.deleteAdvance(a.id);
        await Promise.all([this.loadAdvances(), this.loadBalances()]);
      } catch (e) {
        this.toastErr(e.response?.data?.message || e.message);
      }
    },
    async submitAdj() {
      this.saving = true;
      try {
        const res = await payrollApi.createSalaryAdjustment({
          employeeId: Number(this.adjForm.employeeId),
          type: Number(this.adjForm.type),
          amount: Number(this.adjForm.amount) || 0,
          absenceDays: Number(this.adjForm.absenceDays) || 0,
          date: this.adjForm.date || null,
          notes: this.adjForm.notes || null,
        });
        if (res.data?.errorStatus) {
          this.toastErr(res.data.message || this.$t("error"));
          return;
        }
        this.showAdjModal = false;
        this.adjForm = { employeeId: "", type: 0, amount: 0, absenceDays: 0, date: "", notes: "" };
        this.toastOk(res.data?.message || this.$t("save"));
        await this.loadAdjustments();
      } catch (e) {
        this.toastErr(e.response?.data?.message || e.message);
      } finally {
        this.saving = false;
      }
    },
    async onDeleteAdj(adj) {
      if (!(await this.confirmDanger(this.$t("confirmDelete")))) return;
      try {
        await payrollApi.deleteSalaryAdjustment(adj.id);
        await this.loadAdjustments();
      } catch (e) {
        this.toastErr(e.response?.data?.message || e.message);
      }
    },
    async createRun() {
      this.saving = true;
      try {
        const res = await payrollApi.createPayrollRun({
          year: this.newRun.year,
          month: this.newRun.month,
        });
        if (res.data?.errorStatus) {
          this.toastErr(res.data.message || this.$t("error"));
          return;
        }
        this.toastOk(res.data?.message || this.$t("save"));
        await this.loadRuns();
        const run = res.data?.data;
        if (run?.id) await this.openRun(run.id);
      } catch (e) {
        this.toastErr(e.response?.data?.message || e.message);
      } finally {
        this.saving = false;
      }
    },
    async openRun(id) {
      this.selectedRunId = id;
      const res = await payrollApi.getPayrollRun(id);
      this.selectedRun = res.data?.data || null;
    },
    async saveLine(line) {
      try {
        const res = await payrollApi.updatePayrollLine(this.selectedRunId, line.id, {
          workDays: line.workDays,
          baseAmount: line.baseAmount,
          overtimeAmount: line.overtimeAmount,
          deductionAmount: line.deductionAmount,
          absenceAmount: line.absenceAmount,
          advanceDeducted: line.advanceDeducted,
          notes: line.notes,
        });
        if (res.data?.errorStatus) {
          this.toastErr(res.data.message || this.$t("error"));
          return;
        }
        Object.assign(line, res.data.data);
        this.toastOk(res.data?.message || this.$t("save"));
      } catch (e) {
        this.toastErr(e.response?.data?.message || e.message);
      }
    },
    async onRegenerate() {
      if (!(await this.confirm(this.$t("confirmRegeneratePayroll"), {
        title: this.$t("confirmRegeneratePayrollTitle") || "إعادة توليد الدورة",
        confirmText: this.$t("regenerate") || "إعادة التوليد",
        variant: "warning",
      }))) return;
      const res = await payrollApi.regeneratePayrollRun(this.selectedRunId);
      if (res.data?.errorStatus) this.toastErr(res.data.message);
      else {
        this.selectedRun = res.data.data;
        this.toastOk(res.data.message);
      }
    },
    async onApprove() {
      const res = await payrollApi.approvePayrollRun(this.selectedRunId);
      if (res.data?.errorStatus) this.toastErr(res.data.message);
      else {
        this.selectedRun = res.data.data;
        await this.loadRuns();
        this.toastOk(res.data.message);
      }
    },
    async onUnapprove() {
      const res = await payrollApi.unapprovePayrollRun(this.selectedRunId);
      if (res.data?.errorStatus) this.toastErr(res.data.message);
      else {
        this.selectedRun = res.data.data;
        await this.loadRuns();
        this.toastOk(res.data.message);
      }
    },
    async onPay() {
      if (!(await this.confirm(this.$t("confirmPayPayroll"), {
        title: this.$t("confirmPayPayrollTitle") || "تأكيد صرف الرواتب",
        confirmText: this.$t("payPayroll") || "صرف الرواتب",
        variant: "warning",
        icon: "wallet2",
      }))) return;
      try {
        const res = await payrollApi.payPayrollRun(this.selectedRunId);
        if (res.data?.errorStatus) this.toastErr(res.data.message);
        else {
          this.selectedRun = res.data.data;
          await Promise.all([this.loadRuns(), this.loadBalances(), this.loadAdvances()]);
          this.toastOk(res.data.message);
        }
      } catch (e) {
        this.toastErr(e.response?.data?.message || e.message);
      }
    },
    async onPayLine(line) {
      if (!this.selectedRunId || !line?.id) return;
      const name = line.employee?.name || `#${line.employeeId}`;
      if (!(await this.confirm(this.$t("confirmPayPayrollLine", { name }), {
        title: this.$t("confirmPayPayrollLineTitle") || "تأكيد صرف الراتب",
        confirmText: this.$t("payPayrollLine") || "صرف الراتب",
        variant: "warning",
        icon: "wallet2",
      }))) return;
      try {
        const res = await payrollApi.payPayrollLine(this.selectedRunId, line.id);
        if (res.data?.errorStatus) this.toastErr(res.data.message);
        else {
          this.selectedRun = res.data.data;
          await Promise.all([this.loadRuns(), this.loadBalances(), this.loadAdvances()]);
          this.toastOk(res.data.message);
        }
      } catch (e) {
        this.toastErr(e.response?.data?.message || e.message);
      }
    },
    async onCancelRun() {
      if (!(await this.confirm(this.$t("confirmCancelPayroll"), {
        title: this.$t("confirmCancelPayrollTitle") || "إلغاء دورة الرواتب",
        confirmText: this.$t("cancelPayroll") || this.$t("cancel") || "إلغاء",
        variant: "danger",
      }))) return;
      const res = await payrollApi.cancelPayrollRun(this.selectedRunId);
      if (res.data?.errorStatus) this.toastErr(res.data.message);
      else {
        this.selectedRun = res.data.data;
        await this.loadRuns();
        this.toastOk(res.data.message);
      }
    },
    async loadRunReport() {
      this.runReport = null;
      if (!this.reportRunId) return;
      const res = await payrollApi.getPayrollRunReport(this.reportRunId);
      this.runReport = res.data?.data || null;
    },
    async loadEmployeeLedger() {
      this.employeeLedger = null;
      if (!this.reportEmployeeId) return;
      const res = await payrollApi.getEmployeePayrollLedger(this.reportEmployeeId);
      this.employeeLedger = res.data?.data || null;
    },
  },
};
</script>

<style scoped>
.payroll-tabs-card {
  margin-bottom: 1rem;
  padding: 0.75rem 1rem;
}

.payroll-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.payroll-tab {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-secondary);
  padding: 0.45rem 0.85rem;
  border-radius: var(--radius-md);
  cursor: pointer;
  font-weight: 600;
  font-size: 0.9rem;
  transition: all var(--transition-base, 0.2s);
}

.payroll-tab:hover {
  border-color: color-mix(in srgb, var(--primary-color) 40%, var(--border-color));
  color: var(--text-primary);
}

.payroll-tab.active {
  background: color-mix(in srgb, var(--primary-color) 12%, var(--bg-primary));
  color: var(--primary-color);
  border-color: color-mix(in srgb, var(--primary-color) 45%, var(--border-color));
}

.payroll-tab-icon {
  font-size: 0.95rem;
}

.payroll-create-run {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  align-items: center;
}

.payroll-select {
  width: auto;
  min-width: 5.5rem;
}

.payroll-runs-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.payroll-run-card {
  display: inline-flex;
  gap: 0.5rem;
  align-items: center;
  padding: 0.55rem 0.85rem;
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  background: var(--bg-primary);
  color: var(--text-primary);
  cursor: pointer;
}

.payroll-run-card.selected {
  border-color: var(--primary-color);
  background: color-mix(in srgb, var(--primary-color) 8%, var(--bg-primary));
}

.payroll-run-toolbar {
  margin: 0 0 0.75rem;
  padding: 0;
  border: none;
}

.payroll-run-btns {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.cell-input {
  width: 5.5rem;
  min-width: 4.5rem;
  padding: 0.3rem 0.4rem !important;
}

.payroll-amount {
  font-variant-numeric: tabular-nums;
  font-weight: 600;
}

.payroll-badge {
  display: inline-flex;
  padding: 0.15rem 0.55rem;
  border-radius: var(--radius-sm, 6px);
  font-size: 0.78rem;
  font-weight: 600;
  background: color-mix(in srgb, var(--primary-color) 12%, transparent);
  color: var(--primary-color);
}

.payroll-badge.is-active,
.payroll-badge[data-status="1"],
.payroll-badge[data-type="0"] {
  background: color-mix(in srgb, #16a34a 14%, transparent);
  color: #15803d;
}

.payroll-badge.is-inactive,
.payroll-badge[data-status="3"],
.payroll-badge[data-type="1"],
.payroll-badge[data-type="2"] {
  background: color-mix(in srgb, #dc2626 12%, transparent);
  color: #b91c1c;
}

.payroll-badge[data-status="0"] {
  background: color-mix(in srgb, var(--text-secondary) 12%, transparent);
  color: var(--text-secondary);
}

.payroll-badge[data-status="2"] {
  background: color-mix(in srgb, var(--primary-color) 14%, transparent);
  color: var(--primary-color);
}

.payroll-empty-cell,
.payroll-empty-inline {
  text-align: center;
  color: var(--text-secondary);
  padding: 1.25rem !important;
}

.payroll-report-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  gap: 0.75rem;
  margin-top: 1rem;
}

.payroll-ledger-box {
  margin-top: 1.25rem;
}

.payroll-reports-body {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.payroll-reports-filters {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
  padding: 1rem 1.1rem;
  border-radius: 0.9rem;
  border: 1px solid var(--border-color);
  background:
    linear-gradient(
      135deg,
      color-mix(in srgb, var(--primary-color) 6%, var(--bg-primary)),
      var(--bg-primary)
    );
}

.payroll-reports-filter .users-form-label {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  margin-bottom: 0.45rem;
}

.payroll-filter-icon {
  color: var(--primary-color);
  font-size: 0.95rem;
}

.payroll-reports-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
  padding: 2.5rem 1rem;
  border-radius: 0.9rem;
  border: 1px dashed color-mix(in srgb, var(--primary-color) 28%, var(--border-color));
  background: color-mix(in srgb, var(--primary-color) 4%, var(--bg-secondary));
  text-align: center;
}

.payroll-reports-empty-icon {
  width: 3rem;
  height: 3rem;
  border-radius: 0.85rem;
  display: grid;
  place-items: center;
  margin-bottom: 0.35rem;
  background: color-mix(in srgb, var(--primary-color) 14%, transparent);
  color: var(--primary-color);
  font-size: 1.35rem;
}

.payroll-reports-empty-title {
  margin: 0;
  font-weight: 700;
  color: var(--text-primary);
}

.payroll-reports-empty-text {
  margin: 0;
  font-size: 0.9rem;
  color: var(--text-secondary);
}

.payroll-reports-block {
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
  padding: 1rem 1.1rem 1.15rem;
  border-radius: 0.9rem;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  box-shadow: var(--shadow-sm);
}

.payroll-reports-block-head {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.payroll-reports-block-icon {
  width: 2.5rem;
  height: 2.5rem;
  border-radius: 0.7rem;
  display: grid;
  place-items: center;
  flex-shrink: 0;
  background: color-mix(in srgb, var(--primary-color) 14%, transparent);
  color: var(--primary-color);
  font-size: 1.1rem;
}

.payroll-reports-block-icon--accent {
  background: color-mix(in srgb, var(--accent-color) 16%, transparent);
  color: var(--accent-color);
}

.payroll-reports-block-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
  color: var(--text-primary);
}

.payroll-reports-block-sub {
  margin: 0.15rem 0 0;
  font-size: 0.85rem;
  color: var(--text-secondary);
}

.payroll-reports-net-pill {
  margin-inline-start: auto;
  display: inline-flex;
  align-items: baseline;
  gap: 0.55rem;
  padding: 0.55rem 0.85rem;
  border-radius: 0.75rem;
  background: color-mix(in srgb, var(--accent-color) 12%, transparent);
  border: 1px solid color-mix(in srgb, var(--accent-color) 28%, transparent);
  color: var(--text-secondary);
  font-size: 0.8rem;
  font-weight: 600;
}

.payroll-reports-net-pill strong {
  color: var(--accent-color);
  font-size: 1.05rem;
  font-weight: 800;
  font-variant-numeric: tabular-nums;
}

.payroll-report-stats {
  margin: 0;
}

.payroll-ledger-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0.75rem;
}

.payroll-ledger-card {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  padding: 0.95rem 0.9rem;
  border-radius: 0.8rem;
  border: 1px solid var(--border-color);
  background: var(--bg-secondary);
  min-width: 0;
}

.payroll-ledger-card--balance {
  background: color-mix(in srgb, var(--primary-color) 8%, var(--bg-secondary));
  border-color: color-mix(in srgb, var(--primary-color) 25%, var(--border-color));
}

.payroll-ledger-card-icon {
  width: 2rem;
  height: 2rem;
  border-radius: 0.55rem;
  display: grid;
  place-items: center;
  background: color-mix(in srgb, var(--primary-color) 12%, transparent);
  color: var(--primary-color);
  font-size: 0.95rem;
  margin-bottom: 0.15rem;
}

.payroll-ledger-card-label {
  font-size: 0.8rem;
  color: var(--text-secondary);
  font-weight: 600;
}

.payroll-ledger-card-value {
  font-size: 1.15rem;
  font-weight: 800;
  color: var(--text-primary);
  font-variant-numeric: tabular-nums;
  line-height: 1.2;
}

@media (max-width: 900px) {
  .payroll-reports-filters,
  .payroll-ledger-grid {
    grid-template-columns: 1fr 1fr;
  }
}

@media (max-width: 560px) {
  .payroll-reports-filters,
  .payroll-ledger-grid {
    grid-template-columns: 1fr;
  }

  .payroll-reports-net-pill {
    margin-inline-start: 0;
    width: 100%;
    justify-content: space-between;
  }
}

.spinning {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.required {
  color: var(--danger-color, #dc2626);
}
</style>
