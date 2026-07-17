<script lang="ts">
	import FeeForm from '$lib/components/FeeForm.svelte';
	import Button from '$lib/components/Button.svelte';
	import Modal from '$lib/components/Modal.svelte';
	import InfiniteScroll from '$lib/components/InfiniteScroll.svelte';
	import { type GeneralFeeModel, createDefaultFee, feeValuesValid } from '$lib/services/feeService';
	import { dataStore } from '$lib/stores/dataStore';
	import FeeCard from '$lib/components/FeeCard.svelte';
	import PageHead from '$lib/components/PageHead.svelte';
	import { TextSize } from '$lib/types/TextSize';
	import PageTitle from '$lib/components/PageTitle.svelte';
	import { ContentWidth } from '$lib/types/ContentSize';
	import FeeListItem from '$lib/components/FeeListItem.svelte';
	import { notify } from '$lib/services/notificationService';
	import { StylePadding } from '$lib/types/StylePadding';
	import { onMount } from 'svelte';
	import { formatDate, DateFormatType } from '$lib/utils/dateFormat';
	import { infiniteFeesList } from '$lib/stores/infiniteFeesList';

	let showFeeDialog = $state(false);
	let secondaryLoading = $state(dataStore.secondaryLoading);
	let listLoading = $state(false);
	let groups = $state(dataStore.groups);

	// Subscribe to both stores
	let fees = $state<GeneralFeeModel[]>([]);

	$effect(() => {
		const unsubscribe1 = dataStore.subscribe(() => {
			secondaryLoading = dataStore.secondaryLoading;
			groups = dataStore.groups;
		});

		const unsubscribe2 = infiniteFeesList.subscribe(() => {
			fees = infiniteFeesList.items;
			listLoading = infiniteFeesList.loading;
		});

		return () => {
			unsubscribe1();
			unsubscribe2();
		};
	});

	onMount(async () => {
		try {
			await infiniteFeesList.initialize(25);
		} catch {
			notify.error('Failed to load fees. Please refresh the page.');
		}
	});

	let currentFee = $state<GeneralFeeModel>(createDefaultFee());

	function newFee() {
		currentFee = createDefaultFee();
		showFeeDialog = true;
	}

	function openFeeDialog(fee: GeneralFeeModel) {
		currentFee = { ...fee };
		showFeeDialog = true;
	}

	function onFeeDialogClose() {
		showFeeDialog = false;
	}

	async function saveFee(fee: GeneralFeeModel, createNew: boolean) {
		if (secondaryLoading) return;

		if (!feeValuesValid(fee)) {
			notify.error('Please fill in all required fields with valid values.');
			return;
		}

		try {
			if (fee.id === 0) {
				await dataStore.addFee(fee);
				notify.success('Fee created successfully');
			} else {
				await dataStore.updateFee(fee);
				notify.success('Fee updated successfully');
			}

			// Refresh the list to reflect the change
			await infiniteFeesList.refresh();

			if (createNew) {
				newFee();
			} else {
				onFeeDialogClose();
			}
		} catch (error) {
			console.error('Error saving fee:', error);
			notify.error('Error saving fee: ' + error);
		}
	}

	async function deleteFee(fee: GeneralFeeModel) {
		if (secondaryLoading) return;
		onFeeDialogClose();

		if (fee.id === 0) {
			alert('Cannot delete an unsaved fee.');
			return;
		}

		const confirmed = confirm(
			`Are you sure you want to delete the fee of ${formatCurrency(fee.amount)} on ${formatDate(fee.date, DateFormatType.English)}? This action cannot be undone.`
		);

		if (!confirmed) {
			return;
		}

		try {
			await dataStore.deleteFee(fee.id);
			// Refresh the list to remove the deleted fee
			await infiniteFeesList.refresh();
			notify.success('Fee deleted successfully');
		} catch (error) {
			notify.error('Error deleting fee: ' + error);
			console.error('Error deleting fee:', error);
		}
	}

	function formatCurrency(value: number): string {
		return value.toLocaleString(undefined, { style: 'currency', currency: 'CHF' });
	}
</script>

<PageHead title="Fees" />

<div class="flex flex-col h-screen">
	<div class="px-4 pt-4 xs:px-8 xs:pt-8">
		<PageTitle title="Fees" icon="fa-solid fa-money-bill-transfer" />
		<div class="grid w-full max-w-56 grid-cols-1 gap-2 max-lg:mx-auto">
			<Button
				onclick={newFee}
				text="Add Fee"
				icon="fa fa-plus"
				textSize={TextSize.Medium}
				width={ContentWidth.Full}
				padding={StylePadding.None}
			/>
		</div>
	</div>

	<div class="grid gap-2 overflow-y-auto py-1 min-h-0 px-4 xs:px-8 pt-6">
		{#each fees as fee (fee.id)}
			<div class="max-lg:hidden">
				<FeeListItem
					{fee}
					{groups}
					onEdit={() => openFeeDialog(fee)}
					onDelete={() => deleteFee(fee)}
				/>
			</div>

			<div class="lg:hidden">
				<FeeCard {fee} {groups} onEdit={() => openFeeDialog(fee)} onDelete={() => deleteFee(fee)} />
			</div>
		{/each}

		<!-- Infinite scroll component -->
		<InfiniteScroll
			onLoadMore={() => infiniteFeesList.loadMore()}
			hasMore={infiniteFeesList.hasMore}
			loading={listLoading}
			threshold={300}
		/>
	</div>
</div>

<Modal
	showModal={showFeeDialog}
	onClose={onFeeDialogClose}
	title={currentFee.id === 0 ? 'New Fee' : 'Edit Fee'}
>
	<FeeForm bind:fee={currentFee} {saveFee} />
</Modal>
