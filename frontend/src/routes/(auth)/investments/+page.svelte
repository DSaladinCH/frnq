<script lang="ts">
	import InvestmentForm from '$lib/components/InvestmentForm.svelte';
	import Button from '$lib/components/Button.svelte';
	import Modal from '$lib/components/Modal.svelte';
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import {
		type InvestmentModel,
		createDefaultInvestment,
		investmentValuesValid
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

	// Reactive values that track the store
	let investments = $state(dataStore.investments);
	let quotes = $state(dataStore.quotes);
	let showInvestmentDialog = $state(false);
	let secondaryLoading = $state(dataStore.secondaryLoading);

	// Subscribe to store changes
	$effect(() => {
		const unsubscribe = dataStore.subscribe(() => {
			investments = dataStore.investments;
			quotes = dataStore.quotes;
			secondaryLoading = dataStore.secondaryLoading;
		});
		return unsubscribe;
	});

	let currentInvestment = $state<InvestmentModel>(createDefaultInvestment());
	let currentQuote = $state<QuoteModel | null>(null);
	const locale = navigator.languages?.[0] || navigator.language || 'en-US';

	function getQuoteName(investment: InvestmentModel): string | undefined {
		const quote = quotes.find((quote) => quote.id === investment.quoteId);
		return quote?.customName || quote?.name;
	}

	function formatDate(date: string): string {
		return new Date(date).toLocaleDateString(locale, {
			day: '2-digit',
			month: '2-digit',
			year: 'numeric'
		});
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
			} else {
				await dataStore.updateInvestment(investment);
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
				investment.date
			)}? This action cannot be undone.`
		);

		if (!confirmed) {
			return;
		}

		try {
			await dataStore.deleteInvestment(investment.id);
			onInvestmentDialogClose();
		} catch (error) {
			notify.error('Error deleting investment: ' + error);
			console.error('Error deleting investment:', error);
		}
	}

	function importInvestment() {
		goto('/investments/import');
	}
</script>

<PageHead title="Investments" />

<div class="xs:p-8 p-4">
	<PageTitle title="Investments" icon="fa-solid fa-coins" />

	<div class="mb-3 grid w-full max-w-md grid-cols-2 gap-2">
		<Button
			onclick={newInvestment}
			text="Add Investment"
			icon="fa fa-plus"
			textSize={TextSize.Medium}
			width={ContentWidth.Full}
		/>
		<Button
			onclick={importInvestment}
			text="Import Investment"
			icon="fa fa-file-import"
			textSize={TextSize.Medium}
			width={ContentWidth.Full}
		/>
	</div>

	<div class="investments-list grid gap-2 overflow-y-auto py-1">
		{#each [...investments].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()) as investment}
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
	</div>
</div>

<Modal
	showModal={showInvestmentDialog}
	onClose={onInvestmentDialogClose}
	title={currentInvestment.id === 0 ? 'New Investment' : 'Edit Investment'}
>
	<InvestmentForm bind:investment={currentInvestment} bind:quote={currentQuote} {saveInvestment} />
</Modal>
