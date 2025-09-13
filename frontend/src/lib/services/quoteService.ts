import type { QuoteModel } from '$lib/Models/QuoteModel';

const baseUrl = import.meta.env.VITE_API_BASE_URL;

export async function searchQuotes(query: string, providerId: string = 'yahoo-finance'): Promise<QuoteModel[]> {
	if (!query.trim()) return [];
	
	const params = new URLSearchParams({
		query: query.trim(),
		providerId
	});

	const url = `${baseUrl}/api/quote/search?${params.toString()}`;
	const res = await fetch(url);

	if (!res.ok) throw new Error('Failed to search quotes');
	return res.json();
}