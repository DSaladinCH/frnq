<script lang="ts">
	import { InvestmentType, type InvestmentModel } from '$lib/services/investmentService';

	type InvestmentTypeIcon = { type: InvestmentType; faIcon: string };

	let {
		investment = $bindable({
			id: 0,
			quoteId: 0,
			type: InvestmentType.Buy,
			pricePerUnit: 0,
			amount: 0,
			totalFees: 0,
			date: getLocalDateTimeString(new Date())
		})
	}: { investment?: InvestmentModel } = $props();

	function getLocalDateTimeString(date: Date): string {
		// Returns YYYY-MM-DDTHH:MM
		const pad = (n: number) => n.toString().padStart(2, '0');
		return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
	}

	let totalInvestment: number = $derived(
		investment.pricePerUnit * investment.amount + investment.totalFees
	);

	$inspect(investment.date);

	const investmentTypes: InvestmentTypeIcon[] = [
		{ type: InvestmentType.Buy, faIcon: 'fa-solid fa-plus-circle' },
		{ type: InvestmentType.Sell, faIcon: 'fa-solid fa-minus-circle' },
		{ type: InvestmentType.Dividend, faIcon: 'fa-solid fa-money-bills' }
	];

	function selectType(type: InvestmentType) {
		investment.type = type;
	}

	function formatCurrency(value: number): string {
		return value.toLocaleString(undefined, { style: 'currency', currency: 'CHF' });
	}
</script>

<h1 class="title">New Investment</h1>

<div class="overflow-y-auto pr-1">
	<div class="grid grid-cols-2 gap-3">
		<div class="grid grid-cols-2 gap-3 sm:grid-cols-3">
			{#each investmentTypes as type}
				<button class="btn-fake" onclick={() => selectType(type.type)}>
					<div
						class="investment-type card card-reactive flex h-[90px] flex-col items-center justify-center"
						class:selected={investment.type === type.type}
					>
						<i class="{type.faIcon} xs:text-[1.75rem] text-[1.5rem]"></i>
						<span class="xs:text-[1.25rem] pt-2 text-[1.125rem] font-bold"
							>{InvestmentType[type.type]}</span
						>
					</div>
				</button>
			{/each}
		</div>

		<div></div>
	</div>
	<div></div>
	<div class="xs:grid-cols-2 xs:gap-3 my-4 grid grid-cols-1 gap-1 sm:grid-cols-3">
		<div class="flex flex-col">
			<span class="text-[1.125rem] font-bold">Market</span>
			<input
				class="textbox"
				type="number"
				step="any"
				min="0"
				required
				bind:value={investment.pricePerUnit}
			/>
		</div>
		<div class="flex flex-col">
			<span class="text-[1.125rem] font-bold">Amount</span>
			<input
				class="textbox"
				type="number"
				step="any"
				min="0"
				required
				bind:value={investment.amount}
			/>
		</div>
		<div class="flex flex-col">
			<span class="text-[1.125rem] font-bold">Fees</span>
			<input
				class="textbox"
				type="number"
				step="any"
				min="0"
				required
				bind:value={investment.totalFees}
			/>
		</div>
		<div class="flex flex-col">
			<span class="text-[1.125rem] font-bold">Date</span>
			<input class="textbox" type="datetime-local" required bind:value={investment.date} />
		</div>
		<!-- Empty placeholder -->
		<div class="xs:max-sm:hidden"></div>
		<div class="flex flex-col">
			<span class="text-[1.125rem] font-bold">Total</span>
			<span class="grow-1 text-(--color-success) flex items-center text-[1.25rem] font-bold">
				{#if totalInvestment === 0}
					<span class="text-(--color-error)">-</span>
				{:else}
					{formatCurrency(totalInvestment)}
				{/if}
			</span>
		</div>
	</div>
</div>

<style>
	.title {
		font-size: 1.5em;
		font-weight: bold;
		margin-bottom: 1em;
	}
</style>
