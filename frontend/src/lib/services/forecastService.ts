import { fetchWithAuth } from './authService';

export interface ForecastBand {
	median: number;
	lower: number;
	upper: number;
}

export interface ForecastQuoteDto {
	quoteId: number;
	band: ForecastBand;
}

export interface ForecastGroupDto {
	groupId: number | null;
	band: ForecastBand;
}

export interface ForecastDayDto {
	date: string;
	portfolio: ForecastBand;
	groups: ForecastGroupDto[];
	quotes: ForecastQuoteDto[];
}

export async function getForecast(includeContributions: boolean = false, years: number = 1): Promise<ForecastDayDto[]> {
	const response = await fetchWithAuth(`api/forecast?includeContributions=${includeContributions}&years=${years}`);

	if (!response.ok) {
		throw new Error('Failed to fetch forecast');
	}

	const data = await response.json();

	// Handle both direct array and ApiResponse<T> format
	const forecasts = Array.isArray(data) ? data : data?.value || [];

	if (!Array.isArray(forecasts)) {
		throw new Error('Invalid forecast data format');
	}

	return forecasts;
}
