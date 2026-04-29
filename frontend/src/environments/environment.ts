declare global {
  interface Window {
    TC_CONFIG?: { apiUrl?: string };
  }
}

export const environment = {
  production: false,
  apiUrl: window.TC_CONFIG?.apiUrl ?? 'http://localhost:5555/api'
};
