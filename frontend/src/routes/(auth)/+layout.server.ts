import { bootstrapAuth } from '$lib/services/authService';
import { redirect } from '@sveltejs/kit';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = async ({ fetch, url }) => {
	const data = await bootstrapAuth(fetch);

	if (!data.accessToken && url.pathname !== '/login') {
		throw redirect(302, '/login');
	}

	return {
		accessToken: data.accessToken,
		expiresAt: data.expiresAt
	};
};
