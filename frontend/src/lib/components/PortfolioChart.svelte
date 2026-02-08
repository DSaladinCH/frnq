<script lang="ts">
	import { onMount } from 'svelte';
	import Chart from 'chart.js/auto';
	import type { PositionSnapshot } from '../services/positionService';
	import PillToggle from './PillToggle.svelte';
	import DropDown from './DropDown.svelte';

	let { snapshots, onPeriodChange }: { snapshots: PositionSnapshot[], onPeriodChange?: (period: string) => void } = $props();
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
		{ value: 'totalValue', label: 'Total Value' },
		{ value: 'profitOnly', label: 'Total Profit' }
	] as const;

	type ChartOption = (typeof chartOptionOptions)[number]['value'];
	const CHART_OPTION_STORAGE_KEY = 'portfolioChart.chartOption';
	let chartOption = $state<ChartOption>('totalValue');

	// Group snapshots by date and aggregate values
	function groupSnapshotsByDate(snapshots: PositionSnapshot[]) {
		const grouped: Record<string, PositionSnapshot[]> = {};
		snapshots.forEach((snap) => {
			const date = snap.date.slice(0, 10);
			if (!grouped[date]) grouped[date] = [];
			grouped[date].push(snap);
		});
		const result = Object.entries(grouped).map(([date, snaps]) => {
			// Aggregate across all positions for this date
			const invested = snaps.reduce((sum, s) => sum + (s.invested ?? 0), 0);
			const currentValue = snaps.reduce((sum, s) => sum + (s.currentValue ?? 0), 0);
			const realizedGain = snaps.reduce((sum, s) => sum + (s.realizedGain ?? 0), 0);
			const totalInvestedCash = snaps.reduce((sum, s) => sum + (s.totalInvestedCash ?? 0), 0);
			
			// Unrealized gain = current value - cost basis of current holdings
			const unrealizedGain = currentValue - invested;
			
			// Total profit = unrealized + realized
			const totalProfit = unrealizedGain + realizedGain;

			return {
				date,
				invested,              // cost basis of current holdings
				currentValue,          // market value of current holdings
				unrealizedGain,        // gain/loss on current holdings
				realizedGain,          // profit from sells + dividends
				totalProfit,           // total gain (unrealized + realized)
				totalInvestedCash      // all cash ever invested
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

		// Adjust tooltip height based on number of datasets shown
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
	// Optimize by caching filtered and grouped data
	let filteredSnapshots = $derived.by(() => 
		filterSnapshotsWithActivePositionsInPeriod(snapshots, selectedPeriod)
	);
	
	let groupedSnapshots = $derived.by(() => groupSnapshotsByDate(filteredSnapshots));
	
	let latest = $derived(
		groupedSnapshots.length ? groupedSnapshots[groupedSnapshots.length - 1] : null
	);
	let first = $derived(groupedSnapshots.length ? groupedSnapshots[0] : null);

	// Total profit (realized + unrealized)
	let totalProfit = $derived(latest ? latest.totalProfit : 0);
	
	// Profit at start of period
	let startProfit = $derived(first ? first.totalProfit : 0);
	
	// Profit change in this period
	let profitChange = $derived(latest && first ? totalProfit - startProfit : 0);
	
	// Total Return % = (Total Profit / Total Cash Invested) × 100
	// This shows overall performance including realized gains
	let totalReturnPct = $derived(
		latest && latest.totalInvestedCash > 1e-6
			? (latest.totalProfit / latest.totalInvestedCash) * 100
			: 0
	);
	
	// Position Performance % = (Unrealized Gain / Cost Basis of Current Holdings) × 100
	// This shows how current holdings are performing
	let positionPerformancePct = $derived(
		latest && latest.invested > 1e-6
			? (latest.unrealizedGain / latest.invested) * 100
			: 0
	);

	// Period change % - change in total profit during the selected period
	let profitChangePct = $derived(
		latest && first && Math.abs(startProfit) > 1e-6
			? (profitChange / Math.abs(startProfit)) * 100
			: latest && first && Math.abs(first.totalInvestedCash) > 1e-6
				? (profitChange / Math.abs(first.totalInvestedCash)) * 100
				: 0
	);

	// Display-friendly capped profitChangePct
	let profitChangePctDisplay = $derived(
		first && Math.abs(startProfit) < 1e-6 && latest && Math.abs(first.totalInvestedCash) > 1e-6
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
	let fadeColor = $derived(
		chartOption === 'profitOnly' 
			? '#44223f' 
			: '#6b2b67'
	);

	function updateChartData() {
		if (!chart || !groupedSnapshots) return;

		chart.data.labels = groupedSnapshots.map((s) => s.date);

		// Update first dataset based on chartOption
		const dataset0 = chart.data.datasets[0] as any;
		const dataset1 = chart.data.datasets[1] as any;

		if (chartOption === 'profitOnly') {
			// Show total profit (unrealized + realized)
			dataset0.label = 'Total Profit';
			dataset0.data = groupedSnapshots.map((s) => roundValue(s.totalProfit));
			dataset0.borderColor = '#c14bac'; // Purple
			dataset0.backgroundColor = '#c14bac49';
			dataset0.pointBackgroundColor = '#c14bac';
			dataset1.hidden = true; // Hide invested line
		} else {
			// totalValue: Show total value (position + realized gains) vs total invested
			dataset0.label = 'Total Value';
			dataset0.data = groupedSnapshots.map((s) => roundValue(s.currentValue + s.realizedGain));
			dataset0.borderColor = '#c14bac'; // Green
			dataset0.backgroundColor = '#c14bac49';
			dataset0.pointBackgroundColor = '#c14bac';
			
			dataset1.label = 'Total Invested';
			dataset1.data = groupedSnapshots.map((s) => roundValue(s.totalInvestedCash));
			dataset1.borderColor = '#7a2b7d';
			dataset1.backgroundColor = '#7a2b7d88';
			dataset1.pointBackgroundColor = '#7a2b7d';
			dataset1.hidden = false;
		}

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
		const activeQuoteIds = new Set<number>();
		const quoteGroups = new Map<number, PositionSnapshot[]>();
		
		for (const snap of periodFiltered) {
			let group = quoteGroups.get(snap.quoteId);
			if (!group) {
				group = [];
				quoteGroups.set(snap.quoteId, group);
			}
			group.push(snap);
			
			// Check if this snapshot has an active position
			if (snap.amount > 0) {
				activeQuoteIds.add(snap.quoteId);
			}
		}

		// Return all snapshots (in the period) for quotes that were active during the period
		return periodFiltered.filter((snap) => activeQuoteIds.has(snap.quoteId));
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
		// Notify parent component
		if (onPeriodChange) {
			onPeriodChange(selectedPeriod);
		}
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
		// Notify parent component of initial period
		if (onPeriodChange) {
			onPeriodChange(selectedPeriod);
		}
	});
</script>

<div
	class="text-lg m-5 grid max-w-full grid-cols-[1fr] grid-rows-[auto_35px] justify-items-center md:max-w-md
			md:grid-cols-[auto_120px] md:grid-rows-[1fr] md:justify-items-start"
>
	{#if latest}
	
		<div class="flex flex-wrap">
			<div class="text-xs color-muted mb-1 w-full text-center md:text-left">
				Total Profit (Unrealized + Realized)
			</div>
			<div class="text-4xl font-bold flex w-full justify-center md:justify-start">
				<span class="text-left" style="color: {profitColor}"
					>{totalProfit >= 0 ? '+' : ''} {totalProfit.toLocaleString(undefined, { style: 'currency', currency: 'CHF' })}</span
				>
			</div>

			<div class="flex items-center gap-3 w-full justify-center md:justify-start mt-1">
				<span class="text-base font-semibold" style="color: {profitColor}">
					{profitChange >= 0 ? '+' : ''} {profitChange.toLocaleString(undefined, {
						style: 'currency',
						currency: 'CHF'
					})}
				</span>
				
				<div class="border-l border-button h-3.5"></div>

				{#if profitChangePctDisplay === '+∞' || profitChangePctDisplay === '-∞'}
					<span
						class="text-base font-semibold"
						style="color: {profitColor}"
						title={profitChangePct.toFixed(2) + '%'}>{profitChangePctDisplay}%</span
					>
				{:else if profitChangePctDisplay === '+9999' || profitChangePctDisplay === '-9999'}
					<span
						class="text-base font-semibold"
						style="color: {profitColor}"
						title={profitChangePct.toFixed(2) + '%'}>{profitChangePctDisplay}%</span
					>
				{:else}
					<span class="text-base font-semibold" style="color: {profitColor}"
						>{profitChangePctDisplay}%</span
					>
				{/if}
			</div>
		</div>

		<div class="max-md:pt-2 block">
			<PillToggle
				options={chartOptionOptions.map((opt) => ({ value: opt.value, label: opt.label }))}
				selected={chartOption}
				onSelect={selectChartOption}
				direction="horizontal"
			/>
		</div>
	{/if}
</div>

<div style="position: relative; width: 100%; height: 400px;">
	<canvas bind:this={canvas} class="w-screen max-w-full block" style="height: 400px;"></canvas>
</div>

<div class="background-fade flex h-20 -mt-2.5 w-full pointer-events-none z-1 relative" style="--fade-color: {fadeColor}">
	<div class="flex justify-center items-center mt-5 absolute w-full left-0 top-0 z-1 pointer-events-auto">
		<div class="hidden sm:block">
			<PillToggle
				options={periodOptions.map((opt) => ({ value: opt.value, label: opt.label }))}
				selected={selectedPeriod}
				onSelect={selectPeriod}
				direction="horizontal"
			/>
		</div>
		<div class="block sm:hidden">
			<DropDown
				options={periodOptions.map((opt) => ({ value: opt.value, label: opt.label }))}
				value={selectedPeriod}
				onchange={selectPeriod}
				placeholder="Select period"
			/>
		</div>
	</div>
</div>

<style>
	.background-fade {
		background: linear-gradient(
			to bottom,
			transparent 0px,
			var(--fade-color) 10%,
			transparent 20%
		);
	}
</style>
