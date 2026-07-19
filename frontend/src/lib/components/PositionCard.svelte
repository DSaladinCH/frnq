<script lang="ts">
	import { formatCurrency, formatNumber } from '$lib/utils/numberFormat';
	import { userPreferences } from '$lib/stores/userPreferences';

	interface Props {
		summary?: {
			invested: number;
			currentValue: number;
			totalValue: number;
			realized: number;
			totalProfit: number;
			unrealizedGain?: number;
			totalInvestedCash?: number;
		};
		title?: string;
		onView?: (() => void) | null;
		viewLabel?: string;
		minimal?: boolean; // new prop for minimal mode (e.g. back card)
		groupFees?: number; // optional fee amount for the group or portfolio
	}

	let {
		summary = { invested: 0, currentValue: 0, totalValue: 0, realized: 0, totalProfit: 0 },
		title = '',
		onView = null,
		viewLabel = '',
		minimal = false,
		groupFees = 0
	}: Props = $props();

	let preferences = $state($userPreferences);

	// Subscribe to user preferences changes
	$effect(() => {
		const unsubscribe = userPreferences.subscribe((prefs) => {
			preferences = prefs;
		});
		return unsubscribe;
	});

	// Calculate metrics for display
	let unrealizedGain = $derived(summary.unrealizedGain ?? (summary.currentValue - summary.invested));
	
	// Position performance % (excludes realized gains - shows how current holdings are performing)
	let positionPerformancePct = $derived(
		summary.invested > 0
			? (unrealizedGain / summary.invested) * 100
			: 0
	);

	function handleCardClick(e: MouseEvent) {
		if (onView) {
			e.stopPropagation();
			onView();
		}
	}
</script>

<div
	class="card card-reactive group-card {minimal ? 'justify-center items-center min-h-18' : ''}"
	role={onView ? 'button' : undefined}
	{...onView ? { tabindex: 0 } : {}}
	aria-label={viewLabel || title}
	onclick={handleCardClick}
	onkeydown={(e) => {
		if (onView && (e.key === 'Enter' || e.key === ' ')) {
			e.preventDefault();
			onView();
		}
	}}
>
	<div class="w-full flex items-center justify-between min-h-8.5 {minimal ? 'h-full mb-0 justify-center' : 'mb-2'}">
		<span class="font-bold text-xl flex items-center gap-2">{@html title}</span>
	</div>
	{#if !minimal}
		<div class="w-full flex flex-col gap-1.5">
			<div class="flex justify-between items-center text-base">
				<span class="font-medium color-muted">Current Value:</span>
				<span class="font-semibold"
					>{formatCurrency(summary.currentValue, 'CHF', preferences.numberFormat)}</span
				>
			</div>
			<div class="flex justify-between items-center text-base">
				<span class="font-medium color-muted">Invested:</span>
				<span class="font-semibold"
					>{formatCurrency(summary.invested, 'CHF', preferences.numberFormat)}</span
				>
			</div>
			<div class="h-px bg-muted my-1.25 opacity-30"></div>
			<div class="flex flex-col gap-1">
				<div class="flex items-center justify-between gap-2">
					<span class="font-medium color-muted">Position Gain:</span>
					<span class="text-xl font-bold {unrealizedGain >= 0 ? 'color-success' : 'color-error'}">
						{unrealizedGain >= 0 ? '+' : ''} {formatCurrency(unrealizedGain, 'CHF', preferences.numberFormat)}
					</span>
				</div>
				<span class="text-base font-semibold self-end {unrealizedGain >= 0 ? 'color-success' : 'color-error'}">
					({positionPerformancePct >= 0 ? '+' : ''} {formatNumber(positionPerformancePct, preferences.numberFormat, {
						maximumFractionDigits: 2
					})}%)
				</span>
			</div>
			{#if summary.realized !== 0}
				<div class="realized-gains-note flex items-center gap-1.5 text-sm color-success mt-1.25 px-2 py-1.25 rounded-sm border-l-2 border-success">
					<i class="fa-solid fa-circle-info text-sm opacity-80"></i>
					<span class="font-medium">
						+ {formatCurrency(summary.realized, 'CHF', preferences.numberFormat)} in realized gains
					</span>
				</div>
			{/if}
			{#if groupFees > 0}
				<div class="h-px bg-muted my-1.25 opacity-30"></div>
				<div class="flex justify-between items-center text-base">
					<span class="font-medium color-muted">Fees:</span>
					<span class="font-semibold">
						- {formatCurrency(groupFees, 'CHF', preferences.numberFormat)}
					</span>
				</div>
			{/if}
		</div>
	{/if}
</div>

<style>
	.realized-gains-note {
		background-color: rgba(from var(--color-success) r g b / 0.1);
	}
</style>
