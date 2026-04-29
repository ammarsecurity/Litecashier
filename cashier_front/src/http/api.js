import axios from "axios";

export const HTTP = axios.create({
    // baseURL: `https://pos-api.tatwer.tech/`,
    // Use http://localhost:5189 for development (non-SSL) or https://localhost:7216 (SSL)
    baseURL: process.env.NODE_ENV === 'production' 
        ? `https://pos-api.tanfeeth-iq.tech/` 
        : `https://pos-api.tanfeeth-iq.tech/`, // Changed to http for local development
    timeout: 30000, // 30 seconds timeout
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
                    // Redirect to login if not already there
                    if (window.location.pathname !== '/login') {
                        window.location.href = '/login';
                    }
                    break;
                case 403:
                    // Forbidden - User doesn't have permission
                    console.error('Access forbidden');
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
