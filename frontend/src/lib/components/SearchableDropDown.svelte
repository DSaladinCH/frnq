<script lang="ts">
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import { searchQuotes } from '$lib/services/quoteService';
	import Input from './Input.svelte';

	type Props = {
		selectedQuote?: QuoteModel | null;
		onSelect?: (quote: QuoteModel | null) => void;
		placeholder?: string;
		providerId?: string;
	};

	let {
		selectedQuote = $bindable(null),
		onSelect = undefined,
		placeholder = 'Search for quotes...',
		providerId = 'yahoo-finance'
	}: Props = $props();

	let searchTerm = $state('');
	let isOpen = $state(false);
	let isLoading = $state(false);
	let searchResults = $state<QuoteModel[]>([]);
	let searchTimeout: ReturnType<typeof setTimeout> | null = null;
	let dropdownElement: HTMLDivElement;
	let inputElement: HTMLInputElement | undefined = $state<HTMLInputElement>();

	// Initialize search term with selected quote name only when selectedQuote changes
	$effect(() => {
		if (selectedQuote) {
			searchTerm = selectedQuote.name;
		} else {
			searchTerm = '';
		}
	});

	// Debounced search function
	async function performSearch(query: string) {
		if (!query.trim()) {
			searchResults = [];
			isOpen = false;
			return;
		}

		isLoading = true;
		try {
			const results = await searchQuotes(query, providerId);
			searchResults = results;
			isOpen = results.length > 0;
		} catch (error) {
			console.error('Failed to search quotes:', error);
			searchResults = [];
			isOpen = false;
		} finally {
			isLoading = false;
		}
	}

	// Handle input changes with debouncing
	function handleInput(e: Event) {
		const target = e.target as HTMLInputElement;
		searchTerm = target.value;

		// Don't clear selection while user is typing - let them finish their search
		// Selection will be cleared when they explicitly select a different quote or clear the input
		
		// Clear existing timeout
		if (searchTimeout) {
			clearTimeout(searchTimeout);
		}

		// Set new timeout for debounced search
		searchTimeout = setTimeout(() => {
			performSearch(searchTerm);
		}, 500); // 500ms delay
	}

	// Handle quote selection
	function selectQuote(quote: QuoteModel) {
		selectedQuote = quote;
		searchTerm = quote.name;
		isOpen = false;
		onSelect?.(quote);
	}

	// Clear selection
	function clearSelection() {
		selectedQuote = null;
		searchTerm = '';
		searchResults = [];
		isOpen = false;
		onSelect?.(null);
		inputElement?.focus();
	}

	// Handle keyboard navigation
	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') {
			isOpen = false;
		} else if (e.key === 'ArrowDown' && isOpen && searchResults.length > 0) {
			e.preventDefault();
			const firstItem = dropdownElement.querySelector('.dropdown-item') as HTMLElement;
			firstItem?.focus();
		}
	}

	// Handle dropdown item keyboard navigation
	function handleItemKeydown(e: KeyboardEvent, quote: QuoteModel) {
		if (e.key === 'Enter' || e.key === ' ') {
			e.preventDefault();
			selectQuote(quote);
		} else if (e.key === 'Escape') {
			isOpen = false;
			inputElement?.focus();
		} else if (e.key === 'ArrowDown') {
			e.preventDefault();
			const current = e.target as HTMLElement;
			const next = current.nextElementSibling as HTMLElement;
			next?.focus();
		} else if (e.key === 'ArrowUp') {
			e.preventDefault();
			const current = e.target as HTMLElement;
			const prev = current.previousElementSibling as HTMLElement;
			if (prev) {
				prev.focus();
			} else {
				inputElement?.focus();
			}
		}
	}

	// Click outside to close dropdown
	function handleClickOutside(e: MouseEvent) {
		if (dropdownElement && !dropdownElement.contains(e.target as Node)) {
			isOpen = false;
		}
	}

	// Format quote display
	function formatQuoteDisplay(quote: QuoteModel): string {
		return `${quote.name} (${quote.symbol})`;
	}

	// Handle focus
	function handleFocus() {
		if (searchTerm && searchResults.length > 0) {
			isOpen = true;
		}
	}
</script>

<svelte:window onclick={handleClickOutside} />

<div class="relative w-full z-20" bind:this={dropdownElement}>
	<div class="relative flex items-center">
		<Input bind:inputElement={inputElement}
			id="quote-search"
			type="text"
			bind:value={searchTerm}
			{placeholder}
			oninput={handleInput}
			onkeydown={handleKeydown}
			onfocus={handleFocus}
			autocomplete="off" />

		<!-- Loading spinner -->
		{#if isLoading}
			<div class="loading-spinner">
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
			</div>
		{/if}

		<!-- Clear button -->
		{#if selectedQuote}
			<button
				type="button"
				class="clear-button"
				onclick={clearSelection}
				title="Clear selection"
				aria-label="Clear selection"
			>
				<i class="fa-solid fa-times"></i>
			</button>
		{/if}
	</div>

	<!-- Dropdown list -->
	{#if isOpen && searchResults.length > 0}
		<div class="dropdown-list absolute top-full left-0 right-0 bg-card border border-solid border-button rounded-sm max-h-50 overflow-y-auto z-100">
			{#each searchResults as quote}
				<button
					type="button"
					class="dropdown-item xs:gap-0 gap-1 leading-none w-full py-2 px-4 text-left bg-none border-0 cursor-pointer transition-colors duration-150 flex flex-col"
					class:selected={selectedQuote?.symbol === quote.symbol}
					onclick={() => selectQuote(quote)}
					onkeydown={(e) => handleItemKeydown(e, quote)}
					tabindex="0"
				>
					<div class="flex font-semibold">
						<span class="line-clamp-2 flex-1 overflow-ellipsis text-base">{quote.name}</span>
					</div>

					<div class="color-muted flex items-center gap-2 text-xs">
						<span>{quote.symbol}</span>
						{#if quote.exchangeDisposition}
							<span>•</span>
							<span >{quote.exchangeDisposition}</span>
						{/if}

						{#if quote.typeDisposition}
							<span>•</span>
							<span>{quote.typeDisposition}</span>
						{/if}
					</div>
				</button>
			{/each}
		</div>
	{/if}
</div>

<style>
	.loading-spinner,
	.clear-button {
		position: absolute;
		right: 0.75rem;
		display: flex;
		align-items: center;
		justify-content: center;
		color: var(--color-muted);
		pointer-events: auto;
	}

	.clear-button {
		background: none;
		border: none;
		cursor: pointer;
		padding: 0.25rem;
		border-radius: 50%;
		transition:
			color 0.2s,
			background-color 0.2s;
	}

	.clear-button:hover {
		color: var(--color-primary);
		background-color: rgba(255, 255, 255, 0.1);
	}

	.dropdown-item:hover {
		background: color-mix(in srgb, var(--color-primary), transparent 90%);
		color: var(--color-primary);
	}

	.dropdown-item:focus {
		outline: none;
		background: color-mix(in srgb, var(--color-primary), transparent 90%);
		color: var(--color-primary);
	}

	.dropdown-item.selected {
		background: var(--color-primary);
		color: white;
		font-weight: 500;
	}

	.dropdown-item.selected:hover {
		background: color-mix(in srgb, var(--color-primary), var(--color-text) 10%);
	}

	.dropdown-list::-webkit-scrollbar-thumb:hover {
		background: var(--color-primary);
	}
</style>
