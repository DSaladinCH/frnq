<script lang="ts">
	import { tick } from 'svelte';
	import PageHead from '$lib/components/PageHead.svelte';
	import ForecastChart from '$lib/components/ForecastChart.svelte';
	import { type ForecastDayDto, type ForecastQuoteDto } from '$lib/services/forecastService';
	import { dataStore } from '$lib/stores/dataStore';
	import PageTitle from '$lib/components/PageTitle.svelte';

	// Reactive values that track the store
	let forecast = $derived(dataStore.forecast);
	let quotes = $derived(dataStore.quotes);

	// Extract quote IDs that are actually in the forecast
	let quotesInForecast = $derived.by(() => {
		if (forecast.length === 0) return new Set<number>();
		const quoteIds = new Set<number>();
		for (const day of forecast) {
			if (day.quotes) {
				for (const quote of day.quotes) {
					quoteIds.add(quote.quoteId);
				}
			}
		}
		return quoteIds;
	});

	// Create quote lookup map for O(1) access
	let quoteMap = $derived(new Map(quotes.map(q => [q.id, q])));

	// Group quotes by group, then by quoteId - only for quotes that are in the forecast
	let groupedQuotes = $derived.by(() => {
		const groupedByGroup: Record<string, { name: string; quoteIds: number[] }> = {};
		const ungrouped: number[] = [];

		for (const quote of quotes) {
			// Only include quotes that have forecast data
			if (!quotesInForecast.has(quote.id)) continue;

			const groupId = quote.group?.id;
			const groupName = quote.group?.name;

			if (groupId && groupName) {
				if (!groupedByGroup[groupId]) {
					groupedByGroup[groupId] = { name: groupName, quoteIds: [] };
				}
				groupedByGroup[groupId].quoteIds.push(quote.id);
			} else {
				ungrouped.push(quote.id);
			}
		}

		return { groupedByGroup, ungrouped };
	});

	// Helper to get display name for a quoteId
	function getQuoteDisplayName(quoteId: number) {
		const quote = quoteMap.get(quoteId);
		if (!quote) return 'Unknown Quote';
		return quote.customName ? quote.customName : quote.name;
	}

	// State for filtering
	let filterMode = $state<'full' | 'group' | 'quote'>('full');
	let filterGroupId = $state<string | null>(null);
	let filterQuoteId = $state<number | null>(null);

	// UI handlers
	function handleGroupView(groupId: string) {
		filterMode = 'group';
		filterGroupId = groupId;
		filterQuoteId = null;
		tick();
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

	// Filter forecast data based on selected quotes
	let filteredForecast = $derived.by(() => {
		if (forecast.length === 0) return [];

		let selectedQuoteIds: number[] = [];

		if (filterMode === 'full') {
			// Full view: show all quotes in the forecast
			selectedQuoteIds = Array.from(quotesInForecast);
		} else if (filterMode === 'group' && filterGroupId) {
			// Group view: show quotes in the selected group
			selectedQuoteIds = groupedQuotes.groupedByGroup[filterGroupId]?.quoteIds ?? [];
		} else if (filterMode === 'quote' && filterQuoteId != null) {
			// Quote view: show only the selected quote
			selectedQuoteIds = [filterQuoteId];
		}

		// Map forecast data to only include selected quotes
		return forecast.map(day => ({
			date: day.date,
			portfolio: day.portfolio,
			groups: day.groups,
			quotes: (day.quotes ?? []).filter(q => selectedQuoteIds.includes(q.quoteId))
		}));
	});

	// Calculate statistics based on filtered forecast
	let stats = $derived.by(() => {
		if (filteredForecast.length === 0) return null;

		// For statistics, use portfolio for full view, or compute group/quote specific stats
		let portfolioBands: number[] = [];

		if (filterMode === 'full') {
			// Full view: use portfolio band
			portfolioBands = filteredForecast.map((day) => day.portfolio.median);
		} else if (filterMode === 'group' && filterGroupId) {
			// Group view: find the group in the forecast and use its band
			portfolioBands = filteredForecast.map((day) => {
				const group = day.groups.find(g => g.groupId === parseInt(filterGroupId));
				return group?.band.median ?? 0;
			});
		} else if (filterMode === 'quote' && filterQuoteId != null) {
			// Quote view: use the quote's band
			portfolioBands = filteredForecast.map((day) => {
				const quote = day.quotes.find(q => q.quoteId === filterQuoteId);
				return quote?.band.median ?? 0;
			});
		}

		if (portfolioBands.length === 0) return null;

		const medians = portfolioBands;
		const lowers = filteredForecast.map((day) => {
			if (filterMode === 'full') return day.portfolio.lower;
			if (filterMode === 'group' && filterGroupId) {
				const group = day.groups.find(g => g.groupId === parseInt(filterGroupId));
				return group?.band.lower ?? 0;
			}
			if (filterMode === 'quote' && filterQuoteId != null) {
				const quote = day.quotes.find(q => q.quoteId === filterQuoteId);
				return quote?.band.lower ?? 0;
			}
			return 0;
		});
		const uppers = filteredForecast.map((day) => {
			if (filterMode === 'full') return day.portfolio.upper;
			if (filterMode === 'group' && filterGroupId) {
				const group = day.groups.find(g => g.groupId === parseInt(filterGroupId));
				return group?.band.upper ?? 0;
			}
			if (filterMode === 'quote' && filterQuoteId != null) {
				const quote = day.quotes.find(q => q.quoteId === filterQuoteId);
				return quote?.band.upper ?? 0;
			}
			return 0;
		});

		const latestMedian = medians[medians.length - 1];
		const firstMedian = medians[0];
		const change = latestMedian - firstMedian;
		const changePercent = (change / firstMedian) * 100;

		return {
			latestMedian,
			minLower: Math.min(...lowers),
			maxUpper: Math.max(...uppers),
			avgMedian: medians.reduce((a, b) => a + b, 0) / medians.length,
			change,
			changePercent
		};
	});

	// Get the latest quote values for cards
	function getLatestQuoteValue(quoteId: number) {
		const lastDay = forecast[forecast.length - 1];
		if (!lastDay || !lastDay.quotes) return { median: 0, lower: 0, upper: 0 };
		const quote = lastDay.quotes.find(q => q.quoteId === quoteId);
		return quote ? quote.band : { median: 0, lower: 0, upper: 0 };
	}

	// Get group values from the pre-computed response
	function getGroupLatestValues(groupId: string) {
		const lastDay = forecast[forecast.length - 1];
		if (!lastDay || !lastDay.groups) return { median: 0, lower: 0, upper: 0 };
		const group = lastDay.groups.find(g => g.groupId === parseInt(groupId));
		return group ? group.band : { median: 0, lower: 0, upper: 0 };
	}

	interface GroupCard {
		type: 'group';
		groupId: string;
		groupName: string;
		quoteIds: number[];
	}
	interface QuoteCard {
		type: 'quote';
		quoteId: number;
		groupId?: string;
	}
	interface BackCard {
		type: 'back';
	}
	type Card = GroupCard | QuoteCard | BackCard;

	// Get cards to render based on filter mode
	let cardsToRender = $derived.by((): Card[] => {
		const result: Card[] = [];

		// Add back card if not in full view
		if (filterMode !== 'full') {
			result.push({ type: 'back' });
		}

		if (filterMode === 'full') {
			// Add group cards
			for (const [groupId, { name: groupName, quoteIds }] of Object.entries(groupedQuotes.groupedByGroup)) {
				result.push({
					type: 'group',
					groupId,
					groupName,
					quoteIds
				});
			}

			// Add ungrouped quote cards
			for (const quoteId of groupedQuotes.ungrouped) {
				result.push({
					type: 'quote',
					quoteId
				});
			}
		} else if (filterMode === 'group' && filterGroupId) {
			const group = groupedQuotes.groupedByGroup[filterGroupId];
			if (!group) return result;
			for (const quoteId of group.quoteIds) {
				result.push({
					type: 'quote' as const,
					quoteId,
					groupId: filterGroupId
				});
			}
		}

		return result;
	});
</script>

<PageHead title="Forecast" />

<div class="xs:p-8 p-4">
	<div class="mb-6">
		<PageTitle title="Forecast" icon="fa-solid fa-compass" />
	</div>

	<div class="mb-6">
		<ForecastChart forecast={filteredForecast} />
	</div>

	{#if stats}
		<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4 mb-8">
			<div class="card">
				<div class="mb-1 text-sm font-medium color-muted">
					<i class="fa-solid fa-chart-line mr-2"></i>Latest Median
				</div>
				<div class="text-2xl font-bold">
					{stats.latestMedian.toLocaleString(undefined, {
						style: 'currency',
						currency: 'CHF',
						maximumFractionDigits: 0
					})}
				</div>
			</div>

			<div class="card">
				<div class="mb-1 text-sm font-medium color-muted">
					<i class="fa-solid fa-arrow-up mr-2"></i>Upside (Max)
				</div>
				<div class="text-2xl font-bold color-success">
					{stats.maxUpper.toLocaleString(undefined, {
						style: 'currency',
						currency: 'CHF',
						maximumFractionDigits: 0
					})}
				</div>
			</div>

			<div class="card">
				<div class="mb-1 text-sm font-medium color-muted">
					<i class="fa-solid fa-arrow-down mr-2"></i>Downside (Min)
				</div>
				<div class="text-2xl font-bold color-error">
					{stats.minLower.toLocaleString(undefined, {
						style: 'currency',
						currency: 'CHF',
						maximumFractionDigits: 0
					})}
				</div>
			</div>

			<div class="card">
				<div class="mb-1 text-sm font-medium color-muted">
					<i
						class={`fa-solid mr-2 ${stats.change >= 0 ? 'fa-arrow-trend-up color-success' : 'fa-arrow-trend-down color-error'}`}
					></i>Expected Change
				</div>
				<div class={`text-2xl font-bold ${stats.change >= 0 ? 'color-success' : 'color-error'}`}>
					{stats.change >= 0 ? '+' : ''}{stats.change.toLocaleString(undefined, {
						style: 'currency',
						currency: 'CHF',
						maximumFractionDigits: 0
					})}
					<span class="text-sm"
						>({stats.changePercent >= 0 ? '+' : ''}{stats.changePercent.toFixed(2)}%)</span
					>
				</div>
			</div>
		</div>
	{/if}

	<!-- Cards Section -->
	<div class="quote-groups mx-auto mb-3 mt-4 grid w-full grid-cols-[repeat(auto-fit,minmax(300px,450px))] justify-center gap-5 xl:w-4/5">
		{#each cardsToRender as card (card.type === 'back' ? 'back' : card.type === 'group' ? `group-${card.groupId}` : `quote-${card.quoteId}`)}
			{#if card.type === 'back'}
				<button
					onclick={filterMode === 'quote' && filterGroupId ? handleBackToGroupView : handleBackToFullView}
					class="card card-reactive cursor-pointer hover:opacity-80 transition-opacity text-left"
				>
					<div class="flex items-center justify-center h-full min-h-24">
						<div class="text-center">
							<i class="fa-solid fa-arrow-left text-xl mb-2 block"></i>
							<span class="font-medium">Back to {filterMode === 'group' ? 'full view' : filterMode === 'quote' && filterGroupId ? 'group' : 'full view'}</span>
						</div>
					</div>
				</button>
			{:else if card.type === 'group'}
				{@const values = getGroupLatestValues(card.groupId)}
				<button
					onclick={() => handleGroupView(card.groupId)}
					class="card card-reactive cursor-pointer hover:opacity-80 transition-opacity text-left"
				>
					<div class="flex items-start justify-between mb-4">
						<h3 class="text-base font-semibold">{card.groupName}</h3>
					</div>
					<div class="space-y-2">
						<div class="flex max-xs:flex-col justify-between text-sm">
							<span class="color-muted">Median:</span>
							<span class="font-medium">
								{values.median.toLocaleString(undefined, {
									style: 'currency',
									currency: 'CHF',
									maximumFractionDigits: 0
								})}
							</span>
						</div>
						<div class="flex max-xs:flex-col justify-between text-sm">
							<span class="color-muted">Range:</span>
							<span class="font-medium">
								{values.lower.toLocaleString(undefined, {
									style: 'currency',
									currency: 'CHF',
									maximumFractionDigits: 0
								})}
								-
								{values.upper.toLocaleString(undefined, {
									style: 'currency',
									currency: 'CHF',
									maximumFractionDigits: 0
								})}
							</span>
						</div>
					</div>
				</button>
			{:else}
				{@const values = getLatestQuoteValue(card.quoteId)}
				<button
					onclick={() => handleQuoteView(card.quoteId, card.groupId)}
					class="card card-reactive cursor-pointer hover:opacity-80 transition-opacity text-left"
				>
					<div class="flex items-start justify-between mb-4">
						<h3 class="text-base font-semibold">{getQuoteDisplayName(card.quoteId)}</h3>
					</div>
					<div class="space-y-2">
						<div class="flex justify-between text-sm">
							<span class="color-muted">Median:</span>
							<span class="font-medium">
								{values.median.toLocaleString(undefined, {
									style: 'currency',
									currency: 'CHF',
									maximumFractionDigits: 0
								})}
							</span>
						</div>
						<div class="flex justify-between text-sm">
							<span class="color-muted">Range:</span>
							<span class="font-medium">
								{values.lower.toLocaleString(undefined, {
									style: 'currency',
									currency: 'CHF',
									maximumFractionDigits: 0
								})}
								-
								{values.upper.toLocaleString(undefined, {
									style: 'currency',
									currency: 'CHF',
									maximumFractionDigits: 0
								})}
							</span>
						</div>
					</div>
				</button>
			{/if}
		{/each}
	</div>
</div>

<style>
	.quote-groups {
		margin-bottom: 4.5rem;
	}
</style>
