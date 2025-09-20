<script lang="ts">
	import { InvestmentType, type InvestmentModel } from '$lib/services/investmentService';
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import type { PositionSnapshot } from '$lib/services/positionService';

	let { quote, snapshot }: { quote: QuoteModel; snapshot: PositionSnapshot } = $props();
	let totalGain = $derived(snapshot.unrealizedGain + snapshot.realizedGain);

	function formatCurrency(value: number): string {
		return value.toLocaleString(undefined, { style: 'currency', currency: quote.currency });
	}

	function formatNumber(value: number): string {
		return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 6 });
	}

	function showGroupAssignModal() {
		// TODO: Implement group assignment modal
		alert('Group assignment modal not implemented yet.');
	}
</script>

<!-- Type, Name, Date, Market Value, Amount, Fee, Total -->
<div class="card card-reactive no-click @container relative grid gap-1">
	<div class="color-muted flex items-center gap-2 text-sm">
		{#if quote.group}
			<span class="uppercase">{quote.group.name}</span>
			<span>•</span>
		{:else}
			<button class="link-button" onclick={showGroupAssignModal}>
				<span class="uppercase">No Group</span>
			</button>
			<span>•</span>
		{/if}
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
			<span class="font-bold">{formatCurrency(snapshot.totalValue)}</span>
		</div>
		<div class="grid grid-rows-2">
			<span class="color-muted">Total Fees</span>
			<span class="font-bold">{formatCurrency(snapshot.totalFees)}</span>
		</div>
		<div class="grid grid-rows-2">
			<span class="color-muted">Total Gain</span>
			<span class="font-bold {totalGain < 0 ? 'color-error' : 'color-success'}">{formatCurrency(totalGain)}</span>
		</div>
	</div>
</div>
