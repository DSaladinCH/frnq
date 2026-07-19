import { browser } from '$app/environment';
import { getApiBaseUrl } from '$lib/config';
import { writable, derived } from 'svelte/store';

export interface AuthResponse {
	accessToken: string;
	expiresAt: string; // ISO string from API
}

export const accessToken = writable<string | null>(null);
export const expiresAt = writable<number | null>(null);

let refreshTimeout: ReturnType<typeof setTimeout> | undefined;

export const isLoggedIn = derived(
	[accessToken, expiresAt],
	([$accessToken, $expiresAt]) => !!$accessToken && !!$expiresAt && $expiresAt > Date.now()
);

export function getFullUrl(relativeUrl: string): string {
    const baseUrl = getApiBaseUrl();
    const url = baseUrl.replace(/\/$/, '') + '/' + relativeUrl.replace(/^\//, '');
    return url;
}

function isTransientStatus(status: number): boolean {
	return status === 502 || status === 503 || status === 504;
}

/**
 * Fetch with a single retry for transient failures (network error or 502/503/504).
 * Mobile tabs resuming from a long background period often reuse a stale/dead
 * pooled connection for the first request, which surfaces as one of these -
 * it isn't a real auth or server error, so it shouldn't be treated as one.
 */
async function fetchWithTransientRetry(
	url: string,
	options: RequestInit = {},
	fetchImpl: typeof fetch = fetch
): Promise<Response> {
	try {
		const res = await fetchImpl(url, options);
		if (!isTransientStatus(res.status)) return res;
		await new Promise((r) => setTimeout(r, 500));
		return await fetchImpl(url, options);
	} catch (err) {
		await new Promise((r) => setTimeout(r, 500));
		return await fetchImpl(url, options);
	}
}

/**
 * Schedule automatic refresh ~30s before expiry
 */
function scheduleRefresh(expiry: number) {
	if (refreshTimeout) clearTimeout(refreshTimeout);

	const now = Date.now();
	const msUntilExpiry = expiry - now;

	if (msUntilExpiry > 30_000) {
		refreshTimeout = setTimeout(async () => {
			const success = await refreshToken();
			if (!success && browser) {
				const { goto } = await import('$app/navigation');
				goto('/login');
			}
		}, msUntilExpiry - 30_000);
	}
}

/**
 * Update local state with new tokens
 */
function setAuth(token: string, expiryIso: string) {
	const expiry = new Date(expiryIso).getTime();
	accessToken.set(token);
	expiresAt.set(expiry);
	scheduleRefresh(expiry);
}

/**
 * Called on server-side load to bootstrap auth state
 */
export async function bootstrapAuth(fetchFn: typeof fetch) {
	if (!browser && process.env.NODE_ENV === 'development') {
		process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
	}

	const res = await fetchWithTransientRetry(
		`${getFullUrl('/api/auth/refresh')}`,
		{ method: 'POST', credentials: 'include' },
		fetchFn
	);

	if (!res.ok) {
		// Distinguish "no valid session" from "backend/proxy temporarily unreachable"
		// (e.g. a stale connection on the SSR fetch agent right after a mobile tab
		// reload) so the caller doesn't force a login redirect on a network blip.
		return { accessToken: null, expiresAt: null, transient: isTransientStatus(res.status) };
	}

	const data: AuthResponse = await res.json();
	setAuth(data.accessToken, data.expiresAt);

	return { ...data, transient: false };
}

/**
 * Signup with email/password/firstname
 */
export async function signup(email: string, password: string, firstname: string): Promise<void> {
	const res = await fetch(`${getFullUrl('/api/auth/signup')}`, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify({ email, password, firstname }),
		credentials: 'include'
	});

	if (!res.ok) throw new Error('Signup failed');

	const data: AuthResponse = await res.json();
	setAuth(data.accessToken, data.expiresAt);
}

/**
 * Login with email/password
 */
export async function login(email: string, password: string): Promise<void> {
	const res = await fetch(`${getFullUrl('/api/auth/login')}`, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify({ email, password }),
		credentials: 'include'
	});

	if (!res.ok) throw new Error('Login failed');

	const data: AuthResponse = await res.json();
	setAuth(data.accessToken, data.expiresAt);
}

/**
 * Refresh token using refresh cookie
 */
export async function refreshToken(): Promise<boolean> {
	const res = await fetchWithTransientRetry(`${getFullUrl('/api/auth/refresh')}`, {
		method: 'POST',
		credentials: 'include'
	});

	if (!res.ok) {
		// Only a real rejection from the server (e.g. refresh token expired/invalid)
		// should log the user out. A transient 502/503/504 that survived the retry
		// means the backend/proxy is temporarily unreachable - leave existing tokens
		// alone so the next attempt (timer or 401 retry) can succeed once it recovers.
		if (!isTransientStatus(res.status)) {
			accessToken.set(null);
			expiresAt.set(null);
		}
		return false;
	}

	const data: AuthResponse = await res.json();
	setAuth(data.accessToken, data.expiresAt);
	return true;
}

/**
 * Logout user
 */
