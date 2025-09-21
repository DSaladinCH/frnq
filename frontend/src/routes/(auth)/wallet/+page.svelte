<script lang="ts">
	import Modal from '$lib/components/Modal.svelte';
	import PageHead from '$lib/components/PageHead.svelte';
	import QuoteCard from '$lib/components/QuoteCard.svelte';
	import { dataStore } from '$lib/stores/dataStore';

	let quotes = $state(dataStore.quotes);
	let snapshots = $state(dataStore.snapshots);
	let groups = $state(dataStore.groups);
	let assignGroupQuoteId = $state<number | null>(null);

	let assignGroupModalOpen = $state(true);

	// Subscribe to store changes
	$effect(() => {
		const unsubscribe = dataStore.subscribe(() => {
			quotes = dataStore.quotes;
			snapshots = dataStore.snapshots;
			groups = dataStore.groups;
		});
		return unsubscribe;
	});

	function handleAssignGroup(quoteId: number) {
		assignGroupQuoteId = quoteId;
		assignGroupModalOpen = true;
	}

	function closeAssignGroup() {
		assignGroupQuoteId = null;
		assignGroupModalOpen = false;
	}
</script>

<PageHead title="Wallet" />

<div class="xs:p-8 p-4">
	<h1 class="title mb-4 text-3xl font-bold">Wallet</h1>

	<div
		class="investments-list 3xl:grid-cols-4 grid gap-2 overflow-y-auto py-1 pr-1 lg:grid-cols-2 2xl:grid-cols-3"
	>
		{#each quotes as quote}
			<QuoteCard
				{quote}
				snapshot={snapshots.filter((s) => s.quoteId === quote.id).slice(-1)[0]}
				onAssignGroup={() => handleAssignGroup(quote.id)}
			/>
		{/each}
	</div>
</div>

<Modal showModal={assignGroupModalOpen} onClose={closeAssignGroup}>
	<h2 class="title mb-4 text-2xl font-bold">Assign Group</h2>
	<p>This feature is not implemented yet.</p>
</Modal>