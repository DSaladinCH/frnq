<script lang="ts">
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import { InvestmentType, type InvestmentModel } from '$lib/services/investmentService';

	export let investments: InvestmentModel[] = [];
	export let quotes: QuoteModel[] = [];
	export let pushModal: (type: 'Investments' | 'AddInvestment') => void;

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

	// Handler to open AddInvestment modal
	function handleAddInvestment() {
		pushModal('AddInvestment');
	}
</script>

<h1 class="title">Investments</h1>

<div class="mb-3 flex gap-2">
	<button class="btn btn-primary" on:click={handleAddInvestment}>Add Investment</button>
</div>

<div class="investments-list grid gap-2 overflow-y-auto py-1 pr-1">
	{#each investments as investment}
		<div class="investment-item card card-slim card-reactive">
			<div class="grid w-full grid-rows-[auto_1fr] md:grid-rows-[1fr_1fr]">
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
				<div class="grid grid-cols-[1fr_1fr] sm:grid-cols-[1fr_1fr_1fr] md:grid-cols-[repeat(3,_160px)_1fr] color-muted">
					<div class="color-muted sm:hidden">
						<h2>{formatDate(investment.date)}</h2>
					</div>
					<div class="text-right sm:text-left">
						<span>Market: {formatNumber(investment.pricePerUnit)}</span>
					</div>
					<div>
						<span>Amount: {formatNumber(investment.amount)}</span>
					</div>
					<div class="text-right sm:text-left">
						<span>Fees: {formatNumber(investment.totalFees)}</span>
					</div>
					<div class="md:text-right color-default">
						<span>{formatCurrency(investment.pricePerUnit * investment.amount + investment.totalFees)}</span>
					</div>
				</div>
			</div>
		</div>
	{/each}
</div>

<style>
	.title {
		font-size: 1.5em;
		font-weight: bold;
		margin-bottom: 1em;
	}

	.investment-type {
		font-weight: bold;
		text-transform: uppercase;
		background-color: var(--color-secondary);
		text-align: center;
		border-radius: 0.35rem;
		margin-right: 0.5rem;
	}
</style>