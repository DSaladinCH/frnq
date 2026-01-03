import type { QuoteModel } from '$lib/Models/QuoteModel';
import { fetchWithAuth } from './authService';

export async function searchQuotes(query: string, providerId: string = 'yahoo-finance'): Promise<QuoteModel[]> {
	if (!query.trim()) return [];
	
	const params = new URLSearchParams({
		query: query.trim(),
		providerId
	});

	const res = await fetchWithAuth(`/api/quote/search?${params.toString()}`);

	if (!res.ok) throw new Error('Failed to search quotes');
	return res.json();
}

export async function updateQuoteCustomName(quoteId: number, customName: string): Promise<boolean> {
	const res = await fetchWithAuth(`/api/quote/${quoteId}/customName`, {
		method: 'PUT',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify({ customName })
	});

	return res.ok;
}

export async function removeQuoteCustomName(quoteId: number): Promise<boolean> {
	const res = await fetchWithAuth(`/api/quote/${quoteId}/customName`, {
		method: 'DELETE'
	});

	return res.ok;
}