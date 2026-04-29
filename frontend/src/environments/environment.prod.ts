declare global {
  interface Window {
    TC_CONFIG?: { apiUrl?: string };
  }
}

export const environment = {
  production: true,
  apiUrl: window.TC_CONFIG?.apiUrl ?? '/api'
};
