/**
 * Production: use relative `apiUrl` so the browser hits `/api/*` on the same host (Express or edge proxy forwards JSON).
 * Set `apiBaseUrl` to an absolute origin when the API is on a different host.
 */
export const environment = {
  production: true,
  apiBaseUrl: '',
  apiUrl: '/api',
};
