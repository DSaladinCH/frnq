<script lang="ts">
	import { onMount, tick } from 'svelte';
	import PortfolioChart from '$lib/components/PortfolioChart.svelte';
	import PositionCard from '$lib/components/PositionCard.svelte';
	import { getPositionSnapshots, type PositionSnapshot, type QuoteModel, type PositionsResponse } from '$lib/services/positionService';
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
		const quote = $quotes.find(q => q.id === snap.quoteId);
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
		unrealized: last?.unrealizedGain ?? 0,
	};
}

// Helper to sum all values in a group (sum of last snapshot of each quote)
function getGroupSummaryLast(quotes: Record<number, PositionSnapshot[]>) {
	const lastSnaps = Object.values(quotes)
		.map(snaps => snaps[snaps.length - 1])
		.filter(Boolean);
	return {
		invested: lastSnaps.reduce((sum, s) => sum + (s.invested ?? 0), 0),
		totalValue: lastSnaps.reduce((sum, s) => sum + (s.totalValue ?? 0), 0),
		realized: lastSnaps.reduce((sum, s) => sum + (s.realizedGain ?? 0), 0),
		unrealized: lastSnaps.reduce((sum, s) => sum + (s.unrealizedGain ?? 0), 0),
	};
}

// Helper to get quote by quoteId
function getQuoteById(quoteId: number) {
	const $quotesArr = get(quotes);
	return $quotesArr.find(q => q.id === quoteId);
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
const filteredSnapshots = derived([
  snapshots,
  quotes,
  groupedSnapshots,
  filterMode,
  filterGroupId,
  filterQuoteId
], ([$snapshots, $quotes, $grouped, $filterMode, $filterGroupId, $filterQuoteId]) => {
  if ($filterMode === 'full') return $snapshots;
  if ($filterMode === 'group' && $filterGroupId) {
    const group = $grouped.groups[$filterGroupId];
    if (!group) return [];
    return Object.values(group.quotes).flat();
  }
  if ($filterMode === 'quote' && $filterQuoteId != null) {
    return $snapshots.filter(s => s.quoteId === $filterQuoteId);
  }
  return $snapshots;
});

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
      ...Object.entries($groupedSnapshots.groups).map(([groupId, { name: groupName, quotes }]) => ({
        type: 'group' as const,
        groupId, groupName, quotes
      })),
      ...Object.entries($groupedSnapshots.ungrouped).map(([quoteKey, snaps]) => ({
        type: 'quote' as const, quoteKey: +quoteKey, snaps
      }))
    ];
  } else if ($filterMode === 'group' && $filterGroupId) {
    // Only show quotes of the selected group
    const group = $groupedSnapshots.groups[$filterGroupId];
    if (!group) return [];
    return Object.entries(group.quotes).map(([quoteKey, snaps]) => ({
      type: 'quote' as const, quoteKey: +quoteKey, snaps, groupId: $filterGroupId
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
      return [{ type: 'quote' as const, quoteKey: $filterQuoteId, snaps, groupId, isActiveQuote: true }];
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
        ? (card.groupId ? handleBackToGroupView : handleBackToFullView)
        : () => handleQuoteView(card.quoteKey, card.groupId),
      isActiveQuote: !!card.isActiveQuote,
      viewLabel: card.isActiveQuote
        ? (card.groupId ? 'Cancel quote filter' : 'Back to full view')
        : 'Show only this quote',
      profitClass: profit > 0 ? 'profit-positive' : profit < 0 ? 'profit-negative' : ''
    };
  }
}

// Reactive cards array for the template
$: cards = getQuoteCards($groupedSnapshots, $filterMode, $filterGroupId, $filterQuoteId);
</script>

<svelte:head>
	<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css" crossorigin="anonymous" referrerpolicy="no-referrer" />
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

    <div class="quote-groups grid grid-cols-[repeat(auto-fit,_minmax(300px,_450px))] justify-center mt-4">
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
      {#each cards as card (
        card.type === 'group'
          ? `group-${card.groupId}`
          : card.groupId
            ? `quote-${card.groupId}-${card.quoteKey}`
            : `quote-${card.quoteKey}`
      )}
        <PositionCard {...getCardProps(card)} />
      {/each}
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
	   gap: 1.5rem;
   }

   .group-card {
	   background: #232336;
	   border-radius: 1.1rem;
	   box-shadow: 0 2px 12px 0 rgba(0,0,0,0.10);
	   padding: 1.5rem 1.2rem 1.2rem 1.2rem;
	   width: 100%;
	   display: flex;
	   flex-direction: column;
	   align-items: flex-start;
   }

   .group-header {
	   margin-bottom: 0.5rem;
	   width: 100%;
	   display: flex;
	   align-items: center;
	   justify-content: space-between;
	   min-height: 2.2rem;
   }

   .icon-btn {
	   background: none;
	   border: none;
	   padding: 0.2rem;
	   margin-left: 0.5rem;
	   cursor: pointer;
	   color: #b3b3b3;
	   border-radius: 0.3rem;
	   transition: background 0.15s;
	   display: flex;
	   align-items: center;
	   height: 2.2rem;
   }

   .icon-btn:hover, .icon-btn:focus {
	   background: #35354a;
	   color: #f3f3f3;
   }
   .group-title {
	   font-weight: bold;
	   font-size: 1.2rem;
	   color: #f3f3f3;
   }
   .group-summary {
	   width: 100%;
	   display: flex;
	   flex-direction: column;
	   gap: 0.5rem;
   }
   .invested {
	   color: #b3b3b3;
	   font-size: 1rem;
	   font-weight: 500;
   }
   .amount {
	   font-weight: 600;
	   color: #f3f3f3;
   }
   .profit-row {
	   display: flex;
	   align-items: center;
	   gap: 0.5rem;
	   font-size: 1.1rem;
	   font-weight: 600;
   }
   .profit {
	   font-size: 1.2rem;
	   font-weight: 700;
	   margin-right: 0.2rem;
   }
   .profit-positive {
	   color: #2ecc40;
   }
   .profit-negative {
	   color: #ff4d4f;
   }
   .profit-percent {
	   font-size: 1rem;
	   color: #b3b3b3;
   }
   .btn.btn-back {
  background: #35354a;
  color: #f3f3f3;
  border: none;
  border-radius: 0.5rem;
  padding: 0.5rem 1.2rem;
  font-size: 1rem;
  font-weight: 500;
  margin-bottom: 1rem;
  cursor: pointer;
  transition: background 0.15s;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
.btn.btn-back:hover, .btn.btn-back:focus {
  background: #232336;
}
   .back-card.a11y-card-btn {
     cursor: pointer;
   }
   .back-card.a11y-card-btn:hover, .back-card.a11y-card-btn:focus {
     background: #35354a;
   }
</style>
