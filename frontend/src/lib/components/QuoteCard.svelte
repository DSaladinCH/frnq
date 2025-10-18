<script lang="ts">
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import type { PositionSnapshot } from '$lib/services/positionService';
	import MenuButton from './MenuButton.svelte';
	import MenuItem from './MenuItem.svelte';

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
	let totalGain = $derived(snapshot.currentValue + snapshot.realizedGain - snapshot.invested);

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
<div class="card card-reactive no-click @container relative grid gap-1">
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

	<div class="color-muted flex items-center gap-2 text-sm">
		<span class="uppercase color-secondary">{quote.group ? quote.group.name : 'No Group'}</span>
		<span>•</span>
		<span class="uppercase">{quote.typeDisposition}</span>
	</div>
	<div class="mb-2 mt-2">
		<div class="leading-none">
			<span class="font-bold">{quote.customName || quote.name}</span>
			{#if quote.customName}
				<span class="color-muted ml-2 text-sm">({quote.name})</span>
			{/if}
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
