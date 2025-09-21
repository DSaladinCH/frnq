<script lang="ts">
	import InvestmentForm from '$lib/components/InvestmentForm.svelte';
	import Button from '$lib/components/Button.svelte';
	import Modal from '$lib/components/Modal.svelte';
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import {
		InvestmentType,
		type InvestmentModel,
		createDefaultInvestment
	} from '$lib/services/investmentService';
	import { dataStore } from '$lib/stores/dataStore';
	import InvestmentCard from '$lib/components/InvestmentCard.svelte';
	import PageHead from '$lib/components/PageHead.svelte';
	import { TextSize } from '$lib/types/TextSize';
	import PageTitle from '$lib/components/PageTitle.svelte';

	// Reactive values that track the store
	let investments = $state(dataStore.investments);
	let quotes = $state(dataStore.quotes);
	let showInvestmentDialog = $state(false);

	// Subscribe to store changes
	$effect(() => {
		const unsubscribe = dataStore.subscribe(() => {
			investments = dataStore.investments;
			quotes = dataStore.quotes;
		});
		return unsubscribe;
	});

	let currentInvestment = $state<InvestmentModel>(createDefaultInvestment());
	let currentQuote = $state<QuoteModel | null>(null);
	const locale = navigator.languages?.[0] || navigator.language || 'en-US';

	function getQuoteName(investment: InvestmentModel): string | undefined {
		return quotes.find((quote) => quote.id === investment.quoteId)?.name;
	}

	function formatDate(date: string): string {
		return new Date(date).toLocaleDateString(locale, {
			day: '2-digit',
			month: '2-digit',
			year: 'numeric'
		});
	}

	function formatCurrency(value: number): string {
		return value.toLocaleString(undefined, { style: 'currency', currency: 'CHF' });
	}

	function formatNumber(value: number): string {
		return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 6 });
	}

	function getInvestmentType(investment: InvestmentModel): string {
		switch (investment.type) {
			case InvestmentType.Dividend:
				return 'DIV';
			default:
				return InvestmentType[investment.type];
		}
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
		// Validate inputs
		if (investment.pricePerUnit <= 0 || investment.amount <= 0 || !investment.date) {
			// TODO: Implement toast notifications
			alert('Please fill in all required fields with valid values.');
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
			// TODO: Implement toast notifications
			alert('Error saving investment: ' + error);
			console.error('Error saving investment:', error);
		}
	}

	async function deleteInvestment(investment: InvestmentModel) {
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
			// TODO: Implement toast notifications
			alert('Error deleting investment: ' + error);
			console.error('Error deleting investment:', error);
		}
	}
</script>

<PageHead title="Investments" />

<div class="xs:p-8 p-4">
	<PageTitle title="Investments" icon="fa-solid fa-coins" />

	<div class="mb-3 flex gap-2">
		<Button onclick={newInvestment} text="Add Investment" icon="fa fa-plus" textSize={TextSize.Medium} />
	</div>

	<div
		class="investments-list 3xl:grid-cols-4 grid gap-2 overflow-y-auto py-1 pr-1 lg:grid-cols-2 2xl:grid-cols-3"
	>
		{#each investments as investment}
			<InvestmentCard
				{investment}
				quote={quotes.find((q) => q.id === investment.quoteId)!}
				onclick={() => openInvestmentDialog(investment)}
				ondelete={() => deleteInvestment(investment)}
			/>
		{/each}
	</div>
</div>

<Modal showModal={showInvestmentDialog} onClose={onInvestmentDialogClose} title={currentInvestment.id === 0 ? 'New Investment' : 'Edit Investment'}>
	<InvestmentForm bind:investment={currentInvestment} bind:quote={currentQuote} {saveInvestment} />
</Modal>
