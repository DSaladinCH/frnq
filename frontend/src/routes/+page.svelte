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
	import { derived, writable, get } from 'svelte/store';

	const snapshots = writable<PositionSnapshot[]>([]);
	const quotes = writable<QuoteModel[]>([]);
	const loading = writable(true);
	const error = writable<string | null>(null);

	// Group by quote group (from quote), then by quoteId
	const groupedSnapshots = derived([snapshots, quotes], ([$snapshots, $quotes]) => {
		const groups: Record<string, { name: string; quotes: Record<number, PositionSnapshot[]> }> = {};
		const ungrouped: Record<number, PositionSnapshot[]> = {};

		for (const snap of $snapshots) {
			const quote = $quotes.find((q) => q.id === snap.quoteId);
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
		const $quotesArr = get(quotes);
		return $quotesArr.find((q) => q.id === quoteId);
	}

	// Helper to get display name for a quoteId
	function getQuoteDisplayName(quoteId: number) {
		const quote = getQuoteById(quoteId);
		return quote ? quote.name : `Quote #${quoteId}`;
	}

	let showLoading = true;
	let fadeOut = false;

	$: if (!$loading && showLoading && !fadeOut) {
		// Start fade-out
		fadeOut = true;
	}

	onMount(async () => {
		loading.set(true);
		try {
			const data: PositionsResponse = await getPositionSnapshots(null, null);
			snapshots.set(data.snapshots);
			quotes.set(data.quotes);
			error.set(null);
		} catch (e) {
			error.set((e as Error).message);
		} finally {
			loading.set(false);
		}
	});

	// State for filtering (use Svelte stores for reactivity)
	const filterMode = writable<'full' | 'group' | 'quote'>('full');
	const filterGroupId = writable<string | null>(null);
	const filterQuoteId = writable<number | null>(null);

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

	const filteredSnapshots = derived(
		[snapshots, quotes, groupedSnapshots, filterMode, filterGroupId, filterQuoteId],
		([$snapshots, $quotes, $grouped, $filterMode, $filterGroupId, $filterQuoteId]) => {
			let snaps: PositionSnapshot[];
			if ($filterMode === 'full') snaps = $snapshots;
			else if ($filterMode === 'group' && $filterGroupId) {
				const group = $grouped.groups[$filterGroupId];
				snaps = group ? Object.values(group.quotes).flat() : [];
			} else if ($filterMode === 'quote' && $filterQuoteId != null) {
				snaps = $snapshots.filter((s) => s.quoteId === $filterQuoteId);
			} else snaps = $snapshots;
			return filterLeadingZeroSnapshots(snaps);
		}
	);

	// UI handlers (update stores)
	function handleGroupView(groupId: string) {
		filterMode.set('group');
		filterGroupId.set(groupId);
		filterQuoteId.set(null);
		tick(); // ensure reactivity
	}

	function handleQuoteView(quoteId: number, groupId?: string) {
		filterMode.set('quote');
		filterQuoteId.set(quoteId);
		filterGroupId.set(groupId ?? null);
		tick();
	}

	function handleBackToFullView() {
		filterMode.set('full');
		filterGroupId.set(null);
		filterQuoteId.set(null);
		tick();
	}

	function handleBackToGroupView() {
		if ($filterGroupId) {
			filterMode.set('group');
			filterQuoteId.set(null);
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
		$groupedSnapshots: {
			groups: Record<string, { name: string; quotes: Record<number, PositionSnapshot[]> }>;
			ungrouped: Record<number, PositionSnapshot[]>;
		},
		$filterMode: 'full' | 'group' | 'quote',
		$filterGroupId: string | null,
		$filterQuoteId: number | null
	): Card[] {
		if ($filterMode === 'full') {
			return [
				...Object.entries($groupedSnapshots.groups).map(
					([groupId, { name: groupName, quotes }]) => ({
						type: 'group' as const,
						groupId,
						groupName,
						quotes
					})
				),
				...Object.entries($groupedSnapshots.ungrouped).map(([quoteKey, snaps]) => ({
					type: 'quote' as const,
					quoteKey: +quoteKey,
					snaps
				}))
			];
		} else if ($filterMode === 'group' && $filterGroupId) {
			// Only show quotes of the selected group
			const group = $groupedSnapshots.groups[$filterGroupId];
			if (!group) return [];
			return Object.entries(group.quotes).map(([quoteKey, snaps]) => ({
				type: 'quote' as const,
				quoteKey: +quoteKey,
				snaps,
				groupId: $filterGroupId
			}));
		} else if ($filterMode === 'quote' && $filterQuoteId != null) {
			let snaps = null;
			let groupId: string | undefined = $filterGroupId ?? undefined;
			if (groupId && $groupedSnapshots.groups[groupId]?.quotes[$filterQuoteId]) {
				snaps = $groupedSnapshots.groups[groupId].quotes[$filterQuoteId];
			} else if ($groupedSnapshots.ungrouped[$filterQuoteId]) {
				snaps = $groupedSnapshots.ungrouped[$filterQuoteId];
				groupId = undefined; // assign undefined for type compatibility
			}
			if (snaps) {
				return [
					{ type: 'quote' as const, quoteKey: $filterQuoteId, snaps, groupId, isActiveQuote: true }
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
	$: cards = getQuoteCards($groupedSnapshots, $filterMode, $filterGroupId, $filterQuoteId);

	// Floating Action Button state
	let fabOpen = false;
	const fabActions = [
		{ icon: 'fa-solid fa-plus', label: 'Add Investment', onClick: () => alert('Add action') },
		{ icon: 'fa-solid fa-list-ul', label: 'Show Investments', onClick: () => alert('Edit action') }
	];

	// Helper to get action position: pop up from left bottom on desktop, right bottom on mobile
	function getFabActionStyle(i: number, total: number, open: boolean) {
		const offset = open ? (i + 1) * 55 + 25 : 0;
		return `opacity: ${open ? 1 : 0}; pointer-events: ${open ? 'auto' : 'none'}; transform: translateY(-${offset}px) scale(${open ? 1 : 0.5}); transition: transform 0.35s cubic-bezier(0.4,0,0.2,1) ${i * 0.06}s, opacity 0.2s ${i * 0.06}s;`;
	}
</script>

<svelte:head>
	<link
		rel="stylesheet"
		href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css"
		crossorigin="anonymous"
		referrerpolicy="no-referrer"
	/>
</svelte:head>

{#if showLoading}
	<div
		class="loading-screen"
		class:fade-out={fadeOut}
		on:transitionend={() => {
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
{#if !$loading && !showLoading}
	{#if $error}
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
			<!-- <p class="error-message">{$error}</p> -->
			<button class="btn btn-error" on:click={() => location.reload()}>Reload</button>
		</div>
	{:else if $snapshots.length === 0}
		<p>No data available.</p>
	{:else if $snapshots.length}
		<PortfolioChart snapshots={$filteredSnapshots} />

		<div
			class="quote-groups mx-auto mb-3 mt-4 grid w-full grid-cols-[repeat(auto-fit,_minmax(300px,_450px))] justify-center px-3 xl:w-4/5 gap-5"
		>
			{#if $filterMode !== 'full'}
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
					<button
						class="fab-action absolute bottom-0 right-0 flex min-w-max origin-bottom-right items-center gap-2 whitespace-nowrap rounded-full bg-white px-4 py-2 text-gray-800 shadow-lg md:left-0 md:right-auto md:origin-bottom-left md:gap-3"
						style={getFabActionStyle(i, fabActions.length, fabOpen)}
						aria-label={action.label}
						on:click={() => {
							fabOpen = false;
							action.onClick();
						}}
					>
						<i class="{action.icon} text-lg"></i>
						<span class="font-medium">{action.label}</span>
					</button>
				{/each}
				<button
					class="fab-main relative flex h-16 w-16 items-center justify-center overflow-hidden rounded-full bg-gradient-to-br from-purple-600 to-blue-500 text-2xl text-white shadow-xl transition-transform duration-300 focus:outline-none"
					aria-label="Open actions"
					on:click={() => (fabOpen = !fabOpen)}
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
		background: linear-gradient(135deg, #8e44ad 60%, #3498db 100%);
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
	.fab-action-enter {
		opacity: 0;
		transform: scale(0.7) translateY(20px);
	}
	.fab-action-enter-active {
		opacity: 1;
		transform: scale(1) translateY(0);
		transition:
			opacity 0.25s,
			transform 0.25s;
	}
	.fab-action-leave {
		opacity: 1;
		transform: scale(1) translateY(0);
	}
	.fab-action-leave-active {
		opacity: 0;
		transform: scale(0.7) translateY(20px);
		transition:
			opacity 0.2s,
			transform 0.2s;
	}

	.fab-action {
		min-width: 90px;
		justify-content: flex-start;
		cursor: pointer;
	}
	.fab-main {
		box-shadow: 0 4px 16px 0 rgba(80, 80, 120, 0.18);
		will-change: transform;
		cursor: pointer;
	}
	.fab-main:active {
		scale: 0.95;
	}
	@keyframes fab-pop {
		0% {
			opacity: 0;
			transform: scale(0.7) translate(-50%, -50%);
		}
		100% {
			opacity: 1;
			transform: scale(1) translate(-50%, -50%);
		}
	}
	.animate-fab-pop {
		animation: fab-pop 0.25s cubic-bezier(0.4, 0, 0.2, 1) both;
	}
</style>
