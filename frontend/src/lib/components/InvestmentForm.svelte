<script lang="ts">
	import {
		InvestmentType,
		updateInvestment,
		type InvestmentModel
	} from '$lib/services/investmentService';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import Button from './Button.svelte';
	import SearchableDropDown from './SearchableDropDown.svelte';
	import { ContentWidth } from '$lib/types/ContentSize';

	type InvestmentTypeIcon = { type: InvestmentType; faIcon: string };
	let isLoading = $state(false);
	let selectedQuote: QuoteModel | null = $state(null);

	let {
		investment = $bindable({
			id: 0,
			quoteId: 0,
			providerId: undefined,
			quoteSymbol: undefined,
			type: InvestmentType.Buy,
			pricePerUnit: 0,
			amount: 0,
			totalFees: 0,
			date: getLocalDateTimeString(new Date())
		}),
		quote = $bindable(null),
		saveInvestment
	}: { 
		investment?: InvestmentModel; 
		quote?: QuoteModel | null;
		saveInvestment: (investment: InvestmentModel) => Promise<void> 
	} = $props();

	// Sync selectedQuote with the quote prop
	$effect(() => {
		selectedQuote = quote;
	});

	// Helper to normalize date string for datetime-local input
	function normalizeDateForInput(dateStr: string): string {
		if (!dateStr) return '';
		// Remove timezone (Z or +00:00) and seconds if present
		// Accepts: 2025-05-14T00:00:00Z or 2025-05-14T00:00:00+00:00
		let match = dateStr.match(/^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})/);
		if (match) return match[1];
		// If already correct format, return as is
		if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(dateStr)) return dateStr;
		return '';
	}

	// Derived value for input (Svelte runes mode)
	let dateInputValue = $derived(normalizeDateForInput(investment.date));

	function onDateInput(e: Event) {
		const val = (e.target as HTMLInputElement).value;
		investment.date = val;
	}

	function getLocalDateTimeString(date: Date): string {
		// Returns YYYY-MM-DDTHH:MM
		const pad = (n: number) => n.toString().padStart(2, '0');
		return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
	}

	let totalInvestment: number = $derived(
		investment.pricePerUnit * investment.amount + investment.totalFees
	);

	const investmentTypes: InvestmentTypeIcon[] = [
		{ type: InvestmentType.Buy, faIcon: 'fa-solid fa-plus-circle' },
		{ type: InvestmentType.Sell, faIcon: 'fa-solid fa-minus-circle' },
		{ type: InvestmentType.Dividend, faIcon: 'fa-solid fa-money-bills' }
	];

	function selectType(type: InvestmentType) {
		investment.type = type;
	}

	function handleQuoteSelect(selectedQuote: QuoteModel | null) {
		quote = selectedQuote;

		if (selectedQuote) {
			// Check if the quote exists in database (has a valid ID > 0)
			if (selectedQuote.id > 0) {
				investment.quoteId = selectedQuote.id;
				investment.providerId = undefined;
				investment.quoteSymbol = undefined;
			} else {
				// Quote doesn't exist in database yet, use providerId and symbol
				investment.quoteId = 0;
				investment.providerId = selectedQuote.providerId;
				investment.quoteSymbol = selectedQuote.symbol;
			}
		} else {
			investment.quoteId = 0;
			investment.providerId = undefined;
			investment.quoteSymbol = undefined;
		}
	}

	function formatCurrency(value: number): string {
		return value.toLocaleString(undefined, { style: 'currency', currency: 'CHF' });
	}

	async function saveChanges() {
		// Validate inputs
		const hasValidQuote = investment.quoteId > 0 || (investment.providerId && investment.quoteSymbol);
		
		if (investment.pricePerUnit <= 0 || investment.amount <= 0 || !investment.date || !hasValidQuote) {
			// TODO: Implement toast notifications
			alert('Please fill in all required fields with valid values and select a quote.');
			return;
		}

		isLoading = true;
		await saveInvestment(investment);
		isLoading = false;
	}
</script>

<div class="overflow-y-auto pr-1 grid gap-4">
	<div class="grid gap-3 md:grid-cols-[2fr_1fr]">
		<div class="xs:grid-cols-2 grid gap-3 sm:grid-cols-3">
			{#each investmentTypes as type}
				<button class="btn-fake" onclick={() => selectType(type.type)}>
					<div
						class="investment-type card card-reactive h-15 xs:h-22 xs:flex-col xs:gap-2 flex items-center justify-center gap-4"
						class:selected={investment.type === type.type}
					>
						<i class="{type.faIcon} xs:text-[1.75rem] text-[1.5rem]"></i>
						<span class="xs:text-[1.25rem] text-[1.125rem] font-bold"
							>{InvestmentType[type.type]}</span
						>
					</div>
				</button>
			{/each}
		</div>

		<div></div>
	</div>

	<div>
		<label for="quote-search" class="text-[1.125rem] font-bold mb-2 block">Quote</label>
		<SearchableDropDown 
			bind:selectedQuote={selectedQuote}
			onSelect={handleQuoteSelect}
			placeholder="Search for a quote (e.g., Apple, AAPL)..."
		/>
	</div>


	<div class="xs:grid-cols-2 xs:gap-3 grid grid-cols-1 gap-1 sm:grid-cols-3">
		<div class="flex flex-col">
			<span class="text-[1.125rem] font-bold">Market</span>
			<input
				class="textbox"
				type="number"
				step="any"
				min="0"
				required
				bind:value={investment.pricePerUnit}
			/>
		</div>
		<div class="flex flex-col">
			<span class="text-[1.125rem] font-bold">Amount</span>
			<input
				class="textbox"
				type="number"
				step="any"
				min="0"
				required
				bind:value={investment.amount}
			/>
		</div>
		<div class="flex flex-col">
			<span class="text-[1.125rem] font-bold">Fees</span>
			<input
				class="textbox"
				type="number"
				step="any"
				min="0"
				required
				bind:value={investment.totalFees}
			/>
		</div>
		<div class="flex flex-col">
			<span class="text-[1.125rem] font-bold">Date</span>
			<input
				class="textbox"
				type="datetime-local"
				required
				value={dateInputValue}
				oninput={onDateInput}
			/>
		</div>
		<!-- Empty placeholder -->
		<div class="xs:max-sm:hidden"></div>
		<div class="flex flex-col">
			<span class="text-[1.125rem] font-bold">Total</span>
			<span class="grow-1 text-(--color-success) flex items-center text-[1.25rem] font-bold">
				{#if totalInvestment === 0}
					<span class="text-(--color-error)">-</span>
				{:else}
					{formatCurrency(totalInvestment)}
				{/if}
			</span>
		</div>
	</div>

	<div class="grid">
		<Button
			icon={investment.id === 0 ? "fa-solid fa-plus" : "fa-solid fa-floppy-disk"}
			text={investment.id === 0 ? 'Create Investment' : 'Save Changes'}
			style={ColorStyle.Success}
			width={ContentWidth.Full}
			{isLoading}
			onclick={() => saveChanges()}
		/>
	</div>
</div>

<style>
	.title {
		font-size: 1.5em;
		font-weight: bold;
		margin-bottom: 1em;
	}
</style>
