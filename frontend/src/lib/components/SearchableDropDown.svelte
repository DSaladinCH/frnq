<script lang="ts">
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import { searchQuotes } from '$lib/services/quoteService';

	type Props = {
		selectedQuote?: QuoteModel | null;
		onSelect: (quote: QuoteModel | null) => void;
		placeholder?: string;
		providerId?: string;
	};

	let {
		selectedQuote = $bindable(null),
		onSelect,
		placeholder = 'Search for quotes...',
		providerId = 'yahoo-finance'
	}: Props = $props();

	let searchTerm = $state('');
	let isOpen = $state(false);
	let isLoading = $state(false);
	let searchResults = $state<QuoteModel[]>([]);
	let searchTimeout: number | null = null;
	let dropdownElement: HTMLDivElement;
	let inputElement: HTMLInputElement;

	// Initialize search term with selected quote name
	$effect(() => {
		if (selectedQuote && searchTerm !== selectedQuote.name) {
			searchTerm = selectedQuote.name;
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

		// Clear existing timeout
		if (searchTimeout) {
			clearTimeout(searchTimeout);
		}

		// Set new timeout for debounced search
		searchTimeout = setTimeout(() => {
			performSearch(searchTerm);
		}, 300); // 300ms delay
	}

	// Handle quote selection
	function selectQuote(quote: QuoteModel) {
		console.log('SearchableDropDown: selecting quote', quote);
		selectedQuote = quote;
		searchTerm = quote.name;
		isOpen = false;
		onSelect(quote);
		console.log('SearchableDropDown: called onSelect with', quote);
	}

	// Clear selection
	function clearSelection() {
		selectedQuote = null;
		searchTerm = '';
		searchResults = [];
		isOpen = false;
		onSelect(null);
		inputElement.focus();
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
			inputElement.focus();
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
				inputElement.focus();
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

<div class="searchable-dropdown" bind:this={dropdownElement}>
	<div class="input-container">
		<input
			bind:this={inputElement}
			id="quote-search"
			type="text"
			class="textbox dropdown-input"
			bind:value={searchTerm}
			{placeholder}
			oninput={handleInput}
			onkeydown={handleKeydown}
			onfocus={handleFocus}
			autocomplete="off"
		/>
		
		<!-- Loading spinner -->
		{#if isLoading}
			<div class="loading-spinner">
				<i class="fa-solid fa-spinner fa-spin"></i>
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
		<div class="dropdown-list">
			{#each searchResults as quote}
				<button
					type="button"
					class="dropdown-item"
					class:selected={selectedQuote?.id === quote.id}
					onclick={() => selectQuote(quote)}
					onkeydown={(e) => handleItemKeydown(e, quote)}
					tabindex="0"
				>
					<div class="quote-main">
						<span class="quote-name">{quote.name}</span>
						<span class="quote-symbol">({quote.symbol})</span>
					</div>
					<div class="quote-details">
						{#if quote.exchangeDisposition}
							<span class="quote-exchange">{quote.exchangeDisposition}</span>
						{/if}
						{#if quote.typeDisposition}
							<span class="quote-type">{quote.typeDisposition}</span>
						{/if}
					</div>
				</button>
			{/each}
		</div>
	{/if}
</div>

<style>
	.searchable-dropdown {
		position: relative;
		width: 100%;
		z-index: 20;
	}

	.input-container {
		position: relative;
		display: flex;
		align-items: center;
	}

	.dropdown-input {
		width: 100%;
		padding-right: 3rem; /* Space for loading/clear buttons */
	}

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
		transition: color 0.2s, background-color 0.2s;
	}

	.clear-button:hover {
		color: var(--color-primary);
		background-color: rgba(255, 255, 255, 0.1);
	}

	.dropdown-list {
		position: absolute;
		top: 100%;
		left: 0;
		right: 0;
		background-color: var(--color-card);
		border: 1px solid var(--color-button);
		border-radius: 0.25rem;
		box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
		max-height: 200px;
		overflow-y: auto;
		z-index: 1000;
		margin-top: 0.125rem;
	}

	.dropdown-item {
		width: 100%;
		padding: 0.75rem 1rem;
		background: none;
		border: none;
		color: var(--color-text);
		cursor: pointer;
		text-align: left;
		transition: background-color 0.15s;
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
	}

	.dropdown-item:hover,
	.dropdown-item:focus {
		background-color: var(--color-primary);
		outline: none;
	}

	.dropdown-item.selected {
		background-color: var(--color-secondary);
	}

	.quote-main {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		font-weight: 500;
	}

	.quote-name {
		flex: 1;
		font-size: 0.95rem;
	}

	.quote-symbol {
		font-family: 'Fira Mono', 'Consolas', monospace;
		font-size: 0.85rem;
		color: var(--color-muted);
	}

	.quote-details {
		display: flex;
		gap: 0.75rem;
		font-size: 0.8rem;
		color: var(--color-muted);
	}

	.quote-exchange,
	.quote-type {
		font-family: 'Fira Mono', 'Consolas', monospace;
	}

	/* Scrollbar styling for dropdown */
	.dropdown-list::-webkit-scrollbar {
		width: 6px;
	}

	.dropdown-list::-webkit-scrollbar-track {
		background: transparent;
	}

	.dropdown-list::-webkit-scrollbar-thumb {
		background: var(--color-button);
		border-radius: 3px;
	}

	.dropdown-list::-webkit-scrollbar-thumb:hover {
		background: var(--color-primary);
	}
</style>