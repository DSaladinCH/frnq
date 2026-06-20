<script lang="ts">
	import type { GeneralFeeViewDto } from '$lib/services/positionService';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import Button from './Button.svelte';
	import Input from './Input.svelte';
	import DropDown from './DropDown.svelte';
	import { ContentWidth } from '$lib/types/ContentSize';
	import { StylePadding } from '$lib/types/StylePadding';
	import { dataStore } from '$lib/stores/dataStore';
	import { notify } from '$lib/services/notificationService';

	let isLoading = $state(false);

	let {
		fee = $bindable({
			id: 0,
			userId: '',
			date: getLocalDateString(new Date()),
			amount: 0,
			description: '',
			groupId: null,
			createdAt: new Date().toISOString()
		}),
		saveFee
	}: {
		fee?: Partial<GeneralFeeViewDto>;
		saveFee: (fee: Partial<GeneralFeeViewDto>) => Promise<void>;
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

	function getLocalDateString(date: Date): string {
		const pad = (n: number) => n.toString().padStart(2, '0');
		return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
	}

	function formatCurrency(value: number): string {
		return value.toLocaleString(undefined, { style: 'currency', currency: 'CHF' });
	}

	async function saveChanges() {
		if (!fee.amount || fee.amount <= 0) {
			notify.error('Please enter a valid amount (greater than 0)');
			return;
		}
		if (!fee.date) {
			notify.error('Please select a date');
			return;
		}
		if (!fee.description?.trim()) {
			notify.error('Please enter a description');
			return;
		}

		const feeSnapshot = { ...fee };

		isLoading = true;
		try {
			await saveFee(feeSnapshot);
		} catch (error) {
			console.error('Error saving fee:', error);
		} finally {
			isLoading = false;
		}
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

	<div class="grid">
		<Button
			icon={fee.id === 0 ? 'fa-solid fa-plus' : 'fa-solid fa-floppy-disk'}
			text={fee.id === 0 ? 'Create Fee' : 'Save Changes'}
			style={ColorStyle.Success}
			width={ContentWidth.Full}
			padding={StylePadding.Reduced}
			isLoading={isLoading}
			onclick={() => saveChanges()}
		/>
	</div>
</div>
