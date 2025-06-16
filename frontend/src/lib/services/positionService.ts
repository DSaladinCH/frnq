// src/lib/services/positionService.ts
const baseUrl = import.meta.env.VITE_API_BASE_URL;

export interface PositionSnapshot {
  userId: string;
  providerId: string;
  quoteSymbol: string;
  date: string;
  currency: string;
  amount: number;
  invested: number;
  marketPricePerUnit: number;
  totalFees: number;
  totalValue: number;
  unrealizedGain: number;
  realizedGain: number;
}

export async function getPositionSnapshots(from: string, to: string): Promise<PositionSnapshot[]> {
  const url = `${baseUrl}/api/positions?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`;
  const res = await fetch(url);
  if (!res.ok) throw new Error('Failed to fetch position snapshots');
  return res.json();
}