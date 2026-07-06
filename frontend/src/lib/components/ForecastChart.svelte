<script lang="ts">
	import { onMount } from 'svelte';
	import Chart from 'chart.js/auto';
	import type { ForecastDayDto } from '../services/forecastService';

	let { forecast = [] }: { forecast: ForecastDayDto[] } = $props();
	let canvas: HTMLCanvasElement;
	let chart: Chart;
	let isMobile = false;

	function initializeChart() {
		if (!canvas || forecast.length === 0) return;

		// Destroy existing chart if it exists
		if (chart) {
			chart.destroy();
		}

		// Sort by date to ensure proper order
		const sorted = [...forecast].sort((a, b) => a.date.localeCompare(b.date));

		const dates = sorted.map((f) => f.date);
		// Use pre-computed portfolio values from the backend
		const medians = sorted.map((day) => day.portfolio.median);
		const lowers = sorted.map((day) => day.portfolio.lower);
		const uppers = sorted.map((day) => day.portfolio.upper);

		// Create labels - show month name at first day of each new month
		const labels = dates.map((date, idx) => {
			const dateObj = new Date(date + 'T00:00:00');
			const isFirstDayOfMonth = dateObj.getDate() === 1;
			if (isFirstDayOfMonth || idx === 0) {
				return dateObj.toLocaleString('en-US', { month: 'short' });
			}
			// Check if this is the first occurrence of a new month
			if (idx > 0) {
				const prevDate = new Date(dates[idx - 1] + 'T00:00:00');
				if (dateObj.getMonth() !== prevDate.getMonth()) {
					return dateObj.toLocaleString('en-US', { month: 'short' });
				}
			}
			return '';
		});

		const ctx = canvas.getContext('2d');
		if (!ctx) return;

		chart = new Chart(ctx, {
			type: 'line',
			data: {
				labels: labels,
				datasets: [
					{
						label: 'Upper Bound',
						data: uppers,
						borderColor: '#c14bac21',
						backgroundColor: '#c14bac21',
						borderWidth: 0,
						fill: false,
						pointRadius: 0,
						pointHoverRadius: 0,
						tension: 0.3
					},
					{
						label: 'Lower Bound',
						data: lowers,
						borderColor: '#c14bac21',
						backgroundColor: '#c14bac21',
						borderWidth: 0,
						fill: '-1',
						pointRadius: 0,
						pointHoverRadius: 0,
						tension: 0.3
					},
					{
						label: 'Median',
						data: medians,
						borderColor: '#c14bac',
						backgroundColor: 'transparent',
						borderWidth: 2,
						fill: false,
						pointRadius: 0,
						pointHoverRadius: isMobile ? 5 : 6,
						pointBackgroundColor: '#c14bac',
						pointBorderColor: '#c14bac',
						pointBorderWidth: 2,
						tension: 0.3
					}
				]
			},
			options: {
				responsive: true,
				maintainAspectRatio: false,
				interaction: {
					intersect: false,
					mode: 'index',
					axis: 'x'
				},
				plugins: {
					legend: {
						display: false
					},
					title: {
						display: false
					},
					filler: {
						propagate: true
					},
					tooltip: {
						enabled: true,
						backgroundColor: 'rgba(0, 0, 0, 0.8)',
						titleColor: '#fff',
						bodyColor: '#fff',
						borderColor: 'rgba(255, 255, 255, 0.2)',
						borderWidth: 1,
						padding: isMobile ? 8 : 12,
						displayColors: true,
						titleFont: {
							size: isMobile ? 10 : 12
						},
						bodyFont: {
							size: isMobile ? 9 : 11
						},
						callbacks: {
							title: (context) => {
								if (context.length > 0) {
									return dates[context[0].dataIndex];
								}
								return '';
							},
							label: function (context) {
								if (context.datasetIndex === 2) {
									// Use pre-computed portfolio values
									const dayData = sorted[context.dataIndex];
									return [
										`Portfolio Median: ${dayData.portfolio.median.toLocaleString('en-US', {
											style: 'currency',
											currency: 'CHF',
											maximumFractionDigits: 2
										})}`,
										`Range: ${dayData.portfolio.lower.toLocaleString('en-US', {
											style: 'currency',
											currency: 'CHF',
											maximumFractionDigits: 2
										})} - ${dayData.portfolio.upper.toLocaleString('en-US', {
											style: 'currency',
											currency: 'CHF',
											maximumFractionDigits: 2
										})}`
									];
								}
								return '';
							}
						}
					}
				},
				scales: {
					y: {
						type: 'linear',
						display: true,
						position: 'left',
						ticks: {
							callback: function (value) {
								return (value as number).toLocaleString('en-US', {
									style: 'currency',
									currency: 'CHF',
									maximumFractionDigits: 0
								});
							},
							color: 'rgba(255, 255, 255, 0.7)',
							font: {
								size: isMobile ? 9 : 12
							}
						},
						grid: {
							color: 'rgba(255, 255, 255, 0.1)'
						}
					},
					x: {
						type: 'category',
						display: true,
						grid: {
							display: false
						},
						ticks: {
							font: {
								size: isMobile ? 9 : 12
							},
							color: 'rgba(255, 255, 255, 0.7)',
							autoSkip: false,
							maxRotation: isMobile ? 45 : 0,
							minRotation: isMobile ? 45 : 0,
							callback: function (value, index) {
								// Display the label for this index
								return labels[index as number] || '';
							}
						}
					}
				}
			}
		});
	}

	onMount(() => {
		// Detect mobile
		isMobile = window.innerWidth < 768;

		// Handle window resize
		const handleResize = () => {
			const wasMobile = isMobile;
			isMobile = window.innerWidth < 768;
			if (wasMobile !== isMobile) {
				chart?.resize();
			}
		};

		window.addEventListener('resize', handleResize);

		return () => {
			window.removeEventListener('resize', handleResize);
			chart?.destroy();
		};
	});

	// Reactively update chart when forecast changes
	$effect(() => {
		if (canvas) {
			initializeChart();
		}
	});
</script>

<div class="relative bg-transparent h-[280px] md:h-[400px]">
	<canvas bind:this={canvas}></canvas>
</div>
