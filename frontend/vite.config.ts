import tailwindcss from '@tailwindcss/vite';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig, loadEnv } from 'vite';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';
import { readFileSync } from 'fs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const packageJson = JSON.parse(readFileSync(join(__dirname, 'package.json'), 'utf-8'));

export default defineConfig(({ mode }) => {
    // Load .env file for development - this makes process.env.API_BASE_URL available during dev SSR
    // In production (Docker), the environment variable is set directly
    const env = loadEnv(mode, process.cwd(), '');

    // Set the environment variable for the Node.js process during development
    if (env.API_BASE_URL) {
        process.env.API_BASE_URL = env.API_BASE_URL;
    }

    return {
        plugins: [tailwindcss(), sveltekit()],
        define: {
            __APP_VERSION__: JSON.stringify(packageJson.version)
            // Note: process.env.API_BASE_URL is NOT in define - it's read at runtime
            // This allows Docker containers to use runtime environment variables
        }
    };
});