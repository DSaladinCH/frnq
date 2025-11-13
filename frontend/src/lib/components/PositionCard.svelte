<script lang="ts">
	interface Props {
		type: 'group' | 'quote';
		groupName?: string;
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
		isActiveQuote?: boolean;
		viewLabel?: string;
		profitClass?: string;
		minimal?: boolean; // new prop for minimal mode (e.g. back card)
	}

	let {
		type,
		groupName = '',
		summary = { invested: 0, currentValue: 0, totalValue: 0, realized: 0, totalProfit: 0 },
		title = '',
		onView = null,
		isActiveQuote = false,
		viewLabel = '',
		profitClass = '',
		minimal = false
	}: Props = $props();

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
	class="card card-reactive group-card {minimal ? 'minimal-card' : ''}"
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
	<div class="group-header">
		<span class="group-title">{@html title}</span>
		{#if onView && !minimal}
			<span class="icon-btn view-btn" title={viewLabel} aria-label={viewLabel} tabindex="-1">
				<i class={isActiveQuote ? 'fa-solid fa-xmark fa-lg' : 'fa-solid fa-chart-line fa-lg'}></i>
			</span>
		{/if}
	</div>
	{#if !minimal}
		<div class="group-summary">
			<div class="summary-row">
				<span class="label">Current Value:</span>
				<span class="value"
					>{summary.currentValue.toLocaleString(undefined, {
						style: 'currency',
						currency: 'CHF'
					})}</span
				>
			</div>
			<div class="summary-row">
				<span class="label">Invested:</span>
				<span class="value"
					>{summary.invested.toLocaleString(undefined, {
						style: 'currency',
						currency: 'CHF'
					})}</span
				>
			</div>
			<div class="divider"></div>
			<div class="profit-row">
				<div class="profit-main">
					<span class="profit-label">Position Gain:</span>
					<span class="profit {unrealizedGain >= 0 ? 'positive' : 'negative'}">
						{unrealizedGain >= 0 ? '+' : ''} {unrealizedGain.toLocaleString(undefined, {
							style: 'currency',
							currency: 'CHF'
						})}
					</span>
				</div>
				<span class="profit-percent {unrealizedGain >= 0 ? 'positive' : 'negative'}">
					({positionPerformancePct >= 0 ? '+' : ''} {positionPerformancePct.toLocaleString(undefined, {
						maximumFractionDigits: 2
					})}%)
				</span>
			</div>
			{#if summary.realized !== 0}
				<div class="realized-gains-note">
					<i class="fa-solid fa-circle-info note-icon"></i>
					<span class="note-text">
						+ {summary.realized.toLocaleString(undefined, {
							style: 'currency',
							currency: 'CHF'
						})} in realized gains
					</span>
				</div>
			{/if}
		</div>
	{/if}
</div>

<style>
	.minimal-card {
		/* Remove always-on hover style, just use default background */
		justify-content: center;
		align-items: center;
		min-height: 4.5rem;
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
		color: #b3b3b3;
		border-radius: 0.3rem;
		display: flex;
		align-items: center;
		height: 2.2rem;
		pointer-events: none;
	}
	.group-title {
		font-weight: bold;
		font-size: 1.2rem;
		color: #f3f3f3;
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}
	.group-summary {
		width: 100%;
		display: flex;
		flex-direction: column;
		gap: 0.4rem;
	}
	.summary-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
		font-size: 0.95rem;
	}
	.summary-row .label {
		color: #b3b3b3;
		font-weight: 500;
	}
	.summary-row .value {
		color: #f3f3f3;
		font-weight: 600;
	}
	.summary-row .value.positive {
		color: #2ecc40;
	}
	.divider {
		height: 1px;
		background: #444;
		margin: 0.3rem 0;
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
		flex-direction: column;
		gap: 0.2rem;
	}
	.profit-main {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 0.5rem;
	}
	.profit-label {
		color: #b3b3b3;
		font-size: 0.95rem;
		font-weight: 500;
	}
	.profit {
		font-size: 1.3rem;
		font-weight: 700;
	}
	.profit-positive {
		color: #2ecc40;
	}
	.profit-negative {
		color: #ff4d4f;
	}
	.profit-percent {
		font-size: 0.95rem;
		font-weight: 600;
		align-self: flex-end;
	}
	.profit-percent.profit-positive {
		color: #2ecc40;
	}
	.profit-percent.profit-negative {
		color: #ff4d4f;
	}
	.realized-gains-note {
		display: flex;
		align-items: center;
		gap: 0.4rem;
		font-size: 0.85rem;
		color: #2ecc40;
		margin-top: 0.3rem;
		padding: 0.3rem 0.5rem;
		background: rgba(46, 204, 64, 0.1);
		border-radius: 0.3rem;
		border-left: 2px solid #2ecc40;
	}
	.note-icon {
		font-size: 0.9rem;
		opacity: 0.8;
	}
	.note-text {
		font-weight: 500;
	}
	.minimal-card .group-header {
		margin-bottom: 0;
		justify-content: center;
	}
</style>
