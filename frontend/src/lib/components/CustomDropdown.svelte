<script lang="ts">
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
	let selectedOption = $derived(options.find(opt => opt.value === value));
	let dropdownPosition = $state({ top: 0, left: 0, width: 0 });

	function handleSelect(optionValue: string) {
		value = optionValue;
		isOpen = false;
		onchange?.(optionValue);
	}

	function updateDropdownPosition() {
		if (buttonRef && isOpen) {
			const rect = buttonRef.getBoundingClientRect();
			dropdownPosition = {
				top: rect.bottom + 4,
				left: rect.left,
				width: rect.width
			};
		}
	}

	function toggleDropdown() {
		if (!disabled) {
			isOpen = !isOpen;
			if (isOpen) {
				// Update position when opening
				setTimeout(updateDropdownPosition, 0);
			}
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
					setTimeout(updateDropdownPosition, 0);
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

	// Close dropdown when clicking outside
	$effect(() => {
		if (isOpen) {
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
	class="custom-dropdown {className}"
	class:disabled
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
		<span class="dropdown-text">
			{selectedOption?.label || placeholder}
		</span>
		<span class="dropdown-arrow" class:rotated={isOpen}>
			<i class="fa-solid fa-chevron-down"></i>
		</span>
	</button>
</div>

<!-- Render dropdown menu in a portal-like way using fixed positioning -->
{#if isOpen}
	<div 
		class="dropdown-menu-fixed" 
		style="top: {dropdownPosition.top}px; left: {dropdownPosition.left}px; width: {dropdownPosition.width}px;"
		role="listbox"
	>
		{#each options as option}
			<button
				type="button"
				class="dropdown-option"
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

<style>
	.custom-dropdown {
		position: relative;
		display: inline-block;
		width: 100%;
	}

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

	.dropdown-text {
		flex: 1;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
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

	.dropdown-menu-fixed {
		position: fixed;
		z-index: 99999;
		background: var(--color-card);
		border: 1px solid var(--color-button);
		border-radius: 6px;
		box-shadow: 0 4px 12px color-mix(in srgb, var(--color-text), transparent 85%);
		max-height: 200px;
		overflow-y: auto;
		animation: dropdownSlide 0.15s ease-out;
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

	.dropdown-option {
		width: 100%;
		padding: 0.5rem 0.75rem;
		border: none;
		background: transparent;
		color: var(--color-text);
		font-size: 0.9rem;
		cursor: pointer;
		transition: all 0.15s ease;
		text-align: left;
		display: block;
		font-family: inherit;
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

	.custom-dropdown.disabled {
		pointer-events: none;
	}
</style>