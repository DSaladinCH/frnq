import { browser } from '$app/environment';
import { writable } from 'svelte/store';

const baseUrl = import.meta.env.VITE_API_BASE_URL || '';

export interface AuthResponse {
	accessToken: string;
	expiresAt: string; // ISO string from API
}

export const accessToken = writable<string | null>(null);
export const expiresAt = writable<number | null>(null);

let refreshTimeout: ReturnType<typeof setTimeout> | undefined;

/**
 * Schedule automatic refresh ~30s before expiry
 */
function scheduleRefresh(expiry: number) {
	if (refreshTimeout) clearTimeout(refreshTimeout);

	const now = Date.now();
	const msUntilExpiry = expiry - now;

	if (msUntilExpiry > 30_000) {
		refreshTimeout = setTimeout(refreshToken, msUntilExpiry - 30_000);
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
	
	const res = await fetchFn(`${baseUrl}/api/auth/refresh`, {
		method: 'POST',
		credentials: 'include'
	});

	if (!res.ok) {
		return { accessToken: null, expiresAt: null };
	}

	const data: AuthResponse = await res.json();
	setAuth(data.accessToken, data.expiresAt);

	return data;
}

/**
 * Login with email/password
 */
export async function login(email: string, password: string): Promise<void> {
	const res = await fetch(`${baseUrl}/api/auth/login`, {
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
export async function refreshToken(): Promise<void> {
	const res = await fetch(`${baseUrl}/api/auth/refresh`, {
		method: 'POST',
		credentials: 'include'
	});

	if (!res.ok) {
		accessToken.set(null);
		expiresAt.set(null);
		return;
	}

	const data: AuthResponse = await res.json();
	setAuth(data.accessToken, data.expiresAt);
}

/**
 * Logout user
 */
export async function logout(): Promise<void> {
	await fetch(`${baseUrl}/api/auth/logout`, {
		method: 'POST',
		credentials: 'include'
	});

	accessToken.set(null);
	expiresAt.set(null);

	if (refreshTimeout) clearTimeout(refreshTimeout);
}

/**
 * Authenticated fetch wrapper
 */
export async function fetchWithAuth(url: string, options: RequestInit = {}): Promise<Response> {
	let token: string | null = null;
	accessToken.subscribe((v) => (token = v))();

	options.headers = {
		...(options.headers ?? {}),
		Authorization: token ? `Bearer ${token}` : ''
	};

	let res = await fetch(url, options);

	if (res.status === 401) {
		await refreshToken();
		accessToken.subscribe((v) => (token = v))();
		if (!options.headers) options.headers = {};
		(options.headers as Record<string, string>).Authorization = token ? `Bearer ${token}` : '';
		res = await fetch(url, options);
	}

	return res;
}
