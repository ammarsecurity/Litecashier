import { HTTP } from "./api.js";

export const getAdvanceBalances = () => HTTP.get("EmployeeAdvances/balances");
export const getAdvances = (params = {}) => HTTP.get("EmployeeAdvances", { params });
export const createAdvance = (payload) => HTTP.post("EmployeeAdvances", payload);
export const closeAdvance = (id) => HTTP.post(`EmployeeAdvances/${id}/close`);
export const deleteAdvance = (id) => HTTP.delete(`EmployeeAdvances/${id}`);

export const getSalaryAdjustments = (params = {}) =>
  HTTP.get("SalaryAdjustments", { params });
export const createSalaryAdjustment = (payload) =>
  HTTP.post("SalaryAdjustments", payload);
export const deleteSalaryAdjustment = (id) =>
  HTTP.delete(`SalaryAdjustments/${id}`);

export const getPayrollRuns = () => HTTP.get("PayrollRuns");
export const getPayrollRun = (id) => HTTP.get(`PayrollRuns/${id}`);
export const createPayrollRun = (payload) => HTTP.post("PayrollRuns", payload);
export const regeneratePayrollRun = (id) =>
  HTTP.post(`PayrollRuns/${id}/regenerate`);
export const updatePayrollLine = (runId, lineId, payload) =>
  HTTP.put(`PayrollRuns/${runId}/lines/${lineId}`, payload);
export const approvePayrollRun = (id) => HTTP.post(`PayrollRuns/${id}/approve`);
export const unapprovePayrollRun = (id) =>
  HTTP.post(`PayrollRuns/${id}/unapprove`);
export const payPayrollRun = (id) => HTTP.post(`PayrollRuns/${id}/pay`);
export const cancelPayrollRun = (id) => HTTP.post(`PayrollRuns/${id}/cancel`);
export const getPayrollRunReport = (id) => HTTP.get(`PayrollRuns/${id}/report`);
export const getEmployeePayrollLedger = (employeeId) =>
  HTTP.get(`PayrollRuns/reports/employee/${employeeId}`);
export const handoverPayrollLine = (runId, lineId) =>
  HTTP.post(`PayrollRuns/${runId}/lines/${lineId}/handover`);
export const getPayrollHandovers = (params = {}) =>
  HTTP.get("PayrollRuns/handovers", { params });
export const exportPayrollRunCsvUrl = (id) =>
  `${HTTP.defaults.baseURL}PayrollRuns/${id}/export`;
