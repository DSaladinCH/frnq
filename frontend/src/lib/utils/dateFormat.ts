export type DateFormatType = 'english' | 'german';

export interface DateFormatOptions {
	includeTime?: boolean;
	includeSeconds?: boolean;
}

/**
 * Format a date string or Date object according to the user's preference
 * @param date - Date string or Date object to format
 * @param format - User's preferred date format ('english' or 'german')
 * @param options - Optional formatting options
 * @returns Formatted date string
 */
export function formatDate(
	date: string | Date,
	format: DateFormatType = 'english',
	options: DateFormatOptions = {}
): string {
	const dateObj = typeof date === 'string' ? new Date(date) : date;

	if (isNaN(dateObj.getTime())) {
		return 'Invalid date';
	}

	const { includeTime = false, includeSeconds = false } = options;

	let formattedDate = '';
	
	if (format === 'german') {
		// German format: DD.MM.YYYY
		const day = dateObj.getDate().toString().padStart(2, '0');
		const month = (dateObj.getMonth() + 1).toString().padStart(2, '0');
		const year = dateObj.getFullYear();
		formattedDate = `${day}.${month}.${year}`;
	} else {
		// English format: MM/DD/YYYY
		const day = dateObj.getDate().toString().padStart(2, '0');
		const month = (dateObj.getMonth() + 1).toString().padStart(2, '0');
		const year = dateObj.getFullYear();
		formattedDate = `${month}/${day}/${year}`;
	}

	if (includeTime) {
		if (format === 'german') {
			// German: 24-hour format
			const hours = dateObj.getHours().toString().padStart(2, '0');
			const minutes = dateObj.getMinutes().toString().padStart(2, '0');
			let timeStr = `${hours}:${minutes}`;
			
			if (includeSeconds) {
				const seconds = dateObj.getSeconds().toString().padStart(2, '0');
				timeStr += `:${seconds}`;
			}
			
			formattedDate += ` ${timeStr}`;
		} else {
			// English: 12-hour format with AM/PM
			let hours = dateObj.getHours();
			const minutes = dateObj.getMinutes().toString().padStart(2, '0');
			const ampm = hours >= 12 ? 'PM' : 'AM';
			
			hours = hours % 12;
			hours = hours ? hours : 12; // Convert 0 to 12
			const hoursStr = hours.toString();
			
			let timeStr = `${hoursStr}:${minutes}`;
			
			if (includeSeconds) {
				const seconds = dateObj.getSeconds().toString().padStart(2, '0');
				timeStr += `:${seconds}`;
			}
			
			timeStr += ` ${ampm}`;
			formattedDate += ` ${timeStr}`;
		}
	}

	return formattedDate;
}

/**
 * Format a date with time (convenience function)
 */
export function formatDateTime(
	date: string | Date,
	format: DateFormatType = 'english'
): string {
	return formatDate(date, format, { includeTime: true });
}

/**
 * Format just the time portion
 * @param date - Date string or Date object to format
 * @param format - User's preferred date format ('english' for 12-hour, 'german' for 24-hour)
 * @param includeSeconds - Whether to include seconds
 */
export function formatTime(
	date: string | Date,
	format: DateFormatType = 'english',
	includeSeconds: boolean = false
): string {
	const dateObj = typeof date === 'string' ? new Date(date) : date;

	if (isNaN(dateObj.getTime())) {
		return 'Invalid time';
	}

	if (format === 'german') {
		// German: 24-hour format
		const hours = dateObj.getHours().toString().padStart(2, '0');
		const minutes = dateObj.getMinutes().toString().padStart(2, '0');
		let timeStr = `${hours}:${minutes}`;

		if (includeSeconds) {
			const seconds = dateObj.getSeconds().toString().padStart(2, '0');
			timeStr += `:${seconds}`;
		}

		return timeStr;
	} else {
		// English: 12-hour format with AM/PM
		let hours = dateObj.getHours();
		const minutes = dateObj.getMinutes().toString().padStart(2, '0');
		const ampm = hours >= 12 ? 'PM' : 'AM';
		
		hours = hours % 12;
		hours = hours ? hours : 12; // Convert 0 to 12
		const hoursStr = hours.toString();
		
		let timeStr = `${hoursStr}:${minutes}`;
		
		if (includeSeconds) {
			const seconds = dateObj.getSeconds().toString().padStart(2, '0');
			timeStr += `:${seconds}`;
		}
		
		return `${timeStr} ${ampm}`;
	}
}
