export enum NumberFormatType {
	English = 'English',
	German = 'German',
	Swiss = 'Swiss'
}

const LOCALE_MAP: Record<NumberFormatType, string> = {
	[NumberFormatType.English]: 'en-US',
	[NumberFormatType.German]: 'de-DE',
	[NumberFormatType.Swiss]: 'de-CH'
};

function resolveLocale(format: NumberFormatType): string {
	return LOCALE_MAP[format] ?? LOCALE_MAP[NumberFormatType.English];
}

/**
 * Format a number according to the user's preference
 * @param value - Number to format
 * @param format - User's preferred number format
 * @param options - Intl.NumberFormat options
 */
export function formatNumber(
	value: number,
	format: NumberFormatType = NumberFormatType.English,
	options: Intl.NumberFormatOptions = {}
): string {
	return value.toLocaleString(resolveLocale(format), options);
}

/**
 * Format a number as a currency value according to the user's preference
 * @param value - Number to format
 * @param currency - ISO 4217 currency code (e.g. 'CHF', 'USD')
 * @param format - User's preferred number format
 * @param options - Additional Intl.NumberFormat options
 */
export function formatCurrency(
	value: number,
	currency: string = 'CHF',
	format: NumberFormatType = NumberFormatType.English,
	options: Intl.NumberFormatOptions = {}
): string {
	return value.toLocaleString(resolveLocale(format), {
		style: 'currency',
		currency,
		...options
	});
}
