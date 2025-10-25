<script lang="ts">
	import { InvestmentType, type InvestmentModel } from '$lib/services/investmentService';
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import Button from './Button.svelte';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import { TextSize } from '$lib/types/TextSize';
	import { formatDateTime } from '$lib/utils/dateFormat';
	import { userPreferences } from '$lib/stores/userPreferences';

	let {
		investment,
		quote,
		onclick,
		ondelete
	}: {
		investment: InvestmentModel;
		quote: QuoteModel;
		onclick: () => void;
		ondelete: (event: MouseEvent) => Promise<void>;
	} = $props();

	let preferences = $state($userPreferences);
	let isDeleting = $state(false);

	// Subscribe to user preferences changes
	$effect(() => {
		const unsubscribe = userPreferences.subscribe((prefs) => {
			preferences = prefs;
		});
		return unsubscribe;
	});

	function getInvestmentType(investment: InvestmentModel): string {
		switch (investment.type) {
			case InvestmentType.Dividend:
				return 'DIV';
			default:
				return InvestmentType[investment.type];
		}
	}

	function formatCurrency(value: number): string {
		return value.toLocaleString(undefined, { style: 'currency', currency: quote.currency });
	}

	function formatNumber(value: number): string {
		return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 6 });
	}

	async function deleteInvestment(event: MouseEvent) {
		if (isDeleting) return;

		isDeleting = true;

		event.stopPropagation();
		await ondelete(event);

		isDeleting = false;
	}
</script>

<!-- Type, Name, Date, Market Value, Amount, Fee, Total -->
<button class="btn-fake text-left w-full" {onclick}>
	<div class="card card-reactive @container relative grid grid-cols-1 gap-1">
		<div class="color-muted flex items-center gap-2 text-sm row-1">
			<span class="uppercase">{getInvestmentType(investment)}</span>
			<span>•</span>
			<span>{formatDateTime(investment.date, preferences.dateFormat)}</span>
			<span>•</span>
			<span>{quote.currency}</span>
		</div>

		<div class="row-2">
			<span class="font-bold">{quote.customName || quote.name}</span>
		</div>

		<div class="row-span-2 -col-1 h-10">
			<Button
				onclick={deleteInvestment}
				disabled={isDeleting}
				isLoading={isDeleting}
				icon="fa-solid fa-trash"
				textSize={TextSize.Small}
				style={ColorStyle.Secondary}
			/>
		</div>

		<div class="pt-2 row-3 col-span-2 @md:grid-cols-3 @lg:grid-cols-4 grid grid-cols-2 gap-1 text-sm">
			<div class="grid grid-rows-2">
				<span class="color-muted">Amount</span>
				<span class="font-bold">{formatNumber(investment.amount)}</span>
			</div>
			<div class="grid grid-rows-2 {investment.type === InvestmentType.Dividend ? 'hidden' : ''}">
				<span class="color-muted">Price per Unit</span>
				<span class="font-bold">{formatCurrency(investment.pricePerUnit)}</span>
			</div>
			<div class="grid grid-rows-2 {investment.type === InvestmentType.Dividend ? 'hidden' : ''}">
				<span class="color-muted">Total Fees</span>
				<span class="font-bold">{formatCurrency(investment.totalFees)}</span>
			</div>
			<div class="grid grid-rows-2 {investment.type === InvestmentType.Dividend ? 'hidden' : ''}">
				<span class="color-muted">Total</span>
				<span class="font-bold"
					>{formatCurrency(
						investment.pricePerUnit * investment.amount + investment.totalFees
					)}</span
				>
			</div>
		</div>
	</div>
</button>
