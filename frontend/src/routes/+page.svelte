<script lang="ts">
	import { onMount, tick } from 'svelte';
	import PortfolioChart from '$lib/components/PortfolioChart.svelte';
	import PositionCard from '$lib/components/PositionCard.svelte';
	import {
		getPositionSnapshots,
		type PositionSnapshot,
		type QuoteModel,
		type PositionsResponse
	} from '$lib/services/positionService';

	let snapshots = $state<PositionSnapshot[]>([]);
	let quotes = $state<QuoteModel[]>([]);
	let loading = $state(true);
	let error = $state<string | null>(null);

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

	let showLoading = $state(true);
	let fadeOut = $state(false);

	$effect(() => {
		if (!loading && showLoading && !fadeOut) {
			// Start fade-out
			fadeOut = true;
		}
	});

	onMount(async () => {
		loading = true;
		try {
			const data: PositionsResponse = await getPositionSnapshots(null, null);
			snapshots = data.snapshots;
			quotes = data.quotes;
			error = null;
		} catch (e) {
			error = (e as Error).message;
		} finally {
			loading = false;
		}
	});

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
					{ type: 'quote' as const, quoteKey: filterQuoteIdValue, snaps, groupId, isActiveQuote: true }
				];
			}
			return [];
		}
		return [];
	}

	// Helper to get card summary and props for PositionCard
	import type PositionCardType from '$lib/components/PositionCard.svelte';
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

	// Floating Action Button state
	let fabOpen = $state(false);
	const fabActions = [
		{ icon: 'fa-solid fa-plus', label: 'Add Investment', onClick: () => alert('Add action') },
		{ icon: 'fa-solid fa-list-ul', label: 'Show Investments', onClick: () => alert('Edit action') }
	];

	// Helper to get action position: pop up from left bottom on desktop, right bottom on mobile
	function getFabActionStyle(i: number, total: number, open: boolean) {
		const offset = open ? (i + 1) * 50 - 30 : 0;
		return `opacity: ${open ? 1 : 0}; pointer-events: ${open ? 'auto' : 'none'}; transform: translateY(-${offset}px) scale(${open ? 1 : 0.5}); transition: transform 0.35s cubic-bezier(0.4,0,0.2,1) ${i * 0.06}s, opacity 0.2s ${i * 0.06}s;`;
	}
</script>

{#if showLoading}
	<div
		class="loading-screen"
		class:fade-out={fadeOut}
		ontransitionend={() => {
			if (fadeOut) showLoading = false;
		}}
	>
		<div class="bouncing-dots">
			<div class="dot"></div>
			<div class="dot"></div>
			<div class="dot"></div>
		</div>
		<p class="fade-in pulse">Loading positions...</p>
	</div>
{/if}
{#if !loading && !showLoading}
	{#if error}
		<div class="error-screen">
			<div class="sad-icon" aria-hidden="true">
				<svg
					width="64"
					height="64"
					viewBox="0 0 64 64"
					fill="none"
					xmlns="http://www.w3.org/2000/svg"
					aria-hidden="true"
				>
					<circle cx="32" cy="32" r="28" stroke="#ffb3c6" stroke-width="6" fill="none" />
					<line
						x1="20"
						y1="20"
						x2="44"
						y2="44"
						stroke="#ffb3c6"
						stroke-width="6"
						stroke-linecap="round"
					/>
					<line
						x1="44"
						y1="20"
						x2="20"
						y2="44"
						stroke="#ffb3c6"
						stroke-width="6"
						stroke-linecap="round"
					/>
				</svg>
			</div>
			<h2>There was an error fetching the data</h2>
			<!-- <p class="error-message">{error}</p> -->
			<button class="btn btn-big btn-error" onclick={() => location.reload()}>Reload</button>
		</div>
	{:else if snapshots.length === 0}
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

		<!-- Floating Action Button -->
		<div
			class="fixed bottom-6 right-6 z-50 flex flex-col items-end md:left-6 md:right-auto md:items-start"
		>
			<div class="relative h-16 w-16">
				{#each fabActions as action, i (action.label)}
				<div class="fab-action-wrapper" style={getFabActionStyle(i, fabActions.length, fabOpen)}>
					<button
						class="fab-action btn btn-primary absolute bottom-0 right-0 flex min-w-max origin-bottom-right items-center gap-2 whitespace-nowrap rounded-full px-4 py-2 md:left-0 md:right-auto md:origin-bottom-left md:gap-3"
						aria-label={action.label}
						onclick={() => {
							fabOpen = false;
							action.onClick();
						}}
					>
						<i class="{action.icon} text-lg"></i>
						<span class="font-medium">{action.label}</span>
					</button>
				</div>
				{/each}
				<button
					class="fab-main relative flex h-16 w-16 items-center justify-center overflow-hidden rounded-full bg-gradient-to-br from-purple-600 to-blue-500 text-2xl text-white shadow-xl transition-transform duration-300 focus:outline-none"
					aria-label="Open actions"
					onclick={() => (fabOpen = !fabOpen)}
					style="z-index:20;"
				>
					<span
						class="absolute inset-0 flex items-center justify-center transition-all duration-200"
						style="opacity: {fabOpen ? 0 : 1}; transform: scale({fabOpen
							? 0.7
							: 1}); transition: opacity 0.18s, transform 0.18s;"
					>
						<i class="fa-solid fa-bars"></i>
					</span>
					<span
						class="absolute inset-0 flex items-center justify-center transition-all duration-200"
						style="opacity: {fabOpen ? 1 : 0}; transform: scale({fabOpen
							? 1
							: 0.7}); transition: opacity 0.18s, transform 0.18s;"
					>
						<i class="fa-solid fa-xmark"></i>
					</span>
				</button>
			</div>
		</div>
	{/if}
{/if}

