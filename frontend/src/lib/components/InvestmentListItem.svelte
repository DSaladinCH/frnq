<script lang="ts">
	import { InvestmentType, type InvestmentModel } from '$lib/services/investmentService';
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import Button from './Button.svelte';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import { TextSize } from '$lib/types/TextSize';
	import { ContentWidth } from '$lib/types/ContentSize';

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

	const locale = navigator.languages?.[0] || navigator.language || 'en-US';
	let isDeleting = $state(false);

	let dividendHidden = $derived(investment.type === InvestmentType.Dividend ? 'hidden' : '');

	function getInvestmentType(investment: InvestmentModel): string {
		switch (investment.type) {
			case InvestmentType.Dividend:
				return 'DIV';
			default:
				return InvestmentType[investment.type];
		}
	}

	function formatDate(date: string): string {
		return new Date(date).toLocaleDateString(locale, {
			day: '2-digit',
			month: '2-digit',
			year: 'numeric',
			hour: '2-digit',
			minute: '2-digit'
		});
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
<button class="btn-fake w-full text-left" {onclick}>
	<div
		class="card card-reactive relative grid grid-cols-[2fr_100px_130px_100px_130px] grid-rows-[auto_1fr] gap-2 text-sm items-center leading-none"
	>
		<div class="row-1 col-1 color-muted flex items-center gap-2">
			<span class="uppercase">{getInvestmentType(investment)}</span>
			<span>•</span>
			<span>{formatDate(investment.date)}</span>
			<span>•</span>
			<span>{quote.currency}</span>
		</div>

		<div class="row-2 col-1 text-base leading-none">
			<span class="font-bold">{quote.name}</span>
		</div>

		<span class="row-1 color-muted">Amount</span>
		<span class="row-2 font-bold">{formatNumber(investment.amount)}</span>

		<span class="row-1 color-muted {dividendHidden}"
			>Price per Unit</span
		>
		<span class="row-2 font-bold {dividendHidden}"
			>{formatCurrency(investment.pricePerUnit)}</span
		>

		<span class="row-1 color-muted {dividendHidden}"
			>Total Fees</span
		>
		<span class="row-2 font-bold {dividendHidden}"
			>{formatCurrency(investment.totalFees)}</span
		>

		<span class="row-1 color-muted {dividendHidden}"
			>Total</span
		>
		<span class="row-2 font-bold {dividendHidden}"
			>{formatCurrency(investment.pricePerUnit * investment.amount + investment.totalFees)}</span
		>

		<div class="row-span-2 -col-1 h-full">
			<Button
				onclick={deleteInvestment}
				disabled={isDeleting}
				isLoading={isDeleting}
				icon="fa-solid fa-trash"
				textSize={TextSize.Small}
				style={ColorStyle.Secondary}
				width={ContentWidth.Full}
			/>
		</div>
	</div>
</button>
