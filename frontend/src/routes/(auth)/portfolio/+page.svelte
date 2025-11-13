<script lang="ts">
	import { tick } from 'svelte';
	import PortfolioChart from '$lib/components/PortfolioChart.svelte';
	import PositionCard from '$lib/components/PositionCard.svelte';
	import { type PositionSnapshot } from '$lib/services/positionService';
	import { dataStore } from '$lib/stores/dataStore';
	import PageHead from '$lib/components/PageHead.svelte';

	// Reactive values that track the store
	let snapshots = $state(dataStore.snapshots);
	let quotes = $state(dataStore.quotes);
	let loading = $state(dataStore.loading);

	// Subscribe to store changes
	$effect(() => {
		const unsubscribe = dataStore.subscribe(() => {
			snapshots = dataStore.snapshots;
			quotes = dataStore.quotes;
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
		const unrealizedGain = (last?.currentValue ?? 0) - (last?.invested ?? 0);
		return {
			invested: last?.invested ?? 0,
			currentValue: last?.currentValue ?? 0,
			totalValue: (last?.currentValue ?? 0) + (last?.realizedGain ?? 0),
			realized: last?.realizedGain ?? 0,
			totalProfit: unrealizedGain + (last?.realizedGain ?? 0),
			unrealizedGain: unrealizedGain,
			totalInvestedCash: last?.totalInvestedCash ?? (last?.invested ?? 0)
		};
	}

	// Helper to sum all values in a group (sum of last snapshot of each quote)
	function getGroupSummaryLast(quotes: Record<number, PositionSnapshot[]>) {
		const lastSnaps = Object.values(quotes)
			.map((snaps) => snaps[snaps.length - 1])
			.filter(Boolean);
		const invested = lastSnaps.reduce((sum, s) => sum + (s.invested ?? 0), 0);
		const currentValue = lastSnaps.reduce((sum, s) => sum + (s.currentValue ?? 0), 0);
		const realized = lastSnaps.reduce((sum, s) => sum + (s.realizedGain ?? 0), 0);
		const unrealizedGain = currentValue - invested;
		return {
			invested,
			currentValue,
			realized,
			totalValue: currentValue + realized,
			totalProfit: unrealizedGain + realized,
			unrealizedGain,
			totalInvestedCash: lastSnaps.reduce((sum, s) => sum + (s.totalInvestedCash ?? s.invested ?? 0), 0)
		};
	}

	// Helper to get quote by quoteId
	function getQuoteById(quoteId: number) {
		return quotes.find((q) => q.id === quoteId);
	}

	// Helper to get display name for a quoteId
	function getQuoteDisplayName(quoteId: number) {
		const quote = getQuoteById(quoteId);
		if (!quote) return 'Unknown Quote';
		return quote?.customName ? quote.customName : quote?.name;
	}

	// State for filtering (use Svelte runes for reactivity)
	let filterMode = $state<'full' | 'group' | 'quote'>('full');
	let filterGroupId = $state<string | null>(null);
	let filterQuoteId = $state<number | null>(null);
	let chartPeriod = $state<string>('3m'); // Track the chart's selected period

	// Compute filtered snapshots for the chart
	function filterLeadingZeroSnapshots(snaps: PositionSnapshot[]): PositionSnapshot[] {
		// Find the first index where at least one of the values is non-zero
		const firstNonZeroIdx = snaps.findIndex(
			(s) =>
				(s.invested ?? 0) !== 0 ||
				(s.currentValue ?? 0) !== 0 ||
				(s.realizedGain ?? 0) !== 0
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

	function handlePeriodChange(period: string) {
		chartPeriod = period;
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

	// Helper to check if a quote has any active positions based on the chart's selected period
	function hasActivePosition(snapshots: PositionSnapshot[]): boolean {
		if (snapshots.length === 0) return false;
		
		// Calculate the date range based on the chart's selected period
		const now = new Date();
		let fromDate: Date;
		
		switch (chartPeriod) {
			case '1w':
				fromDate = new Date(now);
				fromDate.setDate(now.getDate() - 7);
				break;
			case '1m':
				fromDate = new Date(now);
				fromDate.setMonth(now.getMonth() - 1);
				break;
			case '3m':
				fromDate = new Date(now);
				fromDate.setMonth(now.getMonth() - 3);
				break;
			case 'ytd':
				fromDate = new Date(now.getFullYear(), 0, 1);
				break;
			case 'all':
				// For 'all', include all snapshots
				return snapshots.some(snap => snap.amount > 0);
			default:
				// Default to 3 months
				fromDate = new Date(now);
				fromDate.setMonth(now.getMonth() - 3);
		}
		
		// Remove time from fromDate for comparison
		fromDate = new Date(fromDate.getFullYear(), fromDate.getMonth(), fromDate.getDate());
		
		const periodSnapshots = snapshots.filter(snap => new Date(snap.date) >= fromDate);
		return periodSnapshots.some(snap => snap.amount > 0);
	}

	// Helper to check if a group has any currently active positions
	function hasActivePositionsInGroup(quotes: Record<number, PositionSnapshot[]>): boolean {
		return Object.values(quotes).some(snaps => hasActivePosition(snaps));
	}

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
				...Object.entries(groupedSnapshotsValue.groups)
					.filter(([groupId, { quotes }]) => hasActivePositionsInGroup(quotes))
					.map(([groupId, { name: groupName, quotes }]) => ({
						type: 'group' as const,
						groupId,
						groupName,
						quotes
					})),
				...Object.entries(groupedSnapshotsValue.ungrouped)
					.filter(([quoteKey, snaps]) => hasActivePosition(snaps))
					.map(([quoteKey, snaps]) => ({
						type: 'quote' as const,
						quoteKey: +quoteKey,
						snaps
					}))
			];
		} else if (filterModeValue === 'group' && filterGroupIdValue) {
			// Only show quotes of the selected group that have active positions
			const group = groupedSnapshotsValue.groups[filterGroupIdValue];
			if (!group) return [];
			return Object.entries(group.quotes)
				.filter(([quoteKey, snaps]) => hasActivePosition(snaps))
				.map(([quoteKey, snaps]) => ({
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
		summary: { invested: number; currentValue: number; totalValue: number; realized: number; totalProfit: number };
		title: string;
		onView?: (() => void) | undefined;
		isActiveQuote?: boolean;
		viewLabel?: string;
		profitClass?: string;
	};

	function getCardProps(card: Card): PositionCardProps {
		if (card.type === 'group') {
			// Filter quotes to only include those with active positions in the selected period
			const filteredQuotes: Record<number, PositionSnapshot[]> = {};
			Object.entries(card.quotes).forEach(([quoteKey, snaps]) => {
				if (hasActivePosition(snaps)) {
					filteredQuotes[+quoteKey] = snaps;
				}
			});
			const summary = getGroupSummaryLast(filteredQuotes);
			const profit = summary.totalProfit;
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
			const profit = summary.totalProfit;
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

<PageHead title="Portfolio" />

{#if snapshots.length === 0}
	<p>No data available.</p>
{:else if snapshots.length}
	<PortfolioChart snapshots={filteredSnapshots()} onPeriodChange={handlePeriodChange} />

	<div
		class="quote-groups mx-auto mb-3 mt-4 grid w-full grid-cols-[repeat(auto-fit,_minmax(300px,_450px))] justify-center gap-5 px-3 xl:w-4/5"
	>
		{#if filterMode !== 'full'}
			<PositionCard
				type="group"
				title="<i class='fa-solid fa-arrow-left'></i> Back to full view"
				summary={{ invested: 0, currentValue: 0, totalValue: 0, realized: 0, totalProfit: 0 }}
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
