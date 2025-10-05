<script lang="ts">
	import Button from '$lib/components/Button.svelte';
	import Modal from '$lib/components/Modal.svelte';
	import PageHead from '$lib/components/PageHead.svelte';
	import PageTitle from '$lib/components/PageTitle.svelte';
	import QuoteCard from '$lib/components/QuoteCard.svelte';
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import { dataStore } from '$lib/stores/dataStore';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import { ContentWidth } from '$lib/types/ContentSize';
	import { TextSize } from '$lib/types/TextSize';

	let quotes = $state(dataStore.quotes);
	let snapshots = $state(dataStore.snapshots);
	let groups = $state(dataStore.groups);

	let assignGroupQuote = $state<QuoteModel | null>(null);
	let assignGroupModalOpen = $state(false);
	let assignGroupId = $state<number | null>(null);
	let secondaryLoading = $state(dataStore.secondaryLoading);

	// Subscribe to store changes
	$effect(() => {
		const unsubscribe = dataStore.subscribe(() => {
			quotes = dataStore.quotes;
			snapshots = dataStore.snapshots;
			groups = dataStore.groups;
			secondaryLoading = dataStore.secondaryLoading;
		});
		return unsubscribe;
	});

	function handleAssignGroup(quote: QuoteModel) {
		assignGroupQuote = quote;
		assignGroupModalOpen = true;
	}

	function closeAssignGroup() {
		assignGroupQuote = null;
		assignGroupModalOpen = false;
		assignGroupId = null;
	}

	async function handleAssignGroupToQuote(groupId: number) {
		if (assignGroupQuote === null) return;
		if (secondaryLoading) return;

		try {
			assignGroupId = groupId;
			await dataStore.assignQuoteToGroup(assignGroupQuote, groupId);
		} catch (error) {
			alert('Error updating quote group: ' + error);
			console.error('Error updating quote group:', error);
		} finally {
			closeAssignGroup();
		}
	}
</script>

<PageHead title="Wallet" />

<div class="xs:p-8 p-4">
	<PageTitle title="Wallet" icon="fa-solid fa-wallet" />

	<div
		class="investments-list 3xl:grid-cols-4 grid gap-2 overflow-y-auto py-1 pr-1 lg:grid-cols-2 2xl:grid-cols-3"
	>
		{#each quotes as quote}
			<QuoteCard
				{quote}
				snapshot={snapshots.filter((s) => s.quoteId === quote.id).slice(-1)[0]}
				onAssignGroup={() => handleAssignGroup(quote)}
			/>
		{/each}
	</div>
</div>

<Modal showModal={assignGroupModalOpen} onClose={closeAssignGroup} title="Assign Group">
	<div class="flex flex-col gap-4">
		{#if assignGroupQuote?.group && assignGroupQuote.group.id}
			<div class="group-item h-10">
				<Button
					text="No Group"
					icon=""
					textSize={TextSize.Medium}
					style={ColorStyle.Secondary}
					width={ContentWidth.Full}
					isLoading={assignGroupId === 0 ? true : false}
					onclick={() => handleAssignGroupToQuote(0)}
				/>
			</div>
		{/if}

		{#each groups.filter((g) => g.id !== assignGroupQuote?.group?.id) as group}
			<div class="group-item h-10">
				<Button
					text={group.name}
					icon=""
					textSize={TextSize.Medium}
					style={ColorStyle.Secondary}
					width={ContentWidth.Full}
					isLoading={assignGroupId === group.id ? true : false}
					onclick={() => handleAssignGroupToQuote(group.id)}
				/>
			</div>
		{/each}
	</div>
</Modal>