export async function logout(): Promise<void> {
	// Get current token for the logout request
	let token: string | null = null;
	accessToken.subscribe((v) => (token = v))();

	// Call backend to invalidate refresh token session
	try {
		await fetch(`${getFullUrl('/api/auth/logout')}`, {
			method: 'POST',
			credentials: 'include',
			headers: {
				Authorization: token ? `Bearer ${token}` : ''
			}
		});
	} catch (error) {
		// Ignore errors - we'll clear local state anyway
		console.warn('Logout request failed, but clearing local state:', error);
	}

	// Clear local state
	accessToken.set(null);
	expiresAt.set(null);

	if (refreshTimeout) clearTimeout(refreshTimeout);
}

/**
 * Refresh the access token if it's missing or already expired/near expiry.
 * Mobile browsers freeze timers (including scheduleRefresh) while a tab is
 * backgrounded, so the proactive refresh never fires there - call this when
 * the tab regains visibility to catch up before any data fetch runs.
 */
export async function ensureFreshToken(): Promise<void> {
	let expiry: number | null = null;
	expiresAt.subscribe((v) => (expiry = v))();

	if (expiry === null) return;

	if (expiry <= Date.now() + 30_000) {
		await refreshToken();
	}
}

/**
 * Authenticated fetch wrapper
 */
export async function fetchWithAuth(url: string, options: RequestInit = {}): Promise<Response> {
	if (!browser && process.env.NODE_ENV === 'development') {
        process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
    }

	let token: string | null = null;
	accessToken.subscribe((v) => (token = v))();

	options.headers = {
		...(options.headers ?? {}),
		Authorization: token ? `Bearer ${token}` : ''
	};

	let res = await fetchWithTransientRetry(getFullUrl(url), options);

	if (res.status === 401) {
		// Try to refresh the token
		const refreshSuccess = await refreshToken();
		
		if (!refreshSuccess) {
			// Refresh failed, redirect to login
			if (browser) {
				const { goto } = await import('$app/navigation');
				goto('/login');
			}
			throw new Error('Authentication failed');
		}
		
		// Get the refreshed token
		let refreshedToken: string | null = null;
		accessToken.subscribe((v) => (refreshedToken = v))();
		
		// Retry with new token
		if (!options.headers) options.headers = {};
		(options.headers as Record<string, string>).Authorization = `Bearer ${refreshedToken}`;
		res = await fetchWithTransientRetry(getFullUrl(url), options);
		
		// If still 401 after refresh, redirect to login
		if (res.status === 401) {
			if (browser) {
				const { goto } = await import('$app/navigation');
				goto('/login');
			}
			throw new Error('Authentication failed');
		}
	}

	return res;
}

/**
 * Check if signup is enabled
 */
export async function checkSignupEnabled(): Promise<boolean> {
	try {
		const res = await fetch(`${getFullUrl('/api/auth/signup-enabled')}`);
		if (!res.ok) return false;
		const data = await res.json();
		return data.signupEnabled ?? false;
	} catch (error) {
		console.error('Failed to check signup status:', error);
		return false;
	}
}

/**
 * OIDC Provider interface
 */
export interface OidcProvider {
	providerId: string;
	displayName: string;
	faviconUrl?: string;
	autoRedirect: boolean;
}

/**
 * External Link interface
 */
export interface ExternalLink {
	id: string;
	providerId: string;
	providerDisplayName: string;
	providerEmail: string | null;
	linkedAt: string;
	lastLoginAt: string;
}

/**
 * Get list of enabled OIDC providers
 */
export async function getOidcProviders(): Promise<OidcProvider[]> {
	try {
		const res = await fetch(`${getFullUrl('/api/authoidc/providers')}`);
		if (!res.ok) return [];
		return await res.json();
	} catch (error) {
		console.error('Failed to load OIDC providers:', error);
		return [];
	}
}

/**
 * Initiate OIDC login by redirecting to provider
 */
export function initiateOidcLogin(providerId: string, returnUrl?: string): void {
	const url = returnUrl 
		? `${getFullUrl(`/api/authoidc/login/${providerId}`)}?returnUrl=${encodeURIComponent(returnUrl)}`
		: `${getFullUrl(`/api/authoidc/login/${providerId}`)}`;
	window.location.href = url;
}

/**
 * Get all external account links for the current user
 */
export async function getExternalLinks(): Promise<ExternalLink[]> {
	try {
		const res = await fetchWithAuth(`/api/authexternallinks`);
		if (!res.ok) return [];
		return await res.json();
	} catch (error) {
		console.error('Failed to load external links:', error);
		return [];
	}
}

/**
 * Initiate linking of an external OIDC account
 * Returns the authorization URL for the frontend to redirect to
 */
export async function linkExternalAccount(providerId: string): Promise<{ authorizationUrl: string } | null> {
	try {
		const res = await fetchWithAuth(`/api/authexternallinks/link/${providerId}`, {
			method: 'POST'
		});
		if (!res.ok) return null;
		return await res.json();
	} catch (error) {
		console.error('Failed to initiate linking:', error);
		return null;
	}
}

/**
 * Unlink an external account
 */
export async function unlinkExternalAccount(linkId: string): Promise<boolean> {
	try {
		const res = await fetchWithAuth(`/api/authexternallinks/${linkId}`, {
			method: 'DELETE'
		});
		return res.ok;
	} catch (error) {
		console.error('Failed to unlink account:', error);
		return false;
	}
}
