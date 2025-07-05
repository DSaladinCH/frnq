<script lang="ts">
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import type { InvestmentModel } from '$lib/services/investmentService';

	let { investments, quotes }: { investments: InvestmentModel[], quotes: QuoteModel[] } = $props();

    function getQuoteName(investment: InvestmentModel): string | undefined {
        return quotes.find(quote => quote.id === investment.quoteId)?.name;
    }
</script>

<h1>Investment List</h1>

<div class="investments-list grid gap-2">
    {#each investments as investment}
        <div class="investment-item card card-slim card-reactive">
            <h2>{getQuoteName(investment)}</h2>
        </div>
    {/each}
</div>

<style>
</style>