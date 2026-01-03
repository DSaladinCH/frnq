// Only for development and testing purposes

import 'dotenv/config';
import fs from 'fs';

const apiBaseUrl = process.env.API_BASE_URL;
if (!apiBaseUrl) {
    throw new Error('API_BASE_URL is required');
}

fs.writeFileSync(
    'static/config.js',
    `window.__APP_CONFIG__ = { apiBaseUrl: "${apiBaseUrl}" };`
);
