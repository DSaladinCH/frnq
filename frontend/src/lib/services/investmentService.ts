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
// src/lib/services/positionService.ts
const baseUrl = import.meta.env.VITE_API_BASE_URL;

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

export enum InvestmentType {
	Buy = 0,
	Sell = 1,
	Dividend = 2
}

type InvestmentRequest = Omit<InvestmentModel, 'provider'> & {
	providerId: string;
};

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

export async function getInvestments(): Promise<InvestmentModel[]> {
	const startTime = Date.now();

	const url = `${baseUrl}/api/investments`;
	const res = await fetchWithAuth(url);

	// wait at least 1 second, to improve UX
	await new Promise((resolve) => setTimeout(resolve, Math.max(0, 1000 - (Date.now() - startTime))));

	if (!res.ok) throw new Error('Failed to fetch investments');
	return res.json();
}

export async function addInvestment(investment: InvestmentModel): Promise<void> {
	const startTime = Date.now();

	const providerId = 'yahoo-finance';
	const request: InvestmentRequest = { ...investment, providerId };

	const url = `${baseUrl}/api/investments`;
	const res = await fetchWithAuth(url, {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(request)
	});

	// wait at least 1 second, to improve UX
	await new Promise((resolve) => setTimeout(resolve, Math.max(0, 1000 - (Date.now() - startTime))));

	if (!res.ok) throw new Error('Failed to add investment');
	return;
}

export async function addInvestmentsBulk(investments: InvestmentModel[]): Promise<InvestmentModel[]> {
	const startTime = Date.now();

	const providerId = 'yahoo-finance';
	const requests: InvestmentRequest[] = investments.map(investment => ({ ...investment, providerId }));

	const url = `${baseUrl}/api/investments/bulk`;
	const res = await fetchWithAuth(url, {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(requests)
	});

	// wait at least 1 second, to improve UX
	await new Promise((resolve) => setTimeout(resolve, Math.max(0, 1000 - (Date.now() - startTime))));

	if (!res.ok) throw new Error('Failed to add investments');
	return res.json();
}

export async function updateInvestment(investment: InvestmentModel): Promise<InvestmentModel> {
	const startTime = Date.now();

	investment.quoteId = 0; // ensure quoteId is not sent in the payload
	console.log('Updating investment:', investment);

	const url = `${baseUrl}/api/investments/${investment.id}`;
	const res = await fetchWithAuth(url, {
		method: 'PUT',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(investment)
	});

	// wait at least 1 second, to improve UX
	await new Promise((resolve) => setTimeout(resolve, Math.max(0, 1000 - (Date.now() - startTime))));

	if (!res.ok) throw new Error('Failed to update investment');
	return res.json();
}

export async function deleteInvestment(investmentId: number): Promise<void> {
	const startTime = Date.now();

	const url = `${baseUrl}/api/investments/${investmentId}`;
	const res = await fetchWithAuth(url, {
		method: 'DELETE'
	});

	// wait at least 1 second, to improve UX
	await new Promise((resolve) => setTimeout(resolve, Math.max(0, 1000 - (Date.now() - startTime))));

	if (!res.ok) throw new Error('Failed to delete investment');
	return;
}
