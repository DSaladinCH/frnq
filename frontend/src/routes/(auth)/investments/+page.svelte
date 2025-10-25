<script lang="ts">
	import InvestmentForm from '$lib/components/InvestmentForm.svelte';
	import Button from '$lib/components/Button.svelte';
	import Modal from '$lib/components/Modal.svelte';
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import {
		type InvestmentModel,
		createDefaultInvestment,
		investmentValuesValid,
		type InvestmentFilters
	} from '$lib/services/investmentService';
	import { dataStore } from '$lib/stores/dataStore';
	import InvestmentCard from '$lib/components/InvestmentCard.svelte';
	import PageHead from '$lib/components/PageHead.svelte';
	import { TextSize } from '$lib/types/TextSize';
	import PageTitle from '$lib/components/PageTitle.svelte';
	import { ContentWidth } from '$lib/types/ContentSize';
	import { goto } from '$app/navigation';
	import InvestmentListItem from '$lib/components/InvestmentListItem.svelte';
	import { notify } from '$lib/services/notificationService';
	import { InfiniteInvestmentsList } from '$lib/stores/infiniteInvestmentsList';
	import InfiniteScroll from '$lib/components/InfiniteScroll.svelte';
	import { onMount } from 'svelte';
	import { formatDate } from '$lib/utils/dateFormat';
	import { userPreferences } from '$lib/stores/userPreferences';
	import InvestmentFiltersComponent from '$lib/components/InvestmentFilters.svelte';
	import { StylePadding } from '$lib/types/StylePadding';

	// Create infinite scroll list
	const investmentsList = new InfiniteInvestmentsList();

	// Reactive values that track the store
	let investments = $state(investmentsList.items);
	let quotes = $state(dataStore.quotes);
	let groups = $state(dataStore.groups);
	let showInvestmentDialog = $state(false);
	let secondaryLoading = $state(dataStore.secondaryLoading);
	let listLoading = $state(investmentsList.loading);
	let hasMore = $state(investmentsList.hasMore);
	let preferences = $state($userPreferences);
	let filters = $state<InvestmentFilters>({});

	// Subscribe to store changes
	$effect(() => {
		const unsubscribeData = dataStore.subscribe(() => {
			quotes = dataStore.quotes;
			groups = dataStore.groups;
			secondaryLoading = dataStore.secondaryLoading;
		});

		const unsubscribeList = investmentsList.subscribe(() => {
			investments = investmentsList.items;
			listLoading = investmentsList.loading;
			hasMore = investmentsList.hasMore;
		});

		const unsubscribePrefs = userPreferences.subscribe((prefs) => {
			preferences = prefs;
		});

		return () => {
			unsubscribeData();
			unsubscribeList();
			unsubscribePrefs();
		};
	});

	onMount(async () => {
		// Initialize the infinite scroll list
		await investmentsList.initialize(25);
	});

	let currentInvestment = $state<InvestmentModel>(createDefaultInvestment());
	let currentQuote = $state<QuoteModel | null>(null);

	function getQuoteName(investment: InvestmentModel): string | undefined {
		const quote = quotes.find((quote) => quote.id === investment.quoteId);
		return quote?.customName || quote?.name;
	}

	function formatNumber(value: number): string {
		return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 6 });
	}

	function newInvestment() {
		currentInvestment = createDefaultInvestment();
		currentQuote = null;
		showInvestmentDialog = true;
	}

	function openInvestmentDialog(investment: InvestmentModel) {
		currentInvestment = { ...investment };
		currentQuote = quotes.find((q) => q.id === investment.quoteId) || null;
		currentInvestment.providerId = currentQuote!.providerId;
		currentInvestment.quoteSymbol = currentQuote!.symbol;
		showInvestmentDialog = true;
	}

	function onInvestmentDialogClose() {
		showInvestmentDialog = false;
	}

	async function saveInvestment(investment: InvestmentModel) {
		if (secondaryLoading) return;
		// Validate inputs
		if (!investmentValuesValid(investment)) {
			notify.error('Please fill in all required fields with valid values.');
			return;
		}

		try {
			if (investment.id === 0) {
				await dataStore.addInvestment(investment);
				// Refresh the list to include the new investment
				await investmentsList.refresh();
			} else {
				await dataStore.updateInvestment(investment);
				// Refresh the list to update the investment
				await investmentsList.refresh();
			}

			onInvestmentDialogClose();
		} catch (error) {
			console.error('Error saving investment:', error);
			notify.error('Error saving investment: ' + error);
		}
	}

	async function deleteInvestment(investment: InvestmentModel) {
		if (secondaryLoading) return;
		onInvestmentDialogClose();

		if (investment.id === 0) {
			alert('Cannot delete an unsaved investment.');
			return;
		}

		// TODO: Implement dialog component for confirmation
		const confirmed = confirm(
			`Are you sure you want to delete the investment of ${formatNumber(
				investment.amount
			)} units of ${getQuoteName(investment) || 'unknown quote'} on ${formatDate(
				investment.date,
				preferences.dateFormat
			)}? This action cannot be undone.`
		);

		if (!confirmed) {
			return;
		}

		try {
			await dataStore.deleteInvestment(investment.id);
			// Refresh the list to remove the deleted investment
			await investmentsList.refresh();
			onInvestmentDialogClose();
		} catch (error) {
			notify.error('Error deleting investment: ' + error);
			console.error('Error deleting investment:', error);
		}
	}

	async function loadMoreInvestments() {
		await investmentsList.loadMore();
	}

	function importInvestment() {
		goto('/investments/import');
	}

	async function handleApplyFilters() {
		await investmentsList.updateFilters(filters);
	}

	async function handleClearFilters() {
		filters = {};
		await investmentsList.updateFilters(filters);
	}
