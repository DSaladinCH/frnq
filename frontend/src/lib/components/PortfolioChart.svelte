<script lang="ts">
	import { onMount } from 'svelte';
	import Chart from 'chart.js/auto';
	import type { PositionSnapshot } from '../services/positionService';
	import DropDown from './DropDown.svelte';

	let { snapshots }: { snapshots: PositionSnapshot[] } = $props();
	let canvas: HTMLCanvasElement;
	let chart: Chart;
	let tooltipEl: HTMLDivElement;

	// Period type for strict typing
	const periodOptions = [
		{ value: '1w', label: '1 Week' },
		{ value: '1m', label: '1 Month' },
		{ value: '3m', label: '3 Month' },
		{ value: 'ytd', label: 'This Year' },
		{ value: 'all', label: 'All Time' }
	] as const;

	type Period = (typeof periodOptions)[number]['value'];
	const PERIOD_STORAGE_KEY = 'portfolioChart.selectedPeriod';
	let selectedPeriod: Period = $state('3m');

	// Chart option variables and handlers
	const chartOptionOptions = [
		{ value: 'both', label: 'Portfolio' },
		{ value: 'profitOnly', label: 'Profit Only' }
	] as const;

	type ChartOption = (typeof chartOptionOptions)[number]['value'];
	const CHART_OPTION_STORAGE_KEY = 'portfolioChart.chartOption';
	let chartOption = $state<ChartOption>('both');

	// Group snapshots by date and aggregate values
	function groupSnapshotsByDate(snapshots: PositionSnapshot[]) {
		const grouped: Record<string, PositionSnapshot[]> = {};
		snapshots.forEach((snap) => {
			const date = snap.date.slice(0, 10);
			if (!grouped[date]) grouped[date] = [];
			grouped[date].push(snap);
		});
		const result = Object.entries(grouped).map(([date, snaps]) => {
			// Aggregate: sum invested, unrealizedGain, realizedGain
			const invested = snaps.reduce((sum, s) => sum + (s.invested ?? 0), 0);
			const currentValue = snaps.reduce((sum, s) => sum + (s.currentValue ?? 0), 0);
			const realizedGain = snaps.reduce((sum, s) => sum + (s.realizedGain ?? 0), 0);
			// Total Value = invested + unrealized + realized
			const totalValue = currentValue + realizedGain;
			// Total Profit = unrealized + realized
			const totalProfit = totalValue - invested;

			return {
				date,
				invested,
				totalValue,
				totalProfit,
				realizedGain
			};
		});

		// Sort by date ascending
		result.sort((a, b) => a.date.localeCompare(b.date));
		return result;
	}

	function getOrCreateTooltip(chart: Chart) {
		let tooltip = document.getElementById('chartjs-custom-tooltip') as HTMLDivElement;
		if (!tooltip) {
			tooltip = document.createElement('div');
			tooltip.id = 'chartjs-custom-tooltip';
			tooltip.className = 'chartjs-tooltip';
			tooltip.style.opacity = '0';
			tooltip.style.pointerEvents = 'none';
			document.body.appendChild(tooltip);
		}
		return tooltip;
	}

	function externalTooltipHandler(context: any) {
		// Tooltip Element
		const { chart, tooltip } = context;
		const tooltipEl = getOrCreateTooltip(chart);

		// Hide if no tooltip
		if (tooltip.opacity === 0) {
			tooltipEl.style.opacity = '0';
			tooltipEl.style.left = '-9999px';
			tooltipEl.style.top = '-9999px';
			return;
		}

		// Set caret position
		tooltipEl.classList.remove('above', 'below', 'no-transform');
		if (tooltip.yAlign) {
			tooltipEl.classList.add(tooltip.yAlign);
		} else {
			tooltipEl.classList.add('no-transform');
		}

		// Set HTML content
		if (tooltip.body) {
			const titleLines = tooltip.title || [];
			const bodyLines = tooltip.body.map((b: any) => b.lines);
			// Use tooltip.dataPoints to get the dataset color directly
			const dataPoints = tooltip.dataPoints || [];
			let innerHtml = `<div class="tooltip-title">${titleLines.join('<br>')}</div>`;
			innerHtml += '<div class="tooltip-body">';
			for (let i = 0; i < bodyLines.length; i++) {
				let color = '#888';
				if (dataPoints[i]) {
					color =
						dataPoints[i].dataset.borderColor || dataPoints[i].dataset.backgroundColor || '#888';
					if (Array.isArray(color)) {
						color = `rgba(${color.join(',')})`;
					} else if (
						typeof color === 'object' &&
						color !== null &&
						'r' in color &&
						'g' in color &&
						'b' in color
					) {
						const { r, g, b, a } = color;
						color = typeof a !== 'undefined' ? `rgba(${r},${g},${b},${a})` : `rgb(${r},${g},${b})`;
					}
				}
				// Split label and value for alignment
				let label = dataPoints[i]?.dataset?.label || '';
				let value = dataPoints[i]?.raw;

				const num = parseFloat(value);
				if (!isNaN(num)) {
					value = num.toFixed(2);
				}

				innerHtml += `<div class="tooltip-item-flex"><span class="tooltip-color" style="background:${color}"></span><span class="tooltip-label">${label}: </span><span class="tooltip-value">${value}</span></div>`;
			}
			innerHtml += '</div>';
			tooltipEl.innerHTML = innerHtml;
		}

		// Use mouse position for fixed positioning, fallback to chart area if not available
		let left = 0;
		const margin = 40;
		const tooltipWidth = tooltipEl.offsetWidth || 200; // fallback width
		let pointX = 0;
		const mouseEvent = tooltip._event;
		const rect = chart.canvas.getBoundingClientRect();
		if (mouseEvent && mouseEvent.clientX) {
			pointX = mouseEvent.clientX;
		} else if (tooltip.caretX !== undefined) {
			pointX = rect.left + window.scrollX + tooltip.caretX;
		}
		// Default: right of point
		left = pointX + margin;
		// If would overflow right, show to the left
		if (left + tooltipWidth > window.innerWidth - margin) {
			left = pointX - tooltipWidth - margin;
		}
		// Fixed top position relative to canvas
		const fixedOffset = 40; // px below the top of the canvas
		const top = rect.top + window.scrollY + fixedOffset;
		tooltipEl.style.left = left + 'px';
		tooltipEl.style.top = top + 'px';
		tooltipEl.style.opacity = '1';
		tooltipEl.style.height = '115px'; // Fixed height

		if (chartOption === 'profitOnly') {
			tooltipEl.style.height = '85px';
		} else {
			tooltipEl.style.height = '115px';
		}

		tooltipEl.style.overflowY = 'auto';
	}

	onMount(() => {
		if (chart) chart.destroy();
		if (!snapshots.length) return;

		const grouped = groupSnapshotsByDate(snapshots);
		chart = new Chart(canvas, {
			type: 'line',
			data: {
				labels: [],
				datasets: [
					{
						label: 'Total Value',
						data: [],
						fill: true,
						tension: 0.3,
						pointRadius: 0,
						pointHoverRadius: 6,
						borderWidth: -1,
						order: 1,
						pointBorderColor: '#222',
						pointStyle: 'circle'
					},
					{
						label: 'Invested',
						data: [],
						fill: true,
						tension: 0.3,
						pointRadius: 0,
						pointHoverRadius: 6,
						borderWidth: -1,
						order: 2,
						pointBorderColor: '#222',
						pointStyle: 'circle'
					}
				]
			},
			options: {
				responsive: true,
				maintainAspectRatio: false,
				plugins: {
					legend: { display: false },
					title: { display: false },
					tooltip: {
						enabled: false,
						external: externalTooltipHandler
					}
				},
				hover: {
					mode: 'index',
					intersect: false,
					axis: 'x'
				},
				interaction: {
					mode: 'index',
					intersect: false,
					axis: 'x'
				},
				scales: {
					x: {
						display: false,
						grid: { display: false }
					},
					xAxis: { display: false },
					y: {
						display: false,
						grid: { display: false }
					},
					yAxis: { display: false }
				}
			}
		});
		return () => {
			chart?.destroy();
			// Remove tooltip element on destroy
			const el = document.getElementById('chartjs-custom-tooltip');
			if (el) el.remove();
		};
	});

	// Compute current portfolio info (latest date)
	let filteredSnapshots = $derived(
		filterSnapshotsWithActivePositionsInPeriod(snapshots, selectedPeriod)
	);
	let groupedSnapshots = $derived(groupSnapshotsByDate(filteredSnapshots));
	let latest = $derived(
		groupedSnapshots.length ? groupedSnapshots[groupedSnapshots.length - 1] : null
	);
	let first = $derived(groupedSnapshots.length ? groupedSnapshots[0] : null);

	// Total profit (realized + unrealized)
	let totalProfit = $derived(latest ? latest.totalProfit : 0);
	// Profit at start
	let startProfit = $derived(first ? first.totalProfit : 0);
	// Profit change in this period
	let profitChange = $derived(latest && first ? totalProfit - startProfit : 0);
	// Profit change %
	let profitChangePct = $derived(
		latest && first && Math.abs(startProfit) > 1e-6
			? (profitChange / Math.abs(startProfit)) * 100
			: latest && first && Math.abs(latest.invested) > 1e-6
				? (profitChange / Math.abs(latest.invested)) * 100
				: 0
	);

	// Display-friendly capped profitChangePct
	let profitChangePctDisplay = $derived(
		first && Math.abs(startProfit) < 1e-6 && latest && Math.abs(latest.invested) > 1e-6
			? profitChange > 0
				? '+∞'
				: profitChange < 0
					? '-∞'
					: '0.00'
			: profitChangePct > 9999
				? '+9999'
				: profitChangePct < -9999
					? '-9999'
					: (profitChangePct >= 0 ? '+' : '') + profitChangePct.toFixed(2)
	);

	// Color for profit change
	let profitColor = $derived(profitChange > 0 ? 'green' : profitChange < 0 ? 'red' : 'gray');

	// Dynamic background fade color based on chartOption
	let fadeColor = $derived(chartOption === 'profitOnly' ? 'rgb(60, 39, 82)' : 'rgb(42, 85, 108)');

	function updateChartData() {
		if (!chart || !groupedSnapshots) return;

		chart.data.labels = groupedSnapshots.map((s) => s.date);

		// Update first dataset based on chartOption
		const dataset0 = chart.data.datasets[0] as any;
		const dataset1 = chart.data.datasets[1] as any;

		if (chartOption === 'profitOnly') {
			dataset0.label = 'Profit';
			dataset0.data = groupedSnapshots.map((s) => roundValue(s.totalProfit));
			dataset0.borderColor = 'rgba(168,85,247,1)'; // Purple for profit
			dataset0.backgroundColor = 'rgba(168,85,247,0.25)';
			dataset0.pointBackgroundColor = 'rgba(168,85,247,1)';
		} else {
			dataset0.label = 'Total Value';
			dataset0.data = groupedSnapshots.map((s) => roundValue(s.totalValue));
			dataset0.borderColor = 'rgba(16,185,129,1)'; // Green for total value
			dataset0.backgroundColor = 'rgba(16,185,129,0.25)';
			dataset0.pointBackgroundColor = 'rgba(16,185,129,1)';
		}

		// Always update the invested dataset
		dataset1.data = groupedSnapshots.map((s) => roundValue(s.invested));
		dataset1.borderColor = 'rgba(99,102,241,1)';
		dataset1.backgroundColor = 'rgba(99,102,241,0.35)';
		dataset1.pointBackgroundColor = 'rgba(99,102,241,1)';

		// Show/hide the "Invested" line based on chartOption
		dataset1.hidden = chartOption === 'profitOnly';

		chart.update();
	}

	function filterSnapshotsByPeriod(
		snapshots: PositionSnapshot[],
		period: typeof selectedPeriod
	): PositionSnapshot[] {
		if (period === 'all') return snapshots;
		const now = new Date();
		let fromDate: Date;
		switch (period) {
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
			default:
				return snapshots;
		}
		// Remove time from fromDate for comparison
		fromDate = new Date(fromDate.getFullYear(), fromDate.getMonth(), fromDate.getDate());
		
		return snapshots.filter((s) => new Date(s.date) >= fromDate);
	}

	// Filter snapshots to only include quotes that had active positions (amount > 0) during the selected period
	function filterSnapshotsWithActivePositionsInPeriod(
		snapshots: PositionSnapshot[],
		period: typeof selectedPeriod
	): PositionSnapshot[] {
		// First filter by period
		const periodFiltered = filterSnapshotsByPeriod(snapshots, period);

		// Group by quoteId to check which quotes had active positions in this period
		const quoteGroups: Record<number, PositionSnapshot[]> = {};
		periodFiltered.forEach((snap) => {
			if (!quoteGroups[snap.quoteId]) quoteGroups[snap.quoteId] = [];
			quoteGroups[snap.quoteId].push(snap);
		});

		// Find quotes that had at least one snapshot with amount > 0 in this period
		const activeQuoteIds = Object.entries(quoteGroups)
			.filter(([quoteId, snaps]) => snaps.some((snap) => snap.amount > 0))
			.map(([quoteId, snaps]) => parseInt(quoteId));

		// Return all snapshots (in the period) for quotes that were active during the period
		return periodFiltered.filter((snap) => activeQuoteIds.includes(snap.quoteId));
	}
	$effect(() => {
		if (chart && groupedSnapshots) {
			updateChartData();
		}

		if (chart && chartOption) {
			updateChartData();
		}
	});

	function roundValue(val: number) {
		return Math.round(val * 100) / 100;
	}

	function selectPeriod(val: string) {
		selectedPeriod = val as Period;
		// Save to localStorage
		try {
			localStorage.setItem(PERIOD_STORAGE_KEY, selectedPeriod);
		} catch {}
	}

	function selectChartOption(val: string) {
		chartOption = val as ChartOption;
		// Save to localStorage
		try {
			localStorage.setItem(CHART_OPTION_STORAGE_KEY, chartOption);
		} catch {}
	}

	// On mount, restore from localStorage if available
	onMount(() => {
		// Restore chartOption
		try {
			const storedChartOption = localStorage.getItem(CHART_OPTION_STORAGE_KEY);
			if (storedChartOption && chartOptionOptions.some((opt) => opt.value === storedChartOption)) {
				chartOption = storedChartOption as ChartOption;
			}
		} catch {}
		// Restore selectedPeriod
		try {
			const storedPeriod = localStorage.getItem(PERIOD_STORAGE_KEY);
			if (storedPeriod && periodOptions.some((opt) => opt.value === storedPeriod)) {
				selectedPeriod = storedPeriod as Period;
			}
		} catch {}
	});
</script>

<div
	class="portfolio-info m-5 grid max-w-full grid-cols-[1fr] grid-rows-[auto_35px] justify-items-center md:max-w-md
			md:grid-cols-[auto_120px] md:grid-rows-[1fr] md:justify-items-start"
>
	{#if latest}
		<div class="portfolio-stats">
			<div class="profit-total w-full justify-center md:justify-start">
				<span class="profit-value color-default"
					>{totalProfit.toLocaleString(undefined, { style: 'currency', currency: 'CHF' })}</span
				>
			</div>

			<div class="profit-change-row w-full justify-center md:justify-start">
				<span class="profit-change-value" style="color: {profitColor}">
					{profitChange >= 0 ? '+' : ''}{profitChange.toLocaleString(undefined, {
						style: 'currency',
						currency: 'CHF'
					})}
				</span>

				<div class="profit-divider"></div>

				{#if profitChangePctDisplay === '+∞' || profitChangePctDisplay === '-∞'}
					<span
						class="profit-change-pct"
						style="color: {profitColor}"
						title={profitChangePct.toFixed(2) + '%'}>{profitChangePctDisplay}%</span
					>
				{:else if profitChangePctDisplay === '+9999' || profitChangePctDisplay === '-9999'}
					<span
						class="profit-change-pct"
						style="color: {profitColor}"
						title={profitChangePct.toFixed(2) + '%'}>{profitChangePctDisplay}%</span
					>
				{:else}
					<span class="profit-change-pct" style="color: {profitColor}"
						>{profitChangePctDisplay}%</span
					>
				{/if}
			</div>
		</div>

		<div
			class="chart-options hidden w-80 gap-2 md:grid md:w-full md:grid-cols-[1fr] md:grid-rows-[auto_auto]"
		>
			<button
				type="button"
				class="btn btn-small"
				class:btn-secondary={chartOption === 'both'}
				onclick={() => selectChartOption('both')}>Portfolio</button
			>
			<button
				type="button"
				class="btn btn-small"
				class:btn-secondary={chartOption === 'profitOnly'}
				onclick={() => selectChartOption('profitOnly')}>Profit Only</button
			>
		</div>
		<div class="block pt-2 md:hidden">
			<DropDown
				options={[...chartOptionOptions]}
				selected={chartOption}
				onSelect={selectChartOption}
			/>
		</div>
	{/if}
</div>

<div style="position: relative; width: 100%; height: 400px;">
	<canvas bind:this={canvas} style="height: 400px;"></canvas>
</div>

<div class="background-fade" style="--fade-color: {fadeColor}">
	<div class="period-selector">
		<div class="block sm:hidden">
			<DropDown options={[...periodOptions]} selected={selectedPeriod} onSelect={selectPeriod} />
		</div>
		<div class="hidden sm:flex">
			{#each periodOptions as opt}
				<button
					type="button"
					class="btn btn-small"
					class:btn-secondary={selectedPeriod === opt.value}
					onclick={() => selectPeriod(opt.value)}>{opt.label}</button
				>
			{/each}
		</div>
	</div>
</div>

<!-- The tooltip element is created dynamically and appended to body -->

<style>
	canvas {
		width: 100vw;
		max-width: 100%;
		display: block;
	}

	.portfolio-info {
		font-size: 1.1rem;
	}

	.portfolio-stats {
		display: flex;
		flex-wrap: wrap;
	}

	.profit-total {
		font-size: 2.1rem;
		font-weight: 700;
		display: flex;
	}

	.profit-value {
		font-size: 2.1rem;
		font-weight: 700;
		text-align: left;
	}

	.profit-change-row {
		display: flex;
		align-items: center;
		gap: 0.7rem;
	}

	.profit-change-value,
	.profit-change-pct {
		font-size: 1.2rem;
		font-weight: 600;
		padding: 0 0.25rem;
	}

	.profit-divider {
		border-left: 1px solid gray;
		height: 15px;
	}

	.background-fade {
		display: flex;
		height: 80px;
		margin-top: -10px;
		width: 100%;
		background: linear-gradient(
			to bottom,
			transparent 0px,
			var(--fade-color) 10px,
			var(--fade-color) 40%,
			transparent 100%
		);
		pointer-events: none;
		z-index: 1;
		position: relative;
	}

	.period-selector {
		display: flex;
		justify-content: center;
		align-items: center;
		margin-top: 20px;
		position: absolute;
		width: 100%;
		left: 0;
		top: 0;
		z-index: 1;
		pointer-events: auto;
	}

	.period-selector button {
		margin: 0 0.15rem;
	}
</style>
