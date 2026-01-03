import adapter from '@sveltejs/adapter-node';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

const config = {
	compilerOptions: {
        runes: true
    },
	preprocess: vitePreprocess(),
	kit: {
		adapter: adapter({
			out: 'build',
			precompress: {
				brotli: true,
				gzip: true,
				files: ['!**/config.js'] // Don't precompress config.js since it's generated at runtime
			}
		})
	}
};

export default config;
