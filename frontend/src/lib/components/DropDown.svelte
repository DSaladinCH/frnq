<script lang="ts">
	import { createPortal } from '$lib/utils/portal.js';
	import type { Snippet } from 'svelte';

	interface Option {
		value: string;
		label: string;
	}

	let {
		options = [],
		value = '',
		title = '',
		placeholder = 'Select an option...',
		disabled = false,
		class: className = '',
		isLoading = false,
		onchange
	}: {
		options: Option[];
		value?: string;
		title?: string | Snippet;
		placeholder?: string;
		disabled?: boolean;
		class?: string;
		isLoading?: boolean;
		onchange?: (value: string) => void;
	} = $props();

	let isOpen = $state(false);
	let dropdownRef: HTMLDivElement;
	let buttonRef: HTMLButtonElement;
	let menuRef: HTMLDivElement | undefined = $state();
	let selectedOption = $derived(options.find((opt) => opt.value === value));
	let dropdownPosition = $state({ top: 0, left: 0, width: 0 });
	let portalTarget: HTMLElement | undefined = $state();

	function handleSelect(optionValue: string) {
		value = optionValue;
		isOpen = false;
		onchange?.(optionValue);
	}

	function updateDropdownPosition() {
		if (buttonRef && isOpen) {
			const rect = buttonRef.getBoundingClientRect();
			const viewportHeight = window.innerHeight;
			const menuHeight = 200; // Max height from CSS

			// Calculate if there's space below, otherwise position above
			const spaceBelow = viewportHeight - rect.bottom;
			const shouldPositionAbove = spaceBelow < menuHeight && rect.top > menuHeight;

			dropdownPosition = {
				top: shouldPositionAbove ? rect.top + 18 - menuHeight - 4 : rect.bottom + 4,
				left: rect.left,
				width: rect.width
			};
		}
	}

	function detectPortalTarget() {
		if (dropdownRef) {
			// Check if we're inside a dialog
			const dialogElement = dropdownRef.closest('dialog');
			portalTarget = dialogElement || document.body;
		}
	}

	function toggleDropdown() {
		if (!disabled) {
			isOpen = !isOpen;
		}
	}

	function handleKeydown(event: KeyboardEvent) {
		if (disabled) return;

		switch (event.key) {
			case 'Enter':
			case ' ':
				event.preventDefault();
				toggleDropdown();
				break;
			case 'Escape':
				isOpen = false;
				break;
			case 'ArrowDown':
				event.preventDefault();
				if (!isOpen) {
					isOpen = true;
				} else {
					// Navigate to next option
					const currentIndex = options.findIndex((opt) => opt.value === value);
					const nextIndex = Math.min(currentIndex + 1, options.length - 1);
					if (nextIndex !== currentIndex) {
						handleSelect(options[nextIndex].value);
					}
				}
				break;
			case 'ArrowUp':
				event.preventDefault();
				if (isOpen) {
					// Navigate to previous option
					const currentIndex = options.findIndex((opt) => opt.value === value);
					const prevIndex = Math.max(currentIndex - 1, 0);
					if (prevIndex !== currentIndex) {
						handleSelect(options[prevIndex].value);
					}
				}
				break;
		}
	}

	function handleClickOutside(event: MouseEvent) {
		if (dropdownRef && !dropdownRef.contains(event.target as Node)) {
			isOpen = false;
		}
	}

	// Close dropdown when clicking outside and handle positioning updates
	$effect(() => {
		if (isOpen) {
			// Detect portal target and initial position calculation
			detectPortalTarget();
			updateDropdownPosition();

			document.addEventListener('click', handleClickOutside);
			window.addEventListener('scroll', updateDropdownPosition, true);
			window.addEventListener('resize', updateDropdownPosition);

			return () => {
				document.removeEventListener('click', handleClickOutside);
				window.removeEventListener('scroll', updateDropdownPosition, true);
				window.removeEventListener('resize', updateDropdownPosition);
			};
		}
	});
</script>

