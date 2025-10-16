<script lang="ts">
	import { createPortal } from '$lib/utils/portal.js';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import IconButton from './IconButton.svelte';
	import type { Snippet } from 'svelte';

	let {
		buttonClass = '',
		menuClass = '',
		disabled = false,
		children
	}: {
		buttonClass?: string;
		menuClass?: string;
		disabled?: boolean;
		children: Snippet;
	} = $props();

	let isOpen = $state(false);
	let menuRef: HTMLDivElement;
	let buttonWrapperRef: HTMLDivElement;
	let menuContentRef: HTMLDivElement | undefined = $state();
	let menuPosition = $state({ top: 0, left: 0 });
	let positionAbove = $state(false);
	let portalTarget: HTMLElement | undefined = $state();

	function updateMenuPosition() {
		if (buttonWrapperRef && isOpen) {
			const rect = buttonWrapperRef.getBoundingClientRect();
			const viewportHeight = window.innerHeight;
			const menuHeight = menuContentRef?.offsetHeight || 200;
			
			// Calculate if there's space below, otherwise position above
			const spaceBelow = viewportHeight - rect.bottom;
			const shouldPositionAbove = spaceBelow < menuHeight + 10 && rect.top > menuHeight + 10;
			
			positionAbove = shouldPositionAbove;
			
			menuPosition = {
				top: shouldPositionAbove ? rect.top - menuHeight - 8 : rect.bottom + 8,
				left: rect.right - 200 // Align to right of button, assuming 200px menu width
			};
		}
	}

	function detectPortalTarget() {
		if (menuRef) {
			// Check if we're inside a dialog
			const dialogElement = menuRef.closest('dialog');
			portalTarget = dialogElement || document.body;
		}
	}

	function toggleMenu() {
		if (!disabled) {
			isOpen = !isOpen;
		}
	}

	function handleClickOutside(event: MouseEvent) {
		if (menuRef && !menuRef.contains(event.target as Node)) {
			isOpen = false;
		}
	}

	function handleKeydown(event: KeyboardEvent) {
		if (disabled) return;

		switch (event.key) {
			case 'Escape':
				isOpen = false;
				// Focus is handled by the IconButton component
				break;
		}
	}

	// Close menu when clicking outside and handle positioning updates
	$effect(() => {
		if (isOpen) {
			// Detect portal target and initial position calculation
			detectPortalTarget();
			// Use setTimeout to ensure the menu is rendered before calculating position
			setTimeout(() => {
				updateMenuPosition();
			}, 0);
			
			document.addEventListener('click', handleClickOutside);
			document.addEventListener('keydown', handleKeydown);
			window.addEventListener('scroll', updateMenuPosition, true);
			window.addEventListener('resize', updateMenuPosition);
			
			return () => {
				document.removeEventListener('click', handleClickOutside);
				document.removeEventListener('keydown', handleKeydown);
				window.removeEventListener('scroll', updateMenuPosition, true);
				window.removeEventListener('resize', updateMenuPosition);
			};
		}
	});

	// Export close function for child components
	export function closeMenu() {
		isOpen = false;
	}
</script>

<div 
	bind:this={menuRef}
	class="relative inline-block"
	class:opacity-50={disabled}
	class:pointer-events-none={disabled}
>
	<div bind:this={buttonWrapperRef} class="h-7 w-7 {buttonClass}">
		<IconButton 
			icon="fa-solid fa-ellipsis"
			tooltip="Open menu"
			hoverColor={ColorStyle.Primary}
			onclick={toggleMenu}
		/>
	</div>

	<!-- Portal the menu to body/dialog to escape overflow constraints -->
	{#if isOpen && portalTarget}
		<div 
			bind:this={menuContentRef}
			class="fixed z-[9999] min-w-[200px] rounded-lg border border-button bg-card p-2 shadow-lg {menuClass}" 
			class:animate-menu-slide-down={!positionAbove}
			class:animate-menu-slide-up={positionAbove}
			style="top: {menuPosition.top}px; left: {menuPosition.left}px;"
			role="menu"
			use:createPortal={portalTarget}
		>
			{@render children()}
		</div>
	{/if}
</div>

<style>
	@keyframes menuSlideDown {
		from {
			opacity: 0;
			transform: translateY(-8px);
		}
		to {
			opacity: 1;
			transform: translateY(0);
		}
	}

	@keyframes menuSlideUp {
		from {
			opacity: 0;
			transform: translateY(8px);
		}
		to {
			opacity: 1;
			transform: translateY(0);
		}
	}

	.animate-menu-slide-down {
		animation: menuSlideDown 0.15s ease-out;
	}

	.animate-menu-slide-up {
		animation: menuSlideUp 0.15s ease-out;
	}
</style>
