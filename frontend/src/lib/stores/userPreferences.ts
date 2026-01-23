import { writable } from 'svelte/store';
import { fetchWithAuth } from '$lib/services/authService';
import { DateFormatType } from '$lib/utils/dateFormat';

export { DateFormatType };

export interface UserPreferences {
	dateFormat: DateFormatType;
}

// Default preferences
const defaultPreferences: UserPreferences = {
	dateFormat: DateFormatType.English
};

// Create the store
function createUserPreferencesStore() {
	const { subscribe, set, update } = writable<UserPreferences>(defaultPreferences);

	return {
		subscribe,
		
		/**
		 * Load user preferences from the backend
		 */
		async load(): Promise<void> {
			try {
				const res = await fetchWithAuth('/api/auth/me');
				if (res.ok) {
					const userData = await res.json();
					set({
						dateFormat: userData.dateFormat || DateFormatType.English
					});
				}
			} catch (error) {
				console.error('Failed to load user preferences:', error);
			}
		},

		/**
		 * Update the date format preference
		 */
		async setDateFormat(format: DateFormatType): Promise<boolean> {
			try {
				const res = await fetchWithAuth('/api/auth/me', {
					method: 'PUT',
					headers: { 'Content-Type': 'application/json' },
					body: JSON.stringify({ dateFormat: format })
				});

				if (res.ok) {
					update(prefs => ({ ...prefs, dateFormat: format }));
					return true;
				}
				return false;
			} catch (error) {
				console.error('Failed to update date format:', error);
				return false;
			}
		},

		/**
		 * Reset to default preferences
		 */
		reset(): void {
			set(defaultPreferences);
		}
	};
}

export const userPreferences = createUserPreferencesStore();
