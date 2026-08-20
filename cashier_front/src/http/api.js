import axios from "axios";
import { resolveApiBaseUrl } from "@/utils/apiBase.js";
import { openLicenseGate } from "@/utils/licenseGateBus.js";
import { openDevicePausedGate } from "@/utils/devicePausedGateBus.js";

/** PAX Nebula sale can wait for card/PIN on device — longer than default API calls. */
export const CARD_PAYMENT_REQUEST_TIMEOUT_MS = 180000;

/** Poll interval while waiting for card payment on device. */
export const CARD_PAYMENT_STATUS_POLL_MS = 2000;

export const startCardPaymentSale = (payload) =>
    HTTP.post("CardPayments/sale/start", payload, { timeout: 30000 });

export const getCardPaymentStatus = (transactionId) =>
    HTTP.get(`CardPayments/${transactionId}/status`, { timeout: 10000 });

export const cancelCardPayment = (transactionId) =>
    HTTP.post(`CardPayments/${transactionId}/cancel`, null, { timeout: 15000 });

export const HTTP = axios.create({
    baseURL: resolveApiBaseUrl(),
    timeout: 30000,
});

// إضافة Interceptor لتحديث Authorization Header
HTTP.interceptors.request.use(
    config => {
        const token = localStorage.getItem("token");
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    error => {
        return Promise.reject(error);
    }
);

// إضافة Response Interceptor للتعامل مع الأخطاء بشكل مركزي
HTTP.interceptors.response.use(
    response => {
        return response;
    },
    error => {
        // التعامل مع أخطاء مختلفة
        if (error.response) {
            // Server responded with error status
            const status = error.response.status;
            
            switch (status) {
                case 401:
                    // Unauthorized - Token expired or invalid
                    localStorage.removeItem('token');
                    localStorage.removeItem('role');
                    localStorage.removeItem('info');
                    localStorage.removeItem('allowedSections');
                    // Redirect to login if not already there
                    if (window.location.pathname !== '/login') {
                        window.location.href = '/login';
                    }
                    break;
                case 402:
                    openLicenseGate(error.response.data || {});
                    break;
                case 403:
                    if (error.response.data?.message === "devicePaused") {
                        openDevicePausedGate(error.response.data || {});
                    } else {
                        console.error("Access forbidden");
                    }
                    break;
                case 404:
                    // Not found
                    console.error('Resource not found');
                    break;
                case 500:
                    // Server error
                    console.error('Server error occurred');
                    break;
                default:
                    console.error('An error occurred:', error.response.data?.message || error.message);
            }
        } else if (error.request) {
            // Request was made but no response received
            console.error('No response received from server');
        } else {
            // Something else happened
            console.error('Error setting up request:', error.message);
        }
        
        return Promise.reject(error);
    }
);
