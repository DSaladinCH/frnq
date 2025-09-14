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
		currentQuote = quotes.find(q => q.id === investment.quoteId) || null;
		showInvestmentDialog = true;
	}

	function onInvestmentDialogClose() {
		showInvestmentDialog = false;
	}

	async function saveInvestment(investment: InvestmentModel) {
		// Validate inputs
		if (investment.pricePerUnit <= 0 || investment.amount <= 0 || !investment.date) {
			alert('Please fill in all required fields with valid values.');
			return;
		}

		try {
			if (investment.id === 0) {
				await dataStore.addInvestment(investment);
			}
			else {
				await dataStore.updateInvestment(investment);
			}
			
			onInvestmentDialogClose();
		} catch (error) {
			alert('Error saving investment: ' + error);
			console.error('Error saving investment:', error);
		}
	}
</script>

<div class="p-8">
	<h1 class="title mb-4 text-3xl font-bold">Investments</h1>

	<div class="mb-3 flex gap-2">
		<Button onclick={newInvestment} text="Add Investment" icon="fa fa-plus" textSize="text-md" />
	</div>

	<div class="investments-list grid gap-2 overflow-y-auto py-1 pr-1">
		{#each investments as investment}
			<button class="text-left" onclick={() => openInvestmentDialog(investment)}>
				<div class="investment-item card card-slim card-reactive">
					<div class="grid w-full md:hidden">
						<div>
							<div class="investment-type">
								{getInvestmentType(investment)}
							</div>
						</div>
					</div>
					<div class="grid w-full grid-rows-[auto_1fr] max-md:hidden md:grid-rows-[1fr_1fr]">
						<div class="grid grid-cols-[60px_1fr_auto] items-center">
							<div class="investment-type">
								{getInvestmentType(investment)}
							</div>
							<div>
								<h2>{getQuoteName(investment)}</h2>
							</div>
							<div class="color-muted hidden sm:block">
								<h2>{formatDate(investment.date)}</h2>
							</div>
						</div>
						<div
							class="color-muted grid grid-cols-[repeat(auto-fit,_minmax(160px,_1fr))] md:grid-cols-[repeat(3,_160px)_1fr]"
						>
							<div class="color-muted sm:hidden">
								<h2>{formatDate(investment.date)}</h2>
							</div>
							<div>
								<span>Market: {formatNumber(investment.pricePerUnit)}</span>
							</div>
							<div>
								<span>Amount: {formatNumber(investment.amount)}</span>
							</div>
							<div>
								<span>Fees: {formatNumber(investment.totalFees)}</span>
							</div>
							<div class="color-default md:text-right">
								<span
									>{formatCurrency(
										investment.pricePerUnit * investment.amount + investment.totalFees
									)}</span
								>
							</div>
						</div>
					</div>
				</div>
			</button>
		{/each}
	</div>
</div>

<Modal showModal={showInvestmentDialog} onClose={onInvestmentDialogClose}>
	<InvestmentForm bind:investment={currentInvestment} bind:quote={currentQuote} {saveInvestment} />
</Modal>

<style>
	.investment-type {
		font-weight: bold;
		text-transform: uppercase;
		background-color: var(--color-secondary);
		text-align: center;
		border-radius: 0.35rem;
		margin-right: 0.5rem;
	}
</style>
