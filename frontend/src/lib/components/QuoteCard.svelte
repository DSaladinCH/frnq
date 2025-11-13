<script lang="ts">
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import type { PositionSnapshot } from '$lib/services/positionService';
	import MenuButton from './MenuButton.svelte';
	import MenuItem from './MenuItem.svelte';
	import MenuSeparator from './MenuSeparator.svelte';

	let isGroupRemoving = $state(false);
	let isCustomNameRemoving = $state(false);

	let {
		quote,
		snapshot,
		onAssignGroup,
		onRemoveGroup,
		onUpdateCustomName,
		onRemoveCustomName
	}: {
		quote: QuoteModel;
		snapshot: PositionSnapshot;
		onAssignGroup: () => void;
		onRemoveGroup: () => Promise<void>;
		onUpdateCustomName: () => void;
		onRemoveCustomName: () => Promise<void>;
	} = $props();
	
	// Calculate metrics
	let unrealizedGain = $derived(snapshot.currentValue - snapshot.invested);
	let totalProfit = $derived(unrealizedGain + snapshot.realizedGain);
	let positionPerformancePct = $derived(
		snapshot.invested > 0 ? (unrealizedGain / snapshot.invested) * 100 : 0
	);
	let totalReturnPct = $derived(
		snapshot.totalInvestedCash && snapshot.totalInvestedCash > 0
			? (totalProfit / snapshot.totalInvestedCash) * 100
			: snapshot.invested > 0
				? (totalProfit / snapshot.invested) * 100
				: 0
	);

	function formatCurrency(value: number): string {
		return value.toLocaleString(undefined, { style: 'currency', currency: quote.currency });
	}

	function formatNumber(value: number): string {
		return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 6 });
	}

	async function removeGroup() {
		isGroupRemoving = true;
		await onRemoveGroup();
		isGroupRemoving = false;
	}

	async function removeCustomName() {
		isCustomNameRemoving = true;
		await onRemoveCustomName();
		isCustomNameRemoving = false;
	}
</script>

<!-- Type, Name, Date, Market Value, Amount, Fee, Total -->
<div class="card card-reactive no-click @container relative">
	<!-- Menu button in top-right corner -->
	<div class="absolute right-2 top-2">
		<MenuButton>
			<MenuItem icon="fa-solid fa-heading" text="Set Custom Name" onclick={onUpdateCustomName} />
			<MenuItem
				icon="fa-solid fa-trash"
				text="Remove Custom Name"
				onclick={removeCustomName}
				danger={true}
				visible={quote.customName !== ''}
				isLoading={isCustomNameRemoving}
			/>

			<MenuSeparator />

			<MenuItem icon="fa-solid fa-tag" text="Set Group" onclick={onAssignGroup} />
			<MenuItem
				icon="fa-solid fa-trash"
				text="Remove Group"
				onclick={removeGroup}
				danger={true}
				visible={quote.group !== null}
				isLoading={isGroupRemoving}
			/>
		</MenuButton>
	</div>

	<!-- Top Section: Name & Symbol -->
	<div class="flex items-start justify-between gap-3 pr-8">
		<div class="flex flex-col">
			<div class="flex items-baseline gap-2 flex-wrap">
				<h3 class="text-xl font-bold leading-tight">{quote.customName || quote.name}</h3>
				{#if quote.customName}
					<span class="text-xs color-muted italic">({quote.name})</span>
				{/if}
			</div>
			<div class="flex items-center gap-2 mt-0.5">
				<span class="text-xs font-bold color-muted tracking-wider">{quote.symbol}</span>
				<span class="text-xs color-muted">•</span>
				<span class="text-xs uppercase color-secondary font-semibold"
					>{quote.group ? quote.group.name : 'No Group'}</span
				>
			</div>
		</div>
	</div>

	<!-- All Values -->
	<div class="grid grid-cols-2 md:grid-cols-4 gap-x-4 gap-y-3 mt-4">
		<div class="flex flex-col">
			<span class="text-xs color-muted mb-1">Amount</span>
			<span class="text-sm font-semibold leading-none">{formatNumber(snapshot.amount)}</span>
		</div>

		<div class="flex flex-col">
			<span class="text-xs color-muted mb-1">Market Price</span>
			<span class="text-sm font-semibold leading-none">{formatCurrency(snapshot.marketPricePerUnit)}</span>
		</div>

		<div class="flex flex-col">
			<span class="text-xs color-muted mb-1">Current Value</span>
			<span class="text-sm font-bold leading-none">{formatCurrency(snapshot.currentValue)}</span>
		</div>

		<div class="flex flex-col">
			<span class="text-xs color-muted mb-1">Invested</span>
			<span class="text-sm font-semibold leading-none">{formatCurrency(snapshot.invested)}</span>
		</div>

		<div class="flex flex-col">
			<span class="text-xs color-muted mb-1">Total Fees</span>
			<span class="text-sm font-semibold leading-none">{formatCurrency(snapshot.totalFees)}</span>
		</div>
	</div>

	<!-- Performance -->
	<div class="grid grid-cols-1 md:grid-cols-2 gap-x-4 gap-y-3 mt-4 border-t border-t-[#333]">
		<div class="flex flex-col">
			<span class="text-xs color-muted mb-1">Position Gain</span>
			<div class="flex items-baseline gap-1.5">
				<span class="text-sm font-bold leading-none {unrealizedGain < 0 ? 'color-error' : 'color-success'}"
					>{unrealizedGain >= 0 ? '+' : ''} {formatCurrency(unrealizedGain)}</span
				>
				<span class="text-xs font-semibold leading-none {unrealizedGain < 0 ? 'color-error' : 'color-success'}"
					>({positionPerformancePct >= 0 ? '+' : ''} {positionPerformancePct.toFixed(2)}%)</span
				>
			</div>
		</div>

		{#if snapshot.realizedGain !== 0}
			<div class="flex flex-col">
				<span class="text-xs leading-none color-muted mb-1">Realized Gains</span>
				<span
					class="text-sm font-semibold leading-none {snapshot.realizedGain < 0 ? 'color-error' : 'color-success'}"
					>{snapshot.realizedGain >= 0 ? '+' : ''} {formatCurrency(snapshot.realizedGain)}</span
				>
			</div>
		{/if}

		<div class="flex flex-col md:col-span-2 pt-2 border-t border-t-[#444]">
			<span class="text-xs color-muted font-semibold mb-1">Total Profit</span>
			<div class="flex items-baseline gap-1.5">
				<span class="text-lg font-bold leading-none {totalProfit < 0 ? 'color-error' : 'color-success'}"
					>{totalProfit >= 0 ? '+' : ''} {formatCurrency(totalProfit)}</span
				>
				<span class="text-sm font-semibold leading-none {totalProfit < 0 ? 'color-error' : 'color-success'}"
					>({totalReturnPct >= 0 ? '+' : ''} {totalReturnPct.toFixed(2)}%)</span
				>
			</div>
		</div>
	</div>
</div>