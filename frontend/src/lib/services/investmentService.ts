import { fetchWithAuth } from './authService';

// Helper to create a default InvestmentModel
export function createDefaultInvestment(): InvestmentModel {
	return {
		id: 0,
		quoteId: 0,
		providerId: undefined,
		quoteSymbol: undefined,
		type: InvestmentType.Buy,
		pricePerUnit: 0,
		amount: 0,
		totalFees: 0,
		date: new Date().toISOString().slice(0, 16)
	};
}

export interface InvestmentModel {
	id: number;
	quoteId: number;
	providerId?: string; // Optional: used when quote doesn't exist in DB yet
	quoteSymbol?: string; // Optional: used when quote doesn't exist in DB yet
	date: string;
	type: InvestmentType;
	amount: number;
	pricePerUnit: number;
	totalFees: number;
}

export interface PaginatedInvestmentsResponse {
	items: InvestmentModel[];
	totalCount: number;
	skip: number;
	take: number;
}

export enum InvestmentType {
	Buy = 'Buy',
	Sell = 'Sell',
	Dividend = 'Dividend'
}

type InvestmentRequest = Omit<InvestmentModel, 'provider'> & {
	providerId: string;
};

export interface InvestmentFilters {
	fromDate?: string;
	toDate?: string;
	quoteId?: number;
	groupId?: number;
	type?: InvestmentType;
}

export function investmentValuesValid(investment: InvestmentModel): boolean {
	const hasValidQuote =
		investment.quoteId > 0 ||
		(investment.providerId !== undefined &&
			investment.providerId !== '' &&
			investment.quoteSymbol !== undefined &&
			investment.quoteSymbol !== '');

	const validNumber =
		investment.type === InvestmentType.Dividend
			? investment.amount > 0
			: investment.pricePerUnit > 0 && investment.amount > 0;

	const hasValidDate = investment.date !== undefined && investment.date !== '';

	return hasValidQuote && validNumber && hasValidDate;
}

export async function getInvestments(
	skip: number = 0, 
	take: number = 25, 
	filters?: InvestmentFilters
): Promise<PaginatedInvestmentsResponse> {
	let url = `/api/investments?skip=${skip}&take=${take}`;
	
	if (filters) {
		if (filters.fromDate) url += `&fromDate=${filters.fromDate}`;
		if (filters.toDate) url += `&toDate=${filters.toDate}`;
		if (filters.quoteId) url += `&quoteId=${filters.quoteId}`;
		if (filters.groupId) url += `&groupId=${filters.groupId}`;
		if (filters.type !== undefined) url += `&type=${filters.type}`;
	}
	
	const res = await fetchWithAuth(url);

	if (!res.ok) throw new Error('Failed to fetch investments');
	return res.json();
}

export async function addInvestment(investment: InvestmentModel): Promise<void> {
	const providerId = 'yahoo-finance';
	const request: InvestmentRequest = { ...investment, providerId };

	const res = await fetchWithAuth('/api/investments', {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(request)
	});

	if (!res.ok) throw new Error('Failed to add investment');
	return;
}

export async function addInvestmentsBulk(investments: InvestmentModel[]): Promise<InvestmentModel[]> {
	const providerId = 'yahoo-finance';
	const requests: InvestmentRequest[] = investments.map(investment => ({ ...investment, providerId }));

	const res = await fetchWithAuth('/api/investments/bulk', {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(requests)
	});

	if (!res.ok) throw new Error('Failed to add investments');
	return res.json();
}

export async function updateInvestment(investment: InvestmentModel): Promise<InvestmentModel> {
	investment.quoteId = 0; // ensure quoteId is not sent in the payload

	const res = await fetchWithAuth(`/api/investments/${investment.id}`, {
		method: 'PUT',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(investment)
	});

	if (!res.ok) throw new Error('Failed to update investment');
	return res.json();
}

export async function deleteInvestment(investmentId: number): Promise<void> {
	const res = await fetchWithAuth(`/api/investments/${investmentId}`, {
		method: 'DELETE'
	});

	if (!res.ok) throw new Error('Failed to delete investment');
	return;
}
