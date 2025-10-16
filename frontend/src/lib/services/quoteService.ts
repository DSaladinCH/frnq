import type { QuoteModel } from '$lib/Models/QuoteModel';
import { fetchWithAuth } from './authService';

const baseUrl = import.meta.env.VITE_API_BASE_URL;

export async function searchQuotes(query: string, providerId: string = 'yahoo-finance'): Promise<QuoteModel[]> {
	if (!query.trim()) return [];
	
	const params = new URLSearchParams({
		query: query.trim(),
		providerId
	});

	const url = `${baseUrl}/api/quote/search?${params.toString()}`;
	const res = await fetchWithAuth(url);

	if (!res.ok) throw new Error('Failed to search quotes');
	return res.json();
}

export async function updateQuoteCustomName(quoteId: number, customName: string): Promise<boolean> {
	const url = `${baseUrl}/api/quote/${quoteId}/customName`;
	console.log('Updating quote custom name:', url, customName);

	const res = await fetchWithAuth(url, {
		method: 'PUT',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify({ customName })
	});

	return res.ok;
}

export async function removeQuoteCustomName(quoteId: number): Promise<boolean> {
	const url = `${baseUrl}/api/quote/${quoteId}/customName`;

	const res = await fetchWithAuth(url, {
		method: 'DELETE'
	});

	return res.ok;
}