<script lang="ts">
	import { InvestmentType, type InvestmentModel } from '$lib/services/investmentService';
	import type { QuoteModel } from '$lib/Models/QuoteModel';

	let {
		investment,
		quote,
		onclick
	}: { investment: InvestmentModel; quote: QuoteModel; onclick: () => void } = $props();

	const locale = navigator.languages?.[0] || navigator.language || 'en-US';

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
			year: 'numeric'
		});
	}

	function formatCurrency(value: number): string {
		return value.toLocaleString(undefined, { style: 'currency', currency: quote.currency });
	}

	function formatNumber(value: number): string {
		return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 6 });
	}
</script>

<!-- Type, Name, Date, Market Value, Amount, Fee, Total -->
<div class="card card-reactive grid gap-1 @container">
	<div class="flex items-center gap-2 text-sm color-muted">
		<span class="uppercase">{getInvestmentType(investment)}</span>
		<span>•</span>
		<span>{formatDate(investment.date)}</span>
		<span>•</span>
		<span>{quote.currency}</span>
	</div>
	<div class="pb-3">
		<span class="font-bold">{quote.name}</span>
	</div>
	<div class="grid grid-cols-2 @md:grid-cols-3 @lg:grid-cols-4 gap-1 text-sm">
		<div class="grid grid-rows-2">
			<span class="color-muted">Amount</span>
			<span class="font-bold">{formatNumber(investment.amount)}</span>
		</div>
		<div class="grid grid-rows-2">
			<span class="color-muted">Price per Unit</span>
			<span class="font-bold">{formatCurrency(investment.pricePerUnit)}</span>
		</div>
		<div class="grid grid-rows-2">
			<span class="color-muted">Total Fees</span>
			<span class="font-bold">{formatCurrency(investment.totalFees)}</span>
		</div>
		<div class="grid grid-rows-2">
			<span class="color-muted">Total</span>
			<span class="font-bold">{formatCurrency(investment.pricePerUnit * investment.amount + investment.totalFees)}</span>
		</div>
	</div>
</div>
