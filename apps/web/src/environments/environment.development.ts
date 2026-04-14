/**
 * Development: all REST calls use paths under `apiUrl` (e.g. `/api/slots`).
 * `ng serve` forwards `/api/*` to the .NET API (see `proxy.conf.json`; default `http://127.0.0.1:5001`).
 * Set `apiBaseUrl` to an absolute origin if the API is not same-origin (e.g. staging URL).
 */
export const environment = {
  production: false,
  apiBaseUrl: '',
  apiUrl: '/api',
};
