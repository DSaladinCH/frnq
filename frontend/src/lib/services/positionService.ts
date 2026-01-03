import type { QuoteModel } from '$lib/Models/QuoteModel';
import { fetchWithAuth } from './authService';

export interface PositionSnapshot {
	userId: string;
	quoteId: number;
	date: string;
	currency: string;
	group: string;
	amount: number;
	invested: number;
	marketPricePerUnit: number;
	totalFees: number;
	currentValue: number;
	realizedGain: number;
	unrealizedGain: number;
	totalProfit: number;
	totalInvestedCash: number;
}

export interface PositionsResponse {
	snapshots: PositionSnapshot[];
	quotes: QuoteModel[];
}

export async function getPositionSnapshots(
	from: string | null,
	to: string | null
): Promise<PositionsResponse> {
	const params = new URLSearchParams();
	if (from) params.append('from', from);
	if (to) params.append('to', to);

	const res = await fetchWithAuth(`/api/positions${params.toString() ? `?${params.toString()}` : ''}`);

	if (!res.ok) throw new Error('Failed to fetch position snapshots');
	return res.json();
}
