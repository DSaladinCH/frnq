// src/lib/services/positionService.ts
const baseUrl = import.meta.env.VITE_API_BASE_URL;

export interface PositionSnapshot {
  userId: string;
  providerId: string;
  quoteSymbol: string;
  date: string;
  currency: string;
  group: string;
  amount: number;
  invested: number;
  marketPricePerUnit: number;
  totalFees: number;
  totalValue: number;
  unrealizedGain: number;
  realizedGain: number;
}

export async function getPositionSnapshots(from: string | null, to: string | null): Promise<PositionSnapshot[]> {
  const params = new URLSearchParams();
  if (from) params.append('from', from);
  if (to) params.append('to', to);

  const url = `${baseUrl}/api/positions${params.toString() ? `?${params.toString()}` : ''}`;
  const res = await fetch(url);
  
  if (!res.ok) throw new Error('Failed to fetch position snapshots');
  return res.json();
}