<script lang="ts">
	import { notificationStore, type Notification, type NotificationType } from '$lib/stores/notificationStore.svelte';
	import { createPortal } from '$lib/utils/portal';
	import { fly, fade } from 'svelte/transition';
	import { browser } from '$app/environment';

	let containerRef: HTMLDivElement;
	let portalTarget: HTMLElement | undefined = $state(browser ? document.body : undefined);
	
	const notifications = $derived(notificationStore.items);
	const position = $derived(notificationStore.position);
	
	// Detect if we're inside a modal/dialog
	$effect(() => {
		if (browser && containerRef) {
			const dialogElement = containerRef.closest('dialog');
			portalTarget = dialogElement || document.body;
		}
	});
	
	function getIcon(type: NotificationType): string {
		switch (type) {
			case 'success':
				return 'fa-circle-check';
			case 'error':
				return 'fa-circle-xmark';
			case 'warning':
				return 'fa-triangle-exclamation';
			case 'info':
			default:
				return 'fa-circle-info';
		}
	}
	
	function getColorClass(type: NotificationType): string {
		switch (type) {
			case 'success':
				return 'notification-success';
			case 'error':
				return 'notification-error';
			case 'warning':
				return 'notification-warning';
			case 'info':
			default:
				return 'notification-info';
		}
	}
	
	function getTransitionParams() {
		// Determine transition direction based on position
		if (position.includes('left')) {
			return { x: -300, duration: 300, opacity: 0 };
		} else if (position.includes('center')) {
			return { y: position.includes('top') ? -50 : 50, duration: 300, opacity: 0 };
		} else {
			// right or default
			return { x: 300, duration: 300, opacity: 0 };
		}
	}
	
	function handleDismiss(id: string) {
		notificationStore.dismiss(id);
	}
</script>

<!-- Hidden reference element for portal detection -->
<div bind:this={containerRef} style="display: none;"></div>

{#if portalTarget}
	<div 
		class="notification-container notification-{position}"
		use:createPortal={portalTarget}
	>
		{#each notifications as notification (notification.id)}
			{@const transitionParams = getTransitionParams()}
			<div 
				class="notification {getColorClass(notification.type)}"
				role="alert"
				aria-live="polite"
				in:fly={transitionParams}
				out:fly={{ ...transitionParams, duration: 250 }}
			>
				<div class="notification-icon">
					<i class="fa-solid {getIcon(notification.type)}"></i>
				</div>
				<div class="notification-content">
					<p class="notification-message">{notification.message}</p>
				</div>
				<button 
					class="notification-close"
					onclick={() => handleDismiss(notification.id)}
					aria-label="Close notification"
				>
					<i class="fa-solid fa-xmark"></i>
				</button>
			</div>
		{/each}
	</div>
{/if}

<style>
	.notification-container {
		position: fixed;
		z-index: 9999;
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
		padding: 1rem;
		pointer-events: none;
		max-width: 100%;
		box-sizing: border-box;
	}
	
	/* Position variants */
	.notification-top-right {
		top: 0;
		right: 0;
		align-items: flex-end;
	}
	
	.notification-top-left {
		top: 0;
		left: 0;
		align-items: flex-start;
	}
	
	.notification-top-center {
		top: 0;
		left: 50%;
		transform: translateX(-50%);
		align-items: center;
	}
	
	.notification-bottom-right {
		bottom: 0;
		right: 0;
		align-items: flex-end;
		flex-direction: column-reverse;
	}
	
	.notification-bottom-left {
		bottom: 0;
		left: 0;
		align-items: flex-start;
		flex-direction: column-reverse;
	}
	
	.notification-bottom-center {
		bottom: 0;
		left: 50%;
		transform: translateX(-50%);
		align-items: center;
		flex-direction: column-reverse;
	}
	
	.notification {
		display: flex;
		align-items: center;
		gap: 0.875rem;
		min-width: 320px;
		max-width: 500px;
		padding: 1rem 1.25rem;
		background-color: var(--color-card);
		border-radius: 0.75rem;
		box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4), 0 2px 6px rgba(0, 0, 0, 0.2);
		pointer-events: auto;
		border-left: 4px solid;
		border-top-left-radius: 0;
		border-bottom-left-radius: 0;
		transition: all 0.2s ease;
	}
	
	.notification:hover {
		box-shadow: 0 6px 16px rgba(0, 0, 0, 0.5), 0 3px 8px rgba(0, 0, 0, 0.3);
		transform: translateY(-2px);
	}
	
	/* Mobile responsive */
	@media (max-width: 640px) {
		.notification-container {
			padding: 0.5rem;
			left: 0 !important;
			right: 0 !important;
			transform: none !important;
			align-items: stretch !important;
		}
		
		.notification {
			min-width: auto;
			width: 100%;
			max-width: none;
		}
	}
	
	/* Type-specific colors */
	.notification-info {
		border-left-color: #3b82f6;
	}
	
	.notification-success {
		border-left-color: var(--color-success);
	}
	
	.notification-warning {
		border-left-color: #f59e0b;
	}
	
	.notification-error {
		border-left-color: var(--color-error);
	}
	
	.notification-icon {
		flex-shrink: 0;
		font-size: 1.5rem;
		line-height: 1;
		display: flex;
		align-items: center;
		justify-content: center;
	}
	
	.notification-info .notification-icon {
		color: #3b82f6;
	}
	
	.notification-success .notification-icon {
		color: var(--color-success);
	}
	
	.notification-warning .notification-icon {
		color: #f59e0b;
	}
	
	.notification-error .notification-icon {
		color: var(--color-error);
	}
	
	.notification-content {
		flex: 1;
		min-width: 0;
	}
	
	.notification-message {
		margin: 0;
		color: var(--color-text);
		font-size: 0.95rem;
		line-height: 1.5;
		word-wrap: break-word;
	}
	
	.notification-close {
		flex-shrink: 0;
		background: none;
		border: none;
		color: var(--color-muted);
		cursor: pointer;
		padding: 0.25rem;
		font-size: 1.25rem;
		line-height: 1;
		display: flex;
		align-items: center;
		justify-content: center;
		border-radius: 0.25rem;
		transition: all 0.15s ease;
		width: 28px;
		height: 28px;
	}
	
	.notification-close:hover {
		color: var(--color-text);
		background-color: rgba(255, 255, 255, 0.1);
	}
	
	.notification-close:active {
		transform: scale(0.9);
	}
	
	/* Respect reduced motion preferences */
	@media (prefers-reduced-motion: reduce) {
		.notification {
			transition: none;
		}
	}
</style>
