import axios from "axios";

// Use relative URL for portable deployment
// If running in development, use the configured URL
// Otherwise, use relative path (empty string) to use same origin
const getBaseURL = () => {
    // Check if we're in development mode
    if (process.env.NODE_ENV === 'development') {
        return `https://localhost:7216/`;
        // return `https://restbackend.alufiq.com/`;
        // return `https://res-pos.safqasoft.com/`;
    }
    // In production/portable mode, use relative URL (empty string)
    // This will make requests to the same server serving the frontend
    // return 'https://restbackend.alufiq.com/';
    // return 'https://res-pos.safqasoft.com/';
    return `https://localhost:7216/`;

};

export const HTTP = axios.create({
    baseURL: getBaseURL(),
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
            
            // Check if current route is a public route (doesn't require auth)
            const publicRoutes = ['/menu/', '/order/', '/login', '/register', '/'];
            const isPublicRoute = publicRoutes.some(route => window.location.pathname.startsWith(route));
            
            switch (status) {
                case 401:
                    // Unauthorized - Token expired or invalid
                    // Only redirect to login if NOT on a public route
                    if (!isPublicRoute) {
                    localStorage.removeItem('token');
                    localStorage.removeItem('role');
                    localStorage.removeItem('info');
                    // Redirect to login if not already there
                    if (window.location.pathname !== '/login') {
                        window.location.href = '/login';
                    }
                    }
                    // For public routes, just log the error without redirecting
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
