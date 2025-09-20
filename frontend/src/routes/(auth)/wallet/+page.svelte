<script lang="ts">
	import PageHead from '$lib/components/PageHead.svelte';
	import QuoteCard from '$lib/components/QuoteCard.svelte';
	import { dataStore } from '$lib/stores/dataStore';

	let quotes = $state(dataStore.quotes);
	let snapshots = $state(dataStore.snapshots);
</script>

<PageHead title="Wallet" />

<div class="xs:p-8 p-4">
	<h1 class="title mb-4 text-3xl font-bold">Wallet</h1>

	<div
		class="investments-list 3xl:grid-cols-4 grid gap-2 overflow-y-auto py-1 pr-1 lg:grid-cols-2 2xl:grid-cols-3"
	>
		{#each quotes as quote}
			<QuoteCard {quote} snapshot={snapshots.filter((s) => s.quoteId === quote.id).slice(-1)[0]} />
		{/each}
	</div>
</div>
