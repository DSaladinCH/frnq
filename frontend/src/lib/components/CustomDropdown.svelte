<script lang="ts">
	import { createPortal } from '$lib/utils/portal.js';

	interface Option {
		value: string;
		label: string;
	}

	let {
		options = [],
		value = '',
		placeholder = 'Select an option...',
		disabled = false,
		class: className = '',
		onchange
	}: {
		options: Option[];
		value?: string;
		placeholder?: string;
		disabled?: boolean;
		class?: string;
		onchange?: (value: string) => void;
	} = $props();

	let isOpen = $state(false);
	let dropdownRef: HTMLDivElement;
	let buttonRef: HTMLButtonElement;
	let menuRef: HTMLDivElement | undefined = $state();
	let selectedOption = $derived(options.find(opt => opt.value === value));
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
					const currentIndex = options.findIndex(opt => opt.value === value);
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
					const currentIndex = options.findIndex(opt => opt.value === value);
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

<div 
	bind:this={dropdownRef}
	class="relative inline-block w-full {className}"
	class:opacity-50={disabled}
	class:pointer-events-none={disabled}
	class:open={isOpen}
>
	<button
		bind:this={buttonRef}
		type="button"
		class="dropdown-button"
		onclick={toggleDropdown}
		onkeydown={handleKeydown}
		{disabled}
		aria-haspopup="listbox"
		aria-expanded={isOpen}
	>
		<span class="flex-1 text-left whitespace-nowrap overflow-hidden text-ellipsis">
			{selectedOption?.label || placeholder}
		</span>
		<span class="dropdown-arrow" class:rotated={isOpen}>
			<i class="fa-solid fa-chevron-down"></i>
		</span>
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
					class="w-full px-3 py-2 border-0 bg-transparent color-default text-sm cursor-pointer transition-all duration-150 ease-in-out text-left block font-inherit dropdown-option"
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

<style>
	/* Keep complex dropdown styling that's difficult to replace with Tailwind */
	.dropdown-button {
		width: 100%;
		padding: 0.5rem 1.5rem 0.5rem 0.75rem;
		border: 1px solid var(--color-button);
		border-radius: 6px;
		background: var(--color-card);
		color: var(--color-text);
		font-size: 0.9rem;
		cursor: pointer;
		transition: all 0.2s ease;
		display: flex;
		align-items: center;
		justify-content: space-between;
		text-align: left;
		font-family: inherit;
	}

	.dropdown-button:hover:not(:disabled) {
		border-color: var(--color-primary);
		background: color-mix(in srgb, var(--color-card), var(--color-primary) 5%);
	}

	.dropdown-button:focus {
		outline: none;
		border-color: var(--color-secondary);
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
		border: 1px solid var(--color-button);
		border-radius: 6px;
		box-shadow: 0 4px 12px color-mix(in srgb, var(--color-text), transparent 85%);
		max-height: 200px;
		overflow-y: auto;
		animation: dropdownSlide 0.15s ease-out;
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