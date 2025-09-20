<script lang="ts">
	import { InvestmentType, type InvestmentModel } from '$lib/services/investmentService';
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import type { PositionSnapshot } from '$lib/services/positionService';

	let { quote, snapshot }: { quote: QuoteModel; snapshot: PositionSnapshot } = $props();

	const locale = navigator.languages?.[0] || navigator.language || 'en-US';

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
</script>

<!-- Type, Name, Date, Market Value, Amount, Fee, Total -->
<button class="btn-fake text-left">
	<div class="card card-reactive @container relative grid gap-1">
		<div class="color-muted flex items-center gap-2 text-sm">
			{#if quote.group}
			<span class="uppercase">{quote.group.name}</span>
			<span>•</span>
			{/if}
			<span class="uppercase">{quote.typeDisposition}</span>
		</div>
		<div class="mt-2 mb-2">
			<div class="leading-none">
				<span class="font-bold">{quote.name}</span>
			</div>
			<div>
				<span class="color-muted text-sm font-bold">{quote.symbol}</span>
			</div>
		</div>
		<div class="@md:grid-cols-3 @lg:grid-cols-4 grid grid-cols-2 gap-1 text-sm">
			<div class="grid grid-rows-2">
				<span class="color-muted">Amount</span>
				<span class="font-bold">{formatNumber(snapshot.amount)}</span>
			</div>
			<div class="grid grid-rows-2">
				<span class="color-muted">Current Value</span>
				<span class="font-bold">{formatCurrency(snapshot.totalValue)}</span>
			</div>
			<div class="grid grid-rows-2">
				<span class="color-muted">Total Fees</span>
				<span class="font-bold">{formatCurrency(snapshot.totalFees)}</span>
			</div>
			<div class="grid grid-rows-2">
				<span class="color-muted">Total Gain</span>
				<span class="font-bold"
					>{formatCurrency(snapshot.unrealizedGain + snapshot.realizedGain)}</span
				>
			</div>
		</div>
	</div>
</button>
