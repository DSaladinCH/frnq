<script lang="ts">
	import { tick } from 'svelte';
	import PortfolioChart from '$lib/components/PortfolioChart.svelte';
	import PositionCard from '$lib/components/PositionCard.svelte';
	import { type PositionSnapshot } from '$lib/services/positionService';
	import type { QuoteModel } from '$lib/Models/QuoteModel';
	import { type InvestmentModel } from '$lib/services/investmentService';
	import { dataStore } from '$lib/stores/dataStore';

	let error = $state<string | null>(null);

	// Reactive values that track the store
	let snapshots = $state(dataStore.snapshots);
	let quotes = $state(dataStore.quotes);
	let investments = $state(dataStore.investments);
	let loading = $state(dataStore.loading);

	// Subscribe to store changes
	$effect(() => {
		const unsubscribe = dataStore.subscribe(() => {
			snapshots = dataStore.snapshots;
			quotes = dataStore.quotes;
			investments = dataStore.investments;
			loading = dataStore.loading;
		});
		return unsubscribe;
	});

	// Group by quote group (from quote), then by quoteId
	let groupedSnapshots = $derived(() => {
		const groups: Record<string, { name: string; quotes: Record<number, PositionSnapshot[]> }> = {};
		const ungrouped: Record<number, PositionSnapshot[]> = {};

		for (const snap of snapshots) {
			const quote = quotes.find((q) => q.id === snap.quoteId);
			const groupId = quote?.group?.id;
			const groupName = quote?.group?.name;
			const quoteKey = snap.quoteId;

			if (groupId && groupName) {
				if (!groups[groupId]) groups[groupId] = { name: groupName, quotes: {} };
				if (!groups[groupId].quotes[quoteKey]) groups[groupId].quotes[quoteKey] = [];
				groups[groupId].quotes[quoteKey].push(snap);
			} else {
				if (!ungrouped[quoteKey]) ungrouped[quoteKey] = [];
				ungrouped[quoteKey].push(snap);
			}
		}
		return { groups, ungrouped };
	});

	// Helper to sum values for a list of snapshots (only the last snapshot of each quote)
	function getSummaryFromLastSnapshots(snaps: PositionSnapshot[]) {
		// Only use the last snapshot in the array
		const last = snaps[snaps.length - 1];
		return {
			invested: last?.invested ?? 0,
			totalValue: last?.totalValue ?? 0,
			realized: last?.realizedGain ?? 0,
			unrealized: last?.unrealizedGain ?? 0
		};
	}

	// Helper to sum all values in a group (sum of last snapshot of each quote)
	function getGroupSummaryLast(quotes: Record<number, PositionSnapshot[]>) {
		const lastSnaps = Object.values(quotes)
			.map((snaps) => snaps[snaps.length - 1])
			.filter(Boolean);
		return {
			invested: lastSnaps.reduce((sum, s) => sum + (s.invested ?? 0), 0),
			totalValue: lastSnaps.reduce((sum, s) => sum + (s.totalValue ?? 0), 0),
			realized: lastSnaps.reduce((sum, s) => sum + (s.realizedGain ?? 0), 0),
			unrealized: lastSnaps.reduce((sum, s) => sum + (s.unrealizedGain ?? 0), 0)
		};
	}

	// Helper to get quote by quoteId
	function getQuoteById(quoteId: number) {
		return quotes.find((q) => q.id === quoteId);
	}

	// Helper to get display name for a quoteId
	function getQuoteDisplayName(quoteId: number) {
		const quote = getQuoteById(quoteId);
		return quote ? quote.name : `Quote #${quoteId}`;
	}

	// State for filtering (use Svelte runes for reactivity)
	let filterMode = $state<'full' | 'group' | 'quote'>('full');
	let filterGroupId = $state<string | null>(null);
	let filterQuoteId = $state<number | null>(null);

	// Compute filtered snapshots for the chart
	function filterLeadingZeroSnapshots(snaps: PositionSnapshot[]): PositionSnapshot[] {
		// Find the first index where at least one of the values is non-zero
		const firstNonZeroIdx = snaps.findIndex(
			(s) =>
				(s.invested ?? 0) !== 0 ||
				(s.totalValue ?? 0) !== 0 ||
				(s.realizedGain ?? 0) !== 0 ||
				(s.unrealizedGain ?? 0) !== 0
		);
		return firstNonZeroIdx === -1 ? [] : snaps.slice(firstNonZeroIdx);
	}

	let filteredSnapshots = $derived(() => {
		let snaps: PositionSnapshot[];
		if (filterMode === 'full') snaps = snapshots;
		else if (filterMode === 'group' && filterGroupId) {
			const group = groupedSnapshots().groups[filterGroupId];
			snaps = group ? Object.values(group.quotes).flat() : [];
		} else if (filterMode === 'quote' && filterQuoteId != null) {
			snaps = snapshots.filter((s) => s.quoteId === filterQuoteId);
		} else snaps = snapshots;
		return filterLeadingZeroSnapshots(snaps);
	});

	// UI handlers (update state)
	function handleGroupView(groupId: string) {
		filterMode = 'group';
		filterGroupId = groupId;
		filterQuoteId = null;
		tick(); // ensure reactivity
	}

	function handleQuoteView(quoteId: number, groupId?: string) {
		filterMode = 'quote';
		filterQuoteId = quoteId;
		filterGroupId = groupId ?? null;
		tick();
	}

	function handleBackToFullView() {
		filterMode = 'full';
		filterGroupId = null;
		filterQuoteId = null;
		tick();
	}

	function handleBackToGroupView() {
		if (filterGroupId) {
			filterMode = 'group';
			filterQuoteId = null;
		} else {
			handleBackToFullView();
		}
		tick();
	}

	// Helper to get all quote cards to render based on filter
	interface GroupCard {
		type: 'group';
		groupId: string;
		groupName: string;
		quotes: Record<number, PositionSnapshot[]>;
	}
	interface QuoteCard {
		type: 'quote';
		quoteKey: number;
		snaps: PositionSnapshot[];
		groupId?: string;
		isActiveQuote?: boolean;
	}
	type Card = GroupCard | QuoteCard;

	function getQuoteCards(
		groupedSnapshotsValue: {
			groups: Record<string, { name: string; quotes: Record<number, PositionSnapshot[]> }>;
			ungrouped: Record<number, PositionSnapshot[]>;
		},
		filterModeValue: 'full' | 'group' | 'quote',
		filterGroupIdValue: string | null,
		filterQuoteIdValue: number | null
	): Card[] {
		if (filterModeValue === 'full') {
			return [
				...Object.entries(groupedSnapshotsValue.groups).map(
					([groupId, { name: groupName, quotes }]) => ({
						type: 'group' as const,
						groupId,
						groupName,
						quotes
					})
				),
				...Object.entries(groupedSnapshotsValue.ungrouped).map(([quoteKey, snaps]) => ({
					type: 'quote' as const,
					quoteKey: +quoteKey,
					snaps
				}))
			];
		} else if (filterModeValue === 'group' && filterGroupIdValue) {
			// Only show quotes of the selected group
			const group = groupedSnapshotsValue.groups[filterGroupIdValue];
			if (!group) return [];
			return Object.entries(group.quotes).map(([quoteKey, snaps]) => ({
				type: 'quote' as const,
				quoteKey: +quoteKey,
				snaps,
				groupId: filterGroupIdValue
			}));
		} else if (filterModeValue === 'quote' && filterQuoteIdValue != null) {
			let snaps = null;
			let groupId: string | undefined = filterGroupIdValue ?? undefined;
			if (groupId && groupedSnapshotsValue.groups[groupId]?.quotes[filterQuoteIdValue]) {
				snaps = groupedSnapshotsValue.groups[groupId].quotes[filterQuoteIdValue];
			} else if (groupedSnapshotsValue.ungrouped[filterQuoteIdValue]) {
				snaps = groupedSnapshotsValue.ungrouped[filterQuoteIdValue];
				groupId = undefined; // assign undefined for type compatibility
			}
			if (snaps) {
				return [
					{
						type: 'quote' as const,
						quoteKey: filterQuoteIdValue,
						snaps,
						groupId,
						isActiveQuote: true
					}
				];
			}
			return [];
		}
		return [];
	}

	// Helper to get card summary and props for PositionCard
	type PositionCardProps = {
		type: 'group' | 'quote';
		groupName?: string;
		summary: { invested: number; totalValue: number; realized: number; unrealized: number };
		title: string;
		onView?: (() => void) | undefined;
		isActiveQuote?: boolean;
		viewLabel?: string;
		profitClass?: string;
	};

	function getCardProps(card: Card): PositionCardProps {
		if (card.type === 'group') {
			const summary = getGroupSummaryLast(card.quotes);
			const profit = summary.realized + summary.unrealized;
			return {
				type: 'group',
				groupName: card.groupName,
				title: card.groupName,
				summary,
				onView: () => handleGroupView(card.groupId),
				isActiveQuote: false,
				viewLabel: 'Show only this group',
				profitClass: profit > 0 ? 'profit-positive' : profit < 0 ? 'profit-negative' : ''
			};
		} else {
			const summary = getSummaryFromLastSnapshots(card.snaps);
			const profit = summary.realized + summary.unrealized;
			return {
				type: 'quote',
				title: getQuoteDisplayName(card.quoteKey),
				summary,
				onView: card.isActiveQuote
					? card.groupId
						? handleBackToGroupView
						: handleBackToFullView
					: () => handleQuoteView(card.quoteKey, card.groupId),
				isActiveQuote: !!card.isActiveQuote,
				viewLabel: card.isActiveQuote
					? card.groupId
						? 'Cancel quote filter'
						: 'Back to full view'
					: 'Show only this quote',
				profitClass: profit > 0 ? 'profit-positive' : profit < 0 ? 'profit-negative' : ''
			};
		}
	}

	// Reactive cards array for the template
	let cards = $derived(getQuoteCards(groupedSnapshots(), filterMode, filterGroupId, filterQuoteId));
</script>

{#if snapshots.length === 0}
	<p>No data available.</p>
{:else if snapshots.length}
	<PortfolioChart snapshots={filteredSnapshots()} />

	<div
		class="quote-groups mx-auto mb-3 mt-4 grid w-full grid-cols-[repeat(auto-fit,_minmax(300px,_450px))] justify-center gap-5 px-3 xl:w-4/5"
	>
		{#if filterMode !== 'full'}
			<PositionCard
				type="group"
				title="<i class='fa-solid fa-arrow-left'></i> Back to full view"
				summary={{ invested: 0, totalValue: 0, realized: 0, unrealized: 0 }}
				onView={handleBackToFullView}
				isActiveQuote={false}
				viewLabel="Back to full view"
				profitClass=""
				minimal={true}
			/>
		{/if}
		{#each cards as card (card.type === 'group' ? `group-${card.groupId}` : card.groupId ? `quote-${card.groupId}-${card.quoteKey}` : `quote-${card.quoteKey}`)}
			<PositionCard {...getCardProps(card)} />
		{/each}
	</div>
{/if}

<style>
	.quote-groups {
		margin-bottom: 4.5rem;
	}
</style>
