/**
 * Notification Store
 * Global store for managing notification messages throughout the application
 */

export type NotificationType = 'info' | 'success' | 'warning' | 'error';

export interface Notification {
	id: string;
	type: NotificationType;
	message: string;
	duration: number;
	timestamp: number;
}

export interface NotificationOptions {
	type?: NotificationType;
	duration?: number; // in milliseconds, 0 means no auto-dismiss
}

class NotificationStore {
	private notifications = $state<Notification[]>([]);
	private maxNotifications = 5;
	
	// Global position configuration (can be changed globally)
	// Options: 'top-right' | 'top-left' | 'top-center' | 'bottom-right' | 'bottom-left' | 'bottom-center'
	public position = $state<'top-right' | 'top-left' | 'top-center' | 'bottom-right' | 'bottom-left' | 'bottom-center'>('top-right');
	
	get items() {
		return this.notifications;
	}
	
	get max() {
		return this.maxNotifications;
	}
	
	/**
	 * Set the maximum number of notifications to display at once
	 */
	setMaxNotifications(max: number) {
		this.maxNotifications = max;
		// Remove oldest notifications if we exceed the limit
		if (this.notifications.length > max) {
			this.notifications = this.notifications.slice(-max);
		}
	}
	
	/**
	 * Set the global position for notifications
	 */
	setPosition(pos: 'top-right' | 'top-left' | 'top-center' | 'bottom-right' | 'bottom-left' | 'bottom-center') {
		this.position = pos;
	}
	
	/**
	 * Show a notification
	 */
	show(message: string, options: NotificationOptions = {}) {
		const notification: Notification = {
			id: crypto.randomUUID(),
			type: options.type || 'info',
			message,
			duration: options.duration !== undefined ? options.duration : 5000,
			timestamp: Date.now()
		};
		
		// Add to the end of the array
		this.notifications = [...this.notifications, notification];
		
		// Remove oldest if exceeding max
		if (this.notifications.length > this.maxNotifications) {
			this.notifications = this.notifications.slice(-this.maxNotifications);
		}
		
		// Auto-dismiss if duration > 0
		if (notification.duration > 0) {
			setTimeout(() => {
				this.dismiss(notification.id);
			}, notification.duration);
		}
		
		return notification.id;
	}
	
	/**
	 * Show an info notification
	 */
	info(message: string, duration?: number) {
		return this.show(message, { type: 'info', duration });
	}
	
	/**
	 * Show a success notification
	 */
	success(message: string, duration?: number) {
		return this.show(message, { type: 'success', duration });
	}
	
	/**
	 * Show a warning notification
	 */
	warning(message: string, duration?: number) {
		return this.show(message, { type: 'warning', duration });
	}
	
	/**
	 * Show an error notification
	 */
	error(message: string, duration?: number) {
		return this.show(message, { type: 'error', duration });
	}
	
	/**
	 * Dismiss a specific notification
	 */
	dismiss(id: string) {
		this.notifications = this.notifications.filter(n => n.id !== id);
	}
	
	/**
	 * Clear all notifications
	 */
	clear() {
		this.notifications = [];
	}
}

// Export singleton instance
export const notificationStore = new NotificationStore();
