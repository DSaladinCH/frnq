<script lang="ts">
	import { InvestmentType, type InvestmentModel } from '$lib/services/investmentService';
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import type { PositionSnapshot } from '$lib/services/positionService';
	import MenuButton from './MenuButton.svelte';
	import MenuItem from './MenuItem.svelte';
	import MenuDivider from './MenuDivider.svelte';
	import Modal from './Modal.svelte';

	let {
		quote,
		snapshot,
		onAssignGroup
	}: { quote: QuoteModel; snapshot: PositionSnapshot; onAssignGroup: () => void } = $props();
	let totalGain = $derived(snapshot.currentValue + snapshot.realizedGain - snapshot.invested);

	let showCustomNameModal = $state(false);
	let customNameInput = $state('');
	let menuButtonRef: any;

	function formatCurrency(value: number): string {
		return value.toLocaleString(undefined, { style: 'currency', currency: quote.currency });
	}

	function formatNumber(value: number): string {
		return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 6 });
	}

	function openSetCustomNameModal() {
		customNameInput = quote.customName || '';
		showCustomNameModal = true;
		menuButtonRef?.closeMenu();
	}

	function openChangeCustomNameModal() {
		customNameInput = quote.customName || '';
		showCustomNameModal = true;
		menuButtonRef?.closeMenu();
	}

	function removeCustomName() {
		quote.customName = undefined;
		menuButtonRef?.closeMenu();
		// Here you would typically also save to backend
	}

	function saveCustomName() {
		if (customNameInput.trim()) {
			quote.customName = customNameInput.trim();
			// Here you would typically also save to backend
		}
		showCustomNameModal = false;
	}

	function closeCustomNameModal() {
		showCustomNameModal = false;
		customNameInput = '';
	}
</script>

<!-- Type, Name, Date, Market Value, Amount, Fee, Total -->
<div class="card card-reactive no-click @container relative grid gap-1">
	<!-- Menu button in top-right corner -->
	<div class="absolute right-2 top-2">
		<MenuButton bind:this={menuButtonRef}>
			{#if quote.customName}
				<MenuItem 
					icon="fa-solid fa-pen"
					text="Change Custom Name"
					onclick={openChangeCustomNameModal}
				/>
				<MenuItem 
					icon="fa-solid fa-trash"
					text="Remove Custom Name"
					onclick={removeCustomName}
					danger={true}
				/>
			{:else}
				<MenuItem 
					icon="fa-solid fa-tag"
					text="Set Custom Name"
					onclick={openSetCustomNameModal}
				/>
			{/if}
		</MenuButton>
	</div>

	<div class="color-muted flex items-center gap-2 text-sm">
		<button class="link-button" onclick={onAssignGroup}>
			<span class="uppercase">{quote.group ? quote.group.name : 'No Group'}</span>
		</button>
		<span>•</span>
		<span class="uppercase">{quote.typeDisposition}</span>
	</div>
	<div class="mb-2 mt-2">
		<div class="leading-none">
			<span class="font-bold">{quote.customName || quote.name}</span>
			{#if quote.customName}
				<span class="color-muted text-sm ml-2">({quote.name})</span>
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

<!-- Custom Name Modal -->
<Modal bind:showModal={showCustomNameModal} onClose={closeCustomNameModal} title="Custom Name">
	<div class="flex flex-col gap-4 min-w-[300px]">
		<div>
			<label for="customName" class="block text-sm font-medium color-muted mb-2">
				Enter a custom name for {quote.name}
			</label>
			<input
				type="text"
				id="customName"
				class="textbox"
				bind:value={customNameInput}
				placeholder="Enter custom name..."
				onkeydown={(e) => e.key === 'Enter' && saveCustomName()}
			/>
		</div>
		<div class="flex gap-2 justify-end">
			<button type="button" class="btn btn-secondary" onclick={closeCustomNameModal}>
				Cancel
			</button>
			<button type="button" class="btn btn-success" onclick={saveCustomName} disabled={!customNameInput.trim()}>
				Save
			</button>
		</div>
	</div>
</Modal>
