<script lang="ts">
	import { notificationStore, type Notification, type NotificationType } from '$lib/stores/notificationStore.svelte';
	import { createPortal } from '$lib/utils/portal';
	import { fly, fade } from 'svelte/transition';
	import { browser } from '$app/environment';

	let containerRef: HTMLDivElement;
	let portalTarget: HTMLElement | undefined = $state(undefined);
	
	const notifications = $derived(notificationStore.items);
	const position = $derived(notificationStore.position);
	
	// Type mappings
	const ICON_MAP: Record<NotificationType, string> = {
		info: 'fa-circle-info',
		success: 'fa-circle-check',
		warning: 'fa-triangle-exclamation',
		error: 'fa-circle-xmark'
	};
	
	const BORDER_COLOR_MAP: Record<NotificationType, string> = {
		info: 'border-primary',
		success: 'border-success',
		warning: 'border-warning',
		error: 'border-error'
	};
	
	const ICON_COLOR_MAP: Record<NotificationType, string> = {
		info: 'color-primary',
		success: 'color-success',
		warning: 'color-warning',
		error: 'color-error'
	};
	
	// Position mappings
	const POSITION_CLASS_MAP = {
		'top-right': 'top-0 right-0 items-end',
		'top-left': 'top-0 left-0 items-start',
		'top-center': 'top-0 left-1/2 -translate-x-1/2 items-center',
		'bottom-right': 'bottom-0 right-0 items-end flex-col-reverse',
		'bottom-left': 'bottom-0 left-0 items-start flex-col-reverse',
		'bottom-center': 'bottom-0 left-1/2 -translate-x-1/2 items-center flex-col-reverse'
	} as const;
	
	const TRANSITION_PARAMS_MAP = {
		left: { x: -300, duration: 300, opacity: 0 },
		center: { y: -50, duration: 300, opacity: 0 }, // will be adjusted for bottom
		right: { x: 300, duration: 300, opacity: 0 }
	};
	
	function resolvePortalTarget() {
		if (!browser) return undefined;

		const openDialogs = Array.from(document.querySelectorAll('dialog[open]')) as HTMLDialogElement[];
		const topMostDialog = openDialogs.at(-1);

		return topMostDialog ?? document.body;
	}

	// Always keep notifications in the top-most layer (dialog when present, body otherwise)
	$effect(() => {
		if (!browser) return;

		const updateTarget = () => {
			const nextTarget = resolvePortalTarget();
			if (nextTarget && nextTarget !== portalTarget) {
				portalTarget = nextTarget;
			}
		};

		updateTarget();

		const observer = new MutationObserver(updateTarget);
		observer.observe(document.body, {
			subtree: true,
			attributes: true,
			childList: true,
			attributeFilter: ['open']
		});

		const handleFocus = () => updateTarget();
		window.addEventListener('focusin', handleFocus, true);

		return () => {
			observer.disconnect();
			window.removeEventListener('focusin', handleFocus, true);
		};
	});
	
	function getTransitionParams() {
		// Determine transition direction based on position
		if (position.includes('left')) {
			return TRANSITION_PARAMS_MAP.left;
		} else if (position.includes('center')) {
			return { 
				...TRANSITION_PARAMS_MAP.center, 
				y: position.includes('bottom') ? 50 : -50 
			};
		} else {
			return TRANSITION_PARAMS_MAP.right;
		}
	}
	
	function handleDismiss(id: string) {
		notificationStore.dismiss(id);
	}
</script>

<!-- Hidden reference element for portal detection -->
<div bind:this={containerRef} class="hidden"></div>

{#if portalTarget}
	<div 
		class="fixed z-[9999] flex flex-col gap-3 p-4 pointer-events-none max-w-full box-border
			{POSITION_CLASS_MAP[position]}
			max-md:p-2 max-md:left-0 max-md:right-0 max-md:!translate-x-0 max-md:items-stretch"
		use:createPortal={portalTarget}
	>
		{#each notifications as notification (notification.id)}
			{@const transitionParams = getTransitionParams()}
			<div 
				class="notification flex items-center gap-3.5 min-w-[320px] max-w-[500px] p-4 px-5 bg-card 
					rounded-xl pointer-events-auto border-l-4 rounded-tl-sm rounded-bl-sm
					max-md:min-w-0 max-md:w-full max-md:max-w-none
					{BORDER_COLOR_MAP[notification.type]}"
				role="alert"
				aria-live="polite"
				in:fly={transitionParams}
				out:fly={{ ...transitionParams, duration: 250 }}
			>
				<div class="notification-icon shrink-0 text-2xl leading-none flex items-center justify-center
					{ICON_COLOR_MAP[notification.type]}">
					<i class="fa-solid {ICON_MAP[notification.type]}"></i>
				</div>
				<div class="flex-1 min-w-0">
					<p class="m-0 color-default text-[0.95rem] leading-6 break-words">{notification.message}</p>
				</div>
				<button 
					class="notification-close shrink-0 bg-transparent border-none color-muted cursor-pointer p-1 
						text-xl leading-none flex items-center justify-center rounded w-7 h-7"
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
	/* Notification base styles with complex shadows and transitions */
	.notification {
		box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4), 0 2px 6px rgba(0, 0, 0, 0.2);
		transition: all 0.2s ease-in-out;
	}
	
	.notification:hover {
		box-shadow: 0 6px 16px rgba(0, 0, 0, 0.5), 0 3px 8px rgba(0, 0, 0, 0.3);
		transform: translateY(-2px);
	}
	
	/* Close button transitions */
	.notification-close {
		transition: all 0.15s ease-in-out;
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
		.notification,
		.notification-close {
			transition: none;
		}
		
		.notification:hover {
			transform: none;
		}
		
		.notification-close:active {
			transform: none;
		}
	}
</style>
