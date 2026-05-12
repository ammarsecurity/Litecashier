import * as signalR from '@microsoft/signalr';

class SignalRService {
  constructor() {
    this.connection = null;
    this.isConnected = false;
    this.reconnectAttempts = 0;
    this.maxReconnectAttempts = 5;
  }

  getBaseUrl() {
    const raw = process.env.VUE_APP_API_URL;
    if (raw != null && String(raw).trim() !== '') {
      return String(raw).trim().replace(/\/+$/, '');
    }
    if (process.env.NODE_ENV === 'development') {
      return 'https://localhost:7216';
    }
    return '';
  }

  startConnection() {
    if (this.connection && this.isConnected) {
      return Promise.resolve();
    }

    const token = localStorage.getItem('token');
    if (!token) {
      console.warn('No token found, SignalR connection will not be authenticated');
    }

    const baseUrl = this.getBaseUrl();
    const url = `${baseUrl}/orderHub`;
    
    console.log('Starting SignalR connection to:', url);
    console.log('Token available:', !!token);
    
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(url, {
        accessTokenFactory: () => {
          const currentToken = localStorage.getItem('token');
          return currentToken || '';
        },
        transport: signalR.HttpTransportType.WebSockets,
        skipNegotiation: false
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
          if (retryContext.previousRetryCount < this.maxReconnectAttempts) {
            return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
          }
          return null;
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Connection event handlers
    this.connection.onclose(() => {
      this.isConnected = false;
      console.log('SignalR connection closed');
    });

    this.connection.onreconnecting(() => {
      this.isConnected = false;
      console.log('SignalR reconnecting...');
    });

    this.connection.onreconnected(() => {
      this.isConnected = true;
      this.reconnectAttempts = 0;
      console.log('SignalR reconnected');
    });

    return this.connection.start()
      .then(() => {
        this.isConnected = true;
        this.reconnectAttempts = 0;
        console.log('SignalR connected successfully to:', url);
      })
      .catch(error => {
        this.isConnected = false;
        console.error('SignalR connection error:', error);
        console.error('Connection URL:', url);
        // Don't throw error, just log it - allow app to continue without SignalR
        return Promise.resolve();
      });
  }

  stopConnection() {
    if (this.connection) {
      return this.connection.stop()
        .then(() => {
          this.isConnected = false;
          console.log('SignalR connection stopped');
        })
        .catch(error => {
          console.error('Error stopping SignalR connection:', error);
        });
    }
    return Promise.resolve();
  }

  on(eventName, callback) {
    if (this.connection) {
      this.connection.on(eventName, callback);
      console.log(`SignalR: Registered listener for '${eventName}'`);
    } else {
      console.warn(`SignalR: Cannot register listener for '${eventName}' - connection not available`);
    }
  }

  off(eventName, callback) {
    if (this.connection) {
      this.connection.off(eventName, callback);
    }
  }

  joinGroup(groupName) {
    if (this.connection && this.isConnected) {
      return this.connection.invoke('JoinGroup', groupName);
    }
    return Promise.resolve();
  }

  leaveGroup(groupName) {
    if (this.connection && this.isConnected) {
      return this.connection.invoke('LeaveGroup', groupName);
    }
    return Promise.resolve();
  }
}

// Export singleton instance
export default new SignalRService();

