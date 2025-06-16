<script lang="ts">
	import { onMount } from 'svelte';
	import Chart from 'chart.js/auto';
	import { Tooltip } from 'chart.js';
	import type { PositionSnapshot } from '../services/positionService';
	import type { TooltipItem, ChartTypeRegistry } from 'chart.js';

	export let snapshots: PositionSnapshot[] = [];
	let canvas: HTMLCanvasElement;
	let chart: Chart;

	// Register the custom positioner every time before chart creation
	(Tooltip as any).positioners.fixedY = function (
		items: TooltipItem<'line'>[],
		event: MouseEvent,
		options: any
	) {
        if (!items.length) return false;
		const x = items[0].element.x;
		// Chart.js v4: get chart instance from the global Chart registry
		const chart = Chart.getChart(canvas);
		const chartWidth = chart?.width ?? 600;
		const boxWidth = 150;
		const margin = 30;
		let boxX;
		if (x > chartWidth - boxWidth - margin) {
			boxX = x - margin;
		} else {
			boxX = x + boxWidth + margin;
		}
		return { x: boxX, y: 100 };
	};

	onMount(() => {
		if (chart) chart.destroy();
		if (!snapshots.length) return;

		// Debug: log the tooltip options to ensure position: 'fixedY' is set
		const tooltipOptions = {
			enabled: true,
			backgroundColor: 'rgba(34,34,40,0.98)',
			borderColor: '#6366f1',
			borderWidth: 1.5,
			titleColor: '#a5b4fc',
			bodyColor: '#fff',
			displayColors: true,
			bodyFont: { family: 'Inter, Segoe UI, Arial, sans-serif', size: 14, weight: 'bold' },
			titleFont: { family: 'Inter, Segoe UI, Arial, sans-serif', size: 11, weight: 'normal' },
			boxHeight: 16,
			boxWidth: 16,
			boxPadding: 0,
			borderRadius: 14,
			boxBorderRadius: 20, // Chart.js v4+ for color box border radius
			caretSize: 0,
			caretPadding: 0,
			position: 'fixedY',
			callbacks: {
				title: (items: any[]) => `Date: ${items[0].label}`,
				labelColor: (item: any) => {
					return {
						borderColor: item.dataset.borderColor,
						backgroundColor: item.dataset.borderColor,
						borderWidth: 1,
						borderRadius: 0
					};
				},
				label: (item: any) => {
					// No manual padding, let Chart.js handle alignment
					return `${item.dataset.label}: ${item.formattedValue}`;
				}
			}
		};
        
		chart = new Chart(canvas, {
			type: 'line',
			data: {
				labels: snapshots.map((s) => s.date.slice(0, 10)),
				datasets: [
					{
						label: 'Total Value',
						data: snapshots.map(
							(s) => s.invested + (s.unrealizedGain ?? 0) + (s.realizedGain ?? 0)
						),
						borderColor: 'rgba(16,185,129,1)',
						backgroundColor: 'rgba(16,185,129,0.15)',
						fill: true,
						tension: 0.5,
						pointRadius: 0, // Hide all points
						pointHoverRadius: 6, // Show only on hover
						borderWidth: 2,
						order: 1,
						pointBackgroundColor: 'rgba(16,185,129,1)',
						pointBorderColor: '#222',
						pointStyle: 'circle'
					},
					{
						label: 'Invested',
						data: snapshots.map((s) => s.invested),
						borderColor: 'rgba(99,102,241,1)',
						backgroundColor: 'rgba(99,102,241,0.25)',
						fill: true,
						tension: 0.5,
						pointRadius: 0, // Hide all points
						pointHoverRadius: 6, // Show only on hover
						borderWidth: 3,
						order: 2,
						pointBackgroundColor: 'rgba(99,102,241,1)',
						pointBorderColor: '#222',
						pointStyle: 'circle'
					}
				]
			},
			options: {
				responsive: false,
				plugins: {
					legend: { display: false },
					title: { display: false },
					// @ts-expect-error: custom positioner is registered at runtime
					tooltip: tooltipOptions
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
					y: {
						display: false,
						grid: { display: false }
					}
				}
			}
		});
		return () => chart?.destroy();
	});
</script>

<canvas bind:this={canvas} style="height: 400px;"></canvas>

<style>
	canvas {
		width: 100vw;
		max-width: 100%;
		display: block;
        margin-bottom: 2rem;
		background: #18181b;
		border: 1px solid #333;
		box-shadow: 0 4px 32px #000a;
	}

	:global(body) {
		background: #18181b;
		color: #fff;
		font-family: 'Inter', sans-serif;
	}
</style>
