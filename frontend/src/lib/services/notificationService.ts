/**
 * Notification Service
 * 
 * A simple, easy-to-use notification service that can be called from anywhere in the application.
 * 
 * @example
 * ```ts
 * import { notify } from '$lib/services/notificationService';
 * 
 * // Simple usage
 * notify.success('Operation completed!');
 * notify.error('Something went wrong!');
 * notify.warning('Please be careful!');
 * notify.info('Here is some information');
 * 
 * // With custom duration (in milliseconds)
 * notify.success('This will disappear in 10 seconds', 10000);
 * 
 * // Persistent notification (won't auto-dismiss)
 * notify.error('Critical error - please address', 0);
 * 
 * // Configure globally
 * notify.setPosition('top-left'); // Change notification position
 * notify.setMaxNotifications(3); // Show max 3 notifications at once
 * ```
 */

export { notificationStore as notify } from '$lib/stores/notificationStore.svelte';
