/**
 * Runtime configuration template.
 * This file is processed by envsubst at container startup to inject environment variables.
 * The result is served as /config.js and loaded before the app starts.
 */
window.__APP_CONFIG__ = {
    apiBaseUrl: '${API_BASE_URL}'
};