<div class="flex h-full flex-col gap-1">
	{#if title}
		<!-- Check if title is string or snippet -->
		<label class="leading-none">
			{#if typeof title === 'string'}
				{title}
			{:else}
				{@render title()}
			{/if}
		</label>
	{/if}

	<div
		bind:this={dropdownRef}
		class="grow relative inline-block w-full gradient-border {className}"
		style="--gradient-border-radius: 0.25rem;"
		class:opacity-50={disabled}
		class:pointer-events-none={disabled}
		class:open={isOpen}
	>
		<button
			bind:this={buttonRef}
			type="button"
			class="dropdown-button h-full"
			onclick={toggleDropdown}
			onkeydown={handleKeydown}
			{disabled}
			aria-haspopup="listbox"
			aria-expanded={isOpen}
		>
			<span class="flex-1 overflow-hidden text-ellipsis whitespace-nowrap text-left">
				{selectedOption?.label || placeholder}
			</span>
			{#if !isLoading}
				<span class="dropdown-arrow" class:rotated={isOpen}>
					<i class="fa-solid fa-chevron-down"></i>
				</span>
			{:else}
				<svg
					class="fa-spin col-1 row-1 mx-auto h-5 w-5 text-white"
					xmlns="http://www.w3.org/2000/svg"
					fill="none"
					viewBox="0 0 24 24"
				>
					<circle class="opacity-50" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"
					></circle>
					<path class="opacity-100" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"
					></path>
				</svg>
			{/if}
		</button>

		<!-- Portal the dropdown to body to escape overflow constraints -->
		{#if isOpen && portalTarget}
			<div
				bind:this={menuRef}
				class="dropdown-menu dropdown-menu-portal"
				style="top: {dropdownPosition.top}px; left: {dropdownPosition.left}px; width: {dropdownPosition.width}px;"
				role="listbox"
				use:createPortal={portalTarget}
			>
				{#each options as option}
					<button
						type="button"
						class="color-default font-inherit dropdown-option block w-full cursor-pointer border-0 bg-transparent px-3 py-2 text-left text-sm transition-all duration-150 ease-in-out"
						class:selected={option.value === value}
						onclick={() => handleSelect(option.value)}
						role="option"
						aria-selected={option.value === value}
					>
						{option.label}
					</button>
				{/each}
			</div>
		{/if}
	</div>
</div>

<style>
	/* Keep complex dropdown styling that's difficult to replace with Tailwind */
	.dropdown-button {
		width: 100%;
		padding: 0.5rem 0.75rem 0.5rem 0.75rem;
		border: 1px solid transparent;
		border-radius: 0.25rem;
		background: var(--color-card);
		color: var(--color-text);
		font-size: 0.9rem;
		cursor: pointer;
		transition: all 0.2s ease;
		display: flex;
		gap: 0.25rem;
		align-items: center;
		justify-content: space-between;
		text-align: left;
		font-family: inherit;
	}

	.dropdown-button:hover:not(:disabled) {
		background: color-mix(in srgb, var(--color-card), var(--color-primary) 5%);
	}

	.dropdown-button:focus {
		outline: none;
		box-shadow: 0 0 0 3px color-mix(in srgb, var(--color-primary), transparent 85%);
	}

	.dropdown-button:disabled {
		opacity: 0.5;
		cursor: not-allowed;
		background: color-mix(in srgb, var(--color-card), var(--color-background) 50%);
	}

	.dropdown-arrow {
		color: var(--color-muted);
		font-size: 0.8rem;
		transition: all 0.2s ease;
		display: flex;
		align-items: center;
		margin-left: 0.25rem;
	}

	.dropdown-arrow.rotated {
		transform: rotate(180deg);
	}

	.dropdown-button:hover:not(:disabled) .dropdown-arrow {
		color: var(--color-primary);
	}

	.dropdown-menu {
		background: var(--color-card);
		border: 1px solid transparent;
		border-radius: 6px;
		box-shadow: 0 4px 12px color-mix(in srgb, var(--color-text), transparent 85%);
		max-height: 200px;
		overflow-y: auto;
		animation: dropdownSlide 0.15s ease-out;
		position: relative;
	}

	.dropdown-menu::before {
		content: '';
		position: absolute;
		inset: 0;
		border-radius: 6px;
		padding: 1px;
		background: linear-gradient(
			135deg, 
			hsl(from var(--color-primary) h s l / 0.4),
			hsl(from var(--color-secondary) h s l / 0.3),
			hsl(from var(--color-accent) h s l / 0.4)
		);
		mask: 
			linear-gradient(#fff 0 0) content-box, 
			linear-gradient(#fff 0 0);
		-webkit-mask: 
			linear-gradient(#fff 0 0) content-box, 
			linear-gradient(#fff 0 0);
		-webkit-mask-composite: xor;
		mask-composite: exclude;
		opacity: 1;
		pointer-events: none;
	}

	.dropdown-menu-absolute {
		position: absolute;
		top: 100%;
		left: 0;
		right: 0;
		z-index: 1000;
		margin-top: 4px;
	}

	.dropdown-menu-portal {
		position: fixed;
		z-index: 9999;
	}

	@keyframes dropdownSlide {
		from {
			opacity: 0;
			transform: translateY(-8px);
		}
		to {
			opacity: 1;
			transform: translateY(0);
		}
	}

	.dropdown-option:hover {
		background: color-mix(in srgb, var(--color-primary), transparent 90%);
		color: var(--color-primary);
	}

	.dropdown-option:focus {
		outline: none;
		background: color-mix(in srgb, var(--color-primary), transparent 90%);
		color: var(--color-primary);
	}

	.dropdown-option.selected {
		background: var(--color-primary);
		color: white;
		font-weight: 500;
	}

	.dropdown-option.selected:hover {
		background: color-mix(in srgb, var(--color-primary), var(--color-text) 10%);
	}
</style>
