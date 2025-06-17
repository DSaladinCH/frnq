
<script lang="ts">
import { onMount } from 'svelte';
import Chart from 'chart.js/auto';
import type { PositionSnapshot } from '../services/positionService';

export let snapshots: PositionSnapshot[] = [];
let canvas: HTMLCanvasElement;
let chart: Chart;
let tooltipEl: HTMLDivElement;

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
				color = dataPoints[i].dataset.borderColor || dataPoints[i].dataset.backgroundColor || '#888';
				if (Array.isArray(color)) {
					color = `rgba(${color.join(',')})`;
				} else if (typeof color === 'object' && color !== null && 'r' in color && 'g' in color && 'b' in color) {
					const { r, g, b, a } = color;
					color = typeof a !== 'undefined' ? `rgba(${r},${g},${b},${a})` : `rgb(${r},${g},${b})`;
				}
			}
			// Split label and value for alignment
			let label = dataPoints[i]?.dataset?.label || '';
			let value = bodyLines[i][0];
			// Remove label and following ' :' from value if present
			if (typeof value === 'string' && label && value.startsWith(label + ' :')) {
				value = value.slice((label + ' :').length).trim();
			} else if (typeof value === 'string' && label && value.startsWith(label + ':')) {
				value = value.slice((label + ':').length).trim();
			}
			// Format as number with 2 decimals if possible
			const num = parseFloat(value.replace(/[^0-9eE.,\-+]/g, '').replace(',', '.'));
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
	if (mouseEvent && mouseEvent.clientX) {
		pointX = mouseEvent.clientX;
	} else if (tooltip.caretX !== undefined) {
		const rect = chart.canvas.getBoundingClientRect();
		pointX = rect.left + window.scrollX + tooltip.caretX;
	}
	// Default: right of point
	left = pointX + margin;
	// If would overflow right, show to the left
	if (left + tooltipWidth > window.innerWidth - margin) {
		left = pointX - tooltipWidth - margin;
	}
	const fixedTop = 50; // px from top of viewport
	tooltipEl.style.position = 'fixed';
	tooltipEl.style.left = left + 'px';
	tooltipEl.style.top = fixedTop + 'px';
	tooltipEl.style.opacity = '1';
	tooltipEl.style.font = tooltip.options.bodyFont?.string || '15px Inter, Segoe UI, Arial, sans-serif';
	tooltipEl.style.zIndex = '1000';
	tooltipEl.style.background = 'rgba(34,34,40,0.98)';
	tooltipEl.style.border = '1.5px solid #333'; // Subtle border for dark mode
	tooltipEl.style.borderRadius = '16px';
	tooltipEl.style.boxShadow = '0 8px 32px #000c';
	tooltipEl.style.color = '#fff';
	tooltipEl.style.padding = '1rem 1.2rem 0.7rem 1.2rem';
	tooltipEl.style.minWidth = '180px';
	tooltipEl.style.height = '110px'; // Fixed height
	tooltipEl.style.overflowY = 'auto';
}

onMount(() => {
	if (chart) chart.destroy();
	if (!snapshots.length) return;

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
					pointRadius: 0,
					pointHoverRadius: 6,
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
					pointRadius: 0,
					pointHoverRadius: 6,
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
				y: {
					display: false,
					grid: { display: false }
				}
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
</script>

<canvas bind:this={canvas} style="height: 400px;"></canvas>

<!-- The tooltip element is created dynamically and appended to body -->

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
