// src/lib/services/positionService.ts
const baseUrl = import.meta.env.VITE_API_BASE_URL;

export interface InvestmentModel {
  id: number;
  quoteId: number;
  date: string;
  type: InvestmentType;
  amount: number;
  pricePerUnit: number;
  totalFees: number;
}

export enum InvestmentType {
  Buy = 0,
  Sell = 1,
  Dividend = 2,
}

export async function getInvestments(): Promise<InvestmentModel[]> {
  const url = `${baseUrl}/api/investments`;
  const res = await fetch(url);

  if (!res.ok) throw new Error('Failed to fetch investments');
  return res.json();
}