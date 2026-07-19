<script lang="ts">
	import { feeValuesValid, createDefaultFee, type GeneralFeeModel } from '$lib/services/feeService';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import Button from './Button.svelte';
	import Input from './Input.svelte';
	import DropDown from './DropDown.svelte';
	import { ContentWidth } from '$lib/types/ContentSize';
	import { StylePadding } from '$lib/types/StylePadding';
	import { dataStore } from '$lib/stores/dataStore';
	import { notify } from '$lib/services/notificationService';

	let isLoading1 = $state(false);
	let isLoading2 = $state(false);

	let {
		fee = $bindable(createDefaultFee()),
		saveFee
	}: {
		fee?: GeneralFeeModel;
		saveFee: (fee: GeneralFeeModel, createNew: boolean) => Promise<void>;
	} = $props();

	// Get groups from dataStore
	let groups = $derived(dataStore.groups);
	let groupOptions = $derived.by(() => {
		const options = [{ value: '', label: 'Portfolio-level Fee' }];
		groups.forEach((group) => {
			options.push({ value: group.id.toString(), label: group.name });
		});
		return options;
	});

	let selectedGroupId = $state<string>('');

	$effect.pre(() => {
		// Sync fee.groupId -> selectedGroupId whenever fee changes
		selectedGroupId = fee.groupId?.toString() || '';
	});

	function handleGroupChange(value: string) {
		// Directly update fee.groupId when dropdown changes
		fee.groupId = value ? parseInt(value) : null;
		selectedGroupId = value;
	}

	async function saveChanges(createNew: boolean = false) {
		if (!feeValuesValid(fee)) {
			notify.error('Please fill in all required fields with valid values.');
			return;
		}

		// Create a snapshot of the fee to prevent reactive updates from affecting the saved data
		const feeSnapshot = { ...fee };

		if (createNew) { isLoading2 = true; } else { isLoading1 = true; }

		await saveFee(feeSnapshot, createNew);

		isLoading1 = false;
		isLoading2 = false;
	}
</script>

<div class="grid gap-4 pr-1">
	<div class="grid grid-cols-1 gap-3 xs:grid-cols-2 sm:grid-cols-3">
		<div class="flex flex-col">
			<Input
				title="Amount"
				type="number"
				step="0.01"
				min={0.01}
				required
				bind:value={fee.amount}
			/>
		</div>
		<div class="flex flex-col">
			<Input
				title="Date"
				type="date"
				required
				bind:value={fee.date}
			/>
		</div>
		<div class="flex flex-col">
			<Input
				title="Description"
				type="text"
				required
				bind:value={fee.description}
			/>
		</div>
	</div>

	<div class="grid">
		<DropDown
			title="Assigned To"
			options={groupOptions}
			value={selectedGroupId}
			onchange={(value) => handleGroupChange(value)}
			placeholder="Select group or portfolio-level"
		/>
	</div>

	<div class="grid {fee.id === 0 ? 'xs:grid-cols-2' : 'xs:grid-cols-1'} gap-2">
		<Button
			icon={fee.id === 0 ? 'fa-solid fa-plus' : 'fa-solid fa-floppy-disk'}
			text={fee.id === 0 ? 'Create Fee' : 'Save Changes'}
			style={ColorStyle.Success}
			width={ContentWidth.Full}
			padding={StylePadding.Reduced}
			isLoading={isLoading1}
			disabled={isLoading2}
			onclick={() => saveChanges()}
		/>

		{#if fee.id === 0}
			<Button
				icon="fa-solid fa-plus"
				text="Create and New"
				style={ColorStyle.Secondary}
				width={ContentWidth.Full}
				padding={StylePadding.Reduced}
				isLoading={isLoading2}
				disabled={isLoading1}
				onclick={() => saveChanges(true)}
			/>
		{/if}
	</div>
</div>
