import { fetchWithAuth } from './authService';

export interface ForecastDto {
	date: string;
	median: number;
	lower: number;
	upper: number;
}

export async function getForecast(): Promise<ForecastDto[]> {
	const response = await fetchWithAuth('api/forecast');

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
