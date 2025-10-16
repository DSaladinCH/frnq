<script lang="ts">
	import {
		InvestmentType,
		investmentValuesValid,
		type InvestmentModel
	} from '$lib/services/investmentService';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import Button from './Button.svelte';
	import SearchableDropDown from './SearchableDropDown.svelte';
	import Input from './Input.svelte';
	import { ContentWidth } from '$lib/types/ContentSize';
	import { notify } from '$lib/services/notificationService';

	type InvestmentTypeIcon = { type: InvestmentType; faIcon: string };
	let isLoading = $state(false);

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
		saveInvestment: (investment: InvestmentModel) => Promise<void>;
	} = $props();

	// Update investment fields when quote changes
	$effect(() => {		
		if (quote) {
			// Check if the quote exists in database (has a valid ID > 0)
			if (quote.id > 0) {
				investment.quoteId = quote.id;
				investment.providerId = quote.providerId;
				investment.quoteSymbol = quote.symbol;
			} else {
				// Quote doesn't exist in database yet, use providerId and symbol
				investment.quoteId = 0;
				investment.providerId = quote.providerId;
				investment.quoteSymbol = quote.symbol;
			}
		} else {
			// Clear quote fields if no quote selected
			investment.quoteId = 0;
			investment.providerId = undefined;
			investment.quoteSymbol = undefined;
		}
	});

	function getLocalDateTimeString(date: Date): string {
		// Returns YYYY-MM-DDTHH:MM
		const pad = (n: number) => n.toString().padStart(2, '0');
		return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
	}

	let totalInvestment: number = $derived(
		investment.type === InvestmentType.Dividend
			? investment.amount
			: investment.pricePerUnit * investment.amount + investment.totalFees
	);

	const investmentTypes: InvestmentTypeIcon[] = [
		{ type: InvestmentType.Buy, faIcon: 'fa-solid fa-plus-circle' },
		{ type: InvestmentType.Sell, faIcon: 'fa-solid fa-minus-circle' },
		{ type: InvestmentType.Dividend, faIcon: 'fa-solid fa-money-bills' }
	];

	function selectType(type: InvestmentType) {
		investment.type = type;
	}

	function formatCurrency(value: number): string {
		return value.toLocaleString(undefined, { style: 'currency', currency: 'CHF' });
	}

	async function saveChanges() {
		if (!investmentValuesValid(investment)) {
			let errorMsg = 'Please fix the following issues:\n';
			if (!investmentValuesValid(investment)) {
				notify.error(errorMsg);
				return;
			}
		}

		isLoading = true;
		await saveInvestment(investment);
		isLoading = false;
	}
</script>

<div class="grid gap-4 overflow-y-auto pr-1">
	<div class="grid gap-3 md:grid-cols-[2fr_1fr]">
		<div class="xs:grid-cols-2 grid gap-3 sm:grid-cols-3">
			{#each investmentTypes as type}
				<button class="btn-fake" onclick={() => selectType(type.type)}>
					<div
						class="investment-type card card-reactive h-15 xs:h-22 xs:flex-col xs:gap-2 flex items-center justify-center gap-4"
						class:selected={investment.type === type.type}
					>
						<i class="{type.faIcon} xs:text-3xl text-2xl"></i>
						<span class="xs:text-xl text-lg font-bold">{InvestmentType[type.type]}</span>
					</div>
				</button>
			{/each}
		</div>

		<div></div>
	</div>

	<div>
		<label for="quote-search" class="mb-2 block text-lg font-bold">Quote</label>
		<SearchableDropDown
			bind:selectedQuote={quote}
			placeholder="Search for a quote (e.g., Apple, AAPL)..."
		/>
	</div>

	<div class="xs:grid-cols-2 xs:gap-3 grid grid-cols-1 gap-1 sm:grid-cols-3">
		<div class="flex flex-col {investment.type === InvestmentType.Dividend ? 'hidden' : ''}">
			<Input
				title="Market"
				type="number"
				step="any"
				min={0}
				required
				bind:value={investment.pricePerUnit}
			/>
		</div>
		<div class="flex flex-col">
			<Input
				title="Amount"
				type="number"
				step="any"
				min={0}
				required
				bind:value={investment.amount}
			/>
		</div>
		<div class="flex flex-col {investment.type === InvestmentType.Dividend ? 'hidden' : ''}">
			<Input
				title="Fees"
				type="number"
				step="any"
				min={0}
				required
				bind:value={investment.totalFees}
			/>
		</div>
		<div class="flex flex-col">
			<Input
				title="Date"
				type="datetime-local"
				locale="de-CH"
				required
				bind:value={investment.date}
			/>
		</div>
		<!-- Empty placeholder -->
		<div
			class="xs:max-sm:hidden {investment.type === InvestmentType.Dividend ? 'hidden' : ''}"
		></div>
		<div class="flex flex-col">
			<span class="text-lg font-bold">Total</span>
			<span class="grow-1 text-(--color-success) flex items-center text-xl font-bold">
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
			icon={investment.id === 0 ? 'fa-solid fa-plus' : 'fa-solid fa-floppy-disk'}
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
