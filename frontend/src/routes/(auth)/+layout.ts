import { accessToken, expiresAt } from '$lib/services/authService';
import type { LayoutLoad } from './$types';

export const load: LayoutLoad = async ({ data }) => {
	if (data.accessToken && data.expiresAt) {
		accessToken.set(data.accessToken);
		expiresAt.set(new Date(data.expiresAt).getTime());
	} else {
		accessToken.set(null);
		expiresAt.set(null);
	}
	
	return {};
};
