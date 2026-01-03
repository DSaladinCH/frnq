/**
 * Shared configuration module that works on both client and server.
 *
 * - Client-side: Reads from window.__APP_CONFIG__ (loaded via config.js)
 * - Server-side: Reads from process.env.API_BASE_URL
 *
 * This is synchronous and available immediately at app startup.
 *
 * IMPORTANT:
 * - Configuration is REQUIRED at runtime (no fallbacks)
 * - Client: Must have config.js with window.__APP_CONFIG__.apiBaseUrl
 * - Server: Must have API_BASE_URL environment variable
 */

import { browser } from '$app/environment';

/**
 * Get the API base URL.
 *
 * Client (browser):
 * - MUST have window.__APP_CONFIG__.apiBaseUrl set by config.js
 *
 * Server (SSR):
 * - MUST have process.env.API_BASE_URL set
 *
 * @throws {Error} If configuration is missing
 */
export function getApiBaseUrl(): string {
    if (browser) {
        // Client-side: use window.__APP_CONFIG__ set by config.js
        const runtimeConfig = window.__APP_CONFIG__?.apiBaseUrl;

        if (runtimeConfig) {
            return runtimeConfig;
        }

        // Runtime config is REQUIRED (no fallbacks)
        throw new Error(
            'CONFIGURATION ERROR: window.__APP_CONFIG__.apiBaseUrl is not defined. ' +
                'This should be set by /config.js loaded at app startup. ' +
                'Create static/config.js from config.template.js for local development.'
        );
    } else {
        // Server-side: read from runtime environment (REQUIRED)
        const runtimeEnv = process.env.API_BASE_URL;

        if (!runtimeEnv) {
            throw new Error(
                'CONFIGURATION ERROR: API_BASE_URL environment variable is not set. ' +
                    'Set this in your .env file or Docker environment variables.'
            );
        }

        return runtimeEnv;
    }
}