</script>

<PageHead title="Investments" />

<div class="flex flex-col h-screen">
	<div class="px-4 pt-4 xs:px-8 xs:pt-8">
		<PageTitle title="Investments" icon="fa-solid fa-coins" />
		<div class="grid w-full max-w-md grid-cols-2 gap-2">
			<Button
				onclick={newInvestment}
				text="Add Investment"
				icon="fa fa-plus"
				textSize={TextSize.Medium}
				width={ContentWidth.Full}
				padding={StylePadding.None}
			/>
			<Button
				onclick={importInvestment}
				text="Import Investment"
				icon="fa fa-file-import"
				textSize={TextSize.Medium}
				width={ContentWidth.Full}
				padding={StylePadding.None}
			/>
		</div>

		<!-- Investment Filters -->
		<InvestmentFiltersComponent
			bind:filters
			{quotes}
			{groups}
			onApplyFilters={handleApplyFilters}
			onClearFilters={handleClearFilters}
		/>
	</div>

	<div class="investments-list grid gap-2 overflow-y-auto py-1 min-h-0 px-4 xs:px-8">
		{#each investments as investment}
			<div class="max-lg:hidden">
				<InvestmentListItem
					{investment}
					quote={quotes.find((q) => q.id === investment.quoteId)!}
					onclick={() => openInvestmentDialog(investment)}
					ondelete={() => deleteInvestment(investment)}
				/>
			</div>

			<div class="lg:hidden">
				<InvestmentCard
					{investment}
					quote={quotes.find((q) => q.id === investment.quoteId)!}
					onclick={() => openInvestmentDialog(investment)}
					ondelete={() => deleteInvestment(investment)}
				/>
			</div>
		{/each}
		
		<!-- Infinite scroll component -->
		<InfiniteScroll 
			onLoadMore={loadMoreInvestments} 
			{hasMore} 
			loading={listLoading}
			threshold={300}
		/>
	</div>
</div>

<Modal
	showModal={showInvestmentDialog}
	onClose={onInvestmentDialogClose}
	title={currentInvestment.id === 0 ? 'New Investment' : 'Edit Investment'}
>
	<InvestmentForm bind:investment={currentInvestment} bind:quote={currentQuote} {saveInvestment} />
</Modal>
