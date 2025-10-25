<script lang="ts">
	import { InvestmentType, type InvestmentFilters } from '$lib/services/investmentService';
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import type { QuoteGroup } from '$lib/Models/QuoteGroup';
	import Input from './Input.svelte';
	import DropDown from './DropDown.svelte';
	import Button from './Button.svelte';
	import { TextSize } from '$lib/types/TextSize';

	let {
		filters = $bindable(),
		quotes,
		groups,
		onApplyFilters,
		onClearFilters
	}: {
		filters: InvestmentFilters;
		quotes: QuoteModel[];
		groups: QuoteGroup[];
		onApplyFilters: () => Promise<void>;
		onClearFilters: () => Promise<void>;
	} = $props();

	// Local state for filter inputs
	let fromDate = $state(filters.fromDate || '');
	let toDate = $state(filters.toDate || '');
	let selectedQuoteId = $state<string>(filters.quoteId?.toString() || '');
	let selectedGroupId = $state<string>(filters.groupId?.toString() || '');
	let selectedType = $state<string>(filters.type?.toString() || '');
	let showFilters = $state(false);

	let applyingFilters = $state(false);
	let clearingFilters = $state(false);

	// Check if any filters are active
	let hasActiveFilters = $derived(
		fromDate !== '' ||
			toDate !== '' ||
			selectedQuoteId !== '' ||
			selectedGroupId !== '' ||
			selectedType !== ''
	);

	async function applyFilters() {
		applyingFilters = true;

		filters.fromDate = fromDate || undefined;
		filters.toDate = toDate || undefined;
		filters.quoteId = selectedQuoteId ? parseInt(selectedQuoteId) : undefined;
		filters.groupId = selectedGroupId ? parseInt(selectedGroupId) : undefined;
		filters.type = selectedType !== '' ? parseInt(selectedType) : undefined;
		await onApplyFilters();

		applyingFilters = false;
	}

	async function clearFilters() {
		clearingFilters = true;

		fromDate = '';
		toDate = '';
		selectedQuoteId = '';
		selectedGroupId = '';
		selectedType = '';
		filters = {};
		await onClearFilters();

		clearingFilters = false;
	}

	function toggleFilters() {
		showFilters = !showFilters;
	}

	// Sort quotes alphabetically
	let sortedQuotes = $derived(
		[...quotes].sort((a, b) => {
			const nameA = a.customName || a.name;
			const nameB = b.customName || b.name;
			return nameA.localeCompare(nameB);
		})
	);

	// Sort groups alphabetically
	let sortedGroups = $derived([...groups].sort((a, b) => a.name.localeCompare(b.name)));

	// Create dropdown options
	let typeOptions = $derived([
		{ value: '', label: 'All Types' },
		{ value: InvestmentType.Buy.toString(), label: 'Buy' },
		{ value: InvestmentType.Sell.toString(), label: 'Sell' },
		{ value: InvestmentType.Dividend.toString(), label: 'Dividend' }
	]);

	let groupOptions = $derived([
		{ value: '', label: 'All Groups' },
		...sortedGroups.map((group) => ({ value: group.id.toString(), label: group.name }))
	]);

	let quoteOptions = $derived([
		{ value: '', label: 'All Quotes' },
		...sortedQuotes.map((quote) => ({
			value: quote.id.toString(),
			label: `${quote.customName || quote.name} (${quote.symbol})`
		}))
	]);
</script>

<div class="@container">
	<!-- Filter Toggle Button -->
	<button
		class="btn-fake flex w-full items-center justify-center xs:justify-between gap-3 rounded-lg px-1 py-3 xs:w-auto"
		onclick={toggleFilters}
	>
		<div class="flex items-center gap-2">
			<i class="fa-solid fa-filter"></i>
			<span class="font-medium">Filters</span>
			{#if hasActiveFilters}
				<span
					class="rounded-full bg-blue-500 px-2 py-0.5 text-xs font-bold text-white dark:bg-blue-600"
				>
					Active
				</span>
			{/if}
		</div>
		<i class="fa-solid {showFilters ? 'fa-chevron-up' : 'fa-chevron-down'}"></i>
	</button>

	<!-- Filter Panel -->
	{#if showFilters}
		<div
			class="mb-3 grid gap-4 rounded-lg bg-card p-4 @sm:grid-cols-2 @xl:grid-cols-3"
		>
			<!-- Date Range -->
			<div class="flex flex-col gap-2 @sm:col-span-2 @xl:col-span-1">
				<Input
					id="fromDate"
					type="date"
					title="From Date"
					bind:value={fromDate}
					placeholder="Select start date"
				/>
			</div>

			<div class="flex flex-col gap-2 @sm:col-span-2 @xl:col-span-1">
				<Input
					id="toDate"
					type="date"
					title="To Date"
					bind:value={toDate}
					placeholder="Select end date"
				/>
			</div>

			<!-- Investment Type -->
			<div class="flex flex-col gap-2 @sm:col-span-2 @xl:col-span-1">
				<DropDown
					options={typeOptions}
					value={selectedType}
					onchange={(value) => (selectedType = value)}
					title="Type"
					placeholder="Select type"
				/>
			</div>

			<!-- Group Filter -->
			<div class="flex flex-col gap-2 @sm:col-span-2 @xl:col-span-1">
				<DropDown
					options={groupOptions}
					value={selectedGroupId}
					onchange={(value) => (selectedGroupId = value)}
					title="Group"
					placeholder="Select group"
				/>
			</div>

			<!-- Quote Filter -->
			<div class="flex flex-col gap-2 @sm:col-span-2">
				<DropDown
					options={quoteOptions}
					value={selectedQuoteId}
					onchange={(value) => (selectedQuoteId = value)}
					title="Quote"
					placeholder="Select quote"
				/>
			</div>

			<!-- Action Buttons -->
			<div class="flex flex-wrap gap-2 @sm:col-span-2 @xl:col-span-3">
				<Button text="Apply Filters" icon="fa-solid fa-check" onclick={applyFilters} isLoading={applyingFilters} textSize={TextSize.Small} />

				{#if hasActiveFilters}
					<Button text="Clear Filters" icon="fa-solid fa-times" onclick={clearFilters} isLoading={clearingFilters} textSize={TextSize.Small} />
				{/if}
			</div>
		</div>
	{/if}
</div>