<style>
	/* Error screen styles */
	.error-screen {
		position: fixed;
		top: 0;
		left: 0;
		width: 100vw;
		height: 100vh;
		z-index: 1000;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		background: rgba(24, 24, 32, 0.96);
		color: #ffb3c6;
		text-align: center;
		animation: fadeIn 0.8s;
	}

	.sad-icon {
		font-size: 4rem;
		margin-bottom: 18px;
		animation: shake 1.2s infinite alternate;
	}

	.error-screen h2 {
		margin: 0 0 1.5rem 0;
		font-size: 1.6rem;
		font-weight: 600;
	}

	@keyframes shake {
		0% {
			transform: translateX(0);
		}
		15% {
			transform: translateX(-2px) rotate(-2deg);
		}
		30% {
			transform: translateX(2px) rotate(2deg);
		}
		45% {
			transform: translateX(-1.5px) rotate(-1.5deg);
		}
		60% {
			transform: translateX(1px) rotate(1deg);
		}
		75% {
			transform: translateX(0);
		}
		100% {
			transform: translateX(0);
		}
	}

	/* Fade-out effect for loading screen */
	.loading-screen {
		position: fixed;
		top: 0;
		left: 0;
		width: 100vw;
		height: 100vh;
		z-index: 1000;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		background: rgba(24, 24, 32, 0.96);
		color: #f3f3f3;
		opacity: 1;
		transition: opacity 0.6s cubic-bezier(0.4, 0, 0.2, 1);
		pointer-events: all;
	}
	.loading-screen.fade-out {
		opacity: 0;
		pointer-events: none;
	}
	.bouncing-dots {
		display: flex;
		gap: 10px;
		margin-bottom: 18px;
	}
	.bouncing-dots .dot {
		width: 16px;
		height: 16px;
		background: linear-gradient(135deg, var(--color-primary) 60%, var(--color-secondary) 100%);
		border-radius: 50%;
		animation: bounce 1.2s infinite ease-in-out;
	}
	.bouncing-dots .dot:nth-child(2) {
		animation-delay: 0.2s;
	}
	.bouncing-dots .dot:nth-child(3) {
		animation-delay: 0.4s;
	}
	@keyframes bounce {
		0%,
		80%,
		100% {
			transform: translateY(0);
		}
		40% {
			transform: translateY(-24px);
		}
	}
	.fade-in {
		animation: fadeIn 1.2s ease-in;
	}
	@keyframes fadeIn {
		from {
			opacity: 0;
		}
		to {
			opacity: 1;
		}
	}
	.pulse {
		animation: pulse 1.8s infinite;
	}
	@keyframes pulse {
		0% {
			opacity: 1;
		}
		50% {
			opacity: 0.5;
		}
		100% {
			opacity: 1;
		}
	}

	.quote-groups {
		margin-bottom: 4.5rem;
	}

	/* Floating Action Button styles */
	.fab-action {
		min-width: 90px;
		justify-content: flex-start;
		cursor: pointer;
	}
	.fab-main {
		box-shadow: 0 4px 16px 0 rgba(80, 80, 120, 0.18);
		will-change: transform;
		cursor: pointer;
		background-image: linear-gradient(
			to right,
			var(--color-primary) 0%,
			color-mix(in srgb, var(--color-primary), var(--color-secondary) 50%) 51%,
			var(--color-primary) 100%
		);
		background-size: 200% auto;
		transition:
			background-position 0.5s cubic-bezier(0.4, 0, 0.2, 1),
			box-shadow 0.2s;
	}

	.fab-main:hover {
		background-position: right center;
	}
	.fab-main:active {
		scale: 0.95;
	}
</style>
