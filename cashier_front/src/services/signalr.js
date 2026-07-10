import * as signalR from '@microsoft/signalr';
import { resolveApiBaseUrl } from '@/utils/apiBase.js';

class SignalRService {
  constructor() {
    this.connection = null;
    this.isConnected = false;
    this.reconnectAttempts = 0;
    this.maxReconnectAttempts = 5;
  }

  getBaseUrl() {
    return resolveApiBaseUrl().replace(/\/+$/, '');
  }

  startConnection() {
    if (this.connection && this.isConnected) {
      return Promise.resolve();
    }

    const baseUrl = this.getBaseUrl();
    const url = `${baseUrl}/orderHub`;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(url, {
        accessTokenFactory: () => localStorage.getItem('token') || '',
        transport: signalR.HttpTransportType.WebSockets,
        skipNegotiation: false,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount < this.maxReconnectAttempts) {
            return Math.min(1000 * 2 ** retryContext.previousRetryCount, 30000);
          }
          return null;
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.connection.onclose(() => {
      this.isConnected = false;
    });

    this.connection.onreconnecting(() => {
      this.isConnected = false;
    });

    this.connection.onreconnected(() => {
      this.isConnected = true;
      this.reconnectAttempts = 0;
    });

    return this.connection
      .start()
      .then(() => {
        this.isConnected = true;
        this.reconnectAttempts = 0;
      })
      .catch(() => Promise.resolve());
  }

  stopConnection() {
    if (this.connection) {
      return this.connection
        .stop()
        .then(() => {
          this.isConnected = false;
        })
        .catch(() => {});
    }
    return Promise.resolve();
  }

  on(eventName, callback) {
    if (this.connection) {
      this.connection.on(eventName, callback);
    }
  }

  off(eventName, callback) {
    if (this.connection) {
      this.connection.off(eventName, callback);
    }
  }
}

export default new SignalRService();
