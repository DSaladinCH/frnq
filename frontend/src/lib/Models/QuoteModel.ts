import type { QuoteGroup } from "./QuoteGroup";

export interface QuoteModel {
  id: number;
  name: string;
  symbol: string;
  providerId: string;
  exchangeDisposition: string;
  typeDisposition: string;
  currency: string;
  lastUpdatedPrices: string;
  group: QuoteGroup;
  customName?: string;
}