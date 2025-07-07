<script lang="ts">
	import { InvestmentType } from '$lib/services/investmentService';

	type InvestmentTypeIcon = { type: InvestmentType; faIcon: string };

	let selectedType: InvestmentType = InvestmentType.Buy;

	const investmentTypes: InvestmentTypeIcon[] = [
		{ type: InvestmentType.Buy, faIcon: 'fa-solid fa-plus-circle' },
		{ type: InvestmentType.Sell, faIcon: 'fa-solid fa-minus-circle' },
		{ type: InvestmentType.Dividend, faIcon: 'fa-solid fa-money-bills' }
	];

	function selectType(type: InvestmentType) {
		selectedType = type;
	}
</script>

<h1 class="title">New Investment</h1>

<div class="overflow-y-auto pr-1">
	<div class="grid grid-cols-2 gap-3 sm:grid-cols-3">
		{#each investmentTypes as type}
			<button class="btn-fake" on:click={() => selectType(type.type)}>
				<div
					class="investment-type card card-reactive flex h-[90px] flex-col items-center justify-center sm:h-[115px]"
					class:selected={selectedType === type.type}
				>
					<i class="{type.faIcon} xs:text-[1.75rem] text-[1.5rem] sm:text-[2.5rem]"></i>
					<span class="xs:text-[1.25rem] pt-2 text-[1.125rem] font-bold sm:text-[1.375rem]"
						>{InvestmentType[type.type]}</span
					>
				</div>
			</button>
		{/each}
	</div>
	<div></div>
	<div class="grid grid-cols-1 xs:grid-cols-2 sm:grid-cols-3 gap-1 xs:gap-3 my-4">
		<div class="flex flex-col">
			<span class="font-bold">Market</span>
			<input class="textbox" type="number" />
		</div>
		<div class="flex flex-col">
			<span class="font-bold">Amount</span>
			<input class="textbox" type="number" />
		</div>
		<div class="flex flex-col">
			<span class="font-bold">Fees</span>
			<input class="textbox" type="number" />
		</div>
		<div class="flex flex-col">
			<span class="font-bold">Date</span>
			<input class="textbox" type="datetime-local" />
		</div>
		<!-- Empty placeholder -->
		<div class="xs:max-sm:hidden"></div>
		<div><div class="flex flex-col">
			<span class="font-bold">Total</span>
			<span>300.00</span>
		</div></div>
	</div>
</div>

<style>
	.title {
		font-size: 1.5em;
		font-weight: bold;
		margin-bottom: 1em;
	}
</style>
