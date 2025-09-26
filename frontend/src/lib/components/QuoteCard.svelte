<script lang="ts">
	import { InvestmentType, type InvestmentModel } from '$lib/services/investmentService';
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import type { PositionSnapshot } from '$lib/services/positionService';

	let {
		quote,
		snapshot,
		onAssignGroup
	}: { quote: QuoteModel; snapshot: PositionSnapshot; onAssignGroup: () => void } = $props();
	let totalGain = $derived(snapshot.currentValue + snapshot.realizedGain - snapshot.invested);

	function formatCurrency(value: number): string {
		return value.toLocaleString(undefined, { style: 'currency', currency: quote.currency });
	}

	function formatNumber(value: number): string {
		return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 6 });
	}
</script>

<!-- Type, Name, Date, Market Value, Amount, Fee, Total -->
<div class="card card-reactive no-click @container relative grid gap-1">
	<div class="color-muted flex items-center gap-2 text-sm">
		<button class="link-button" onclick={onAssignGroup}>
			<span class="uppercase">{quote.group ? quote.group.name : 'No Group'}</span>
		</button>
		<span>•</span>
		<span class="uppercase">{quote.typeDisposition}</span>
	</div>
	<div class="mb-2 mt-2">
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
			<span class="font-bold">{formatCurrency(snapshot.currentValue)}</span>
		</div>
		<div class="grid grid-rows-2">
			<span class="color-muted">Total Fees</span>
			<span class="font-bold">{formatCurrency(snapshot.totalFees)}</span>
		</div>
		<div class="grid grid-rows-2">
			<span class="color-muted">Total Gain</span>
			<span class="font-bold {totalGain < 0 ? 'color-error' : 'color-success'}"
				>{formatCurrency(totalGain)}</span
			>
		</div>
	</div>
</div>
