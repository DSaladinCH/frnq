<script lang="ts">
	import { onMount } from 'svelte';
	import Chart from 'chart.js/auto';
	import type { ForecastDayDto } from '../services/forecastService';
	import PillToggle from './PillToggle.svelte';
	import DropDown from './DropDown.svelte';
	import { dataStore } from '$lib/stores/dataStore';

	let {
		forecast = [],
		filterMode = 'full',
		filterGroupId = null,
		filterQuoteId = null,
		onTypeChange
	}: {
		forecast: ForecastDayDto[];
		filterMode?: 'full' | 'group' | 'quote';
		filterGroupId?: string | null;
		filterQuoteId?: number | null;
		onTypeChange?: (period: string) => void;
	} = $props();
	let canvas: HTMLCanvasElement;
	let chart: Chart;
	let isMobile = false;

	function getChartData(sortedForecast: ForecastDayDto[]) {
		// Determine which band to use based on filter mode
		if (filterMode === 'full') {
			// Full view: use portfolio band
			return sortedForecast.map(day => ({
				median: day.portfolio.median,
				lower: day.portfolio.lower,
				upper: day.portfolio.upper
			}));
		} else if (filterMode === 'group' && filterGroupId) {
			// Group view: find the group and use its band
			const numGroupId = parseInt(filterGroupId);
			return sortedForecast.map(day => {
				const group = day.groups.find(g => g.groupId === numGroupId);
				return {
					median: group?.band.median ?? 0,
					lower: group?.band.lower ?? 0,
					upper: group?.band.upper ?? 0
				};
			});
		} else if (filterMode === 'quote' && filterQuoteId != null) {
			// Quote view: find the quote and use its band
			return sortedForecast.map(day => {
				const quote = day.quotes.find(q => q.quoteId === filterQuoteId);
				return {
					median: quote?.band.median ?? 0,
					lower: quote?.band.lower ?? 0,
					upper: quote?.band.upper ?? 0
				};
			});
		}
		return [];
	}

	function initializeChart() {
		if (!canvas || forecast.length === 0) return;

		// Destroy existing chart if it exists
		if (chart) {
			chart.destroy();
		}

		// Sort by date to ensure proper order
		const sorted = [...forecast].sort((a, b) => a.date.localeCompare(b.date));

		const dates = sorted.map((f) => f.date);
		const chartData = getChartData(sorted);
		const medians = chartData.map(d => d.median);
		const lowers = chartData.map(d => d.lower);
		const uppers = chartData.map(d => d.upper);

		// Calculate date range to determine label strategy
		const firstDate = new Date(dates[0] + 'T00:00:00');
		const lastDate = new Date(dates[dates.length - 1] + 'T00:00:00');
		const rangeInDays = (lastDate.getTime() - firstDate.getTime()) / (1000 * 60 * 60 * 24);
		const rangeInYears = rangeInDays / 365.25;

		// Calculate tick indices for year labels
		let tickIndices: number[] = [];
		if (rangeInDays >= 730) {
			const yearInterval = rangeInYears > 15 ? 3 : rangeInYears > 8 ? 2 : 1;
			
			// Find all indices where we should show year labels
			for (let i = 0; i < dates.length; i++) {
				const dateObj = new Date(dates[i] + 'T00:00:00');
				const yearsSinceFirst = dateObj.getFullYear() - firstDate.getFullYear();
				
				// Show year on first index and every yearInterval years
				if (i === 0 || yearsSinceFirst % yearInterval === 0) {
					// Only add if it's a new year or first index
					if (i === 0 || (i > 0 && new Date(dates[i - 1] + 'T00:00:00').getFullYear() !== dateObj.getFullYear())) {
						tickIndices.push(i);
					}
				}
			}
		}

		// Create labels for display
		const labels = dates.map((date, idx) => {
			if (rangeInDays >= 730 && tickIndices.includes(idx)) {
				const dateObj = new Date(date + 'T00:00:00');
				return dateObj.toLocaleString('en-US', { year: 'numeric' });
			}
			
			const dateObj = new Date(date + 'T00:00:00');
			const prevDate = idx > 0 ? new Date(dates[idx - 1] + 'T00:00:00') : null;

			// For ranges >= 1 year: show months but skip some to avoid overlap
			if (rangeInDays >= 365 && rangeInDays < 730) {
				if (idx === 0 || dateObj.getMonth() !== prevDate?.getMonth()) {
					// Show month names, but only every other month if range is large
					const monthInterval = rangeInDays > 500 ? 2 : 1;
					const monthsSinceFirst = dateObj.getMonth() - firstDate.getMonth() + (dateObj.getFullYear() - firstDate.getFullYear()) * 12;
					if (monthsSinceFirst % monthInterval === 0) {
						return dateObj.toLocaleString('en-US', { month: 'short' });
					}
				}
				return '';
			}

			// For ranges < 1 year: show all month changes
			if (rangeInDays < 365) {
				if (idx === 0 || dateObj.getMonth() !== prevDate?.getMonth()) {
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
									// Use the correct data based on filter mode
									const dataPoint = chartData[context.dataIndex];
									return [
										`Median: ${dataPoint.median.toLocaleString('en-US', {
											style: 'currency',
											currency: 'CHF',
											maximumFractionDigits: 2
										})}`,
										`Range: ${dataPoint.lower.toLocaleString('en-US', {
											style: 'currency',
											currency: 'CHF',
											maximumFractionDigits: 2
										})} - ${dataPoint.upper.toLocaleString('en-US', {
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
		selectedType = dataStore.getSavedForecastType() ? 'predict' : 'current';

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

	// Reactively update chart when forecast or filters change
	$effect(() => {
		if (canvas && forecast && filterMode !== undefined && filterGroupId !== undefined && filterQuoteId !== undefined) {
			initializeChart();
		}
	});

	// Period type for strict typing
	const forecastType = [
		{ value: 'current', label: 'With current portfolio' },
		{ value: 'predict', label: 'Predict future investments' }
	] as const;

	type Period = (typeof forecastType)[number]['value'];
	const FORECAST_TYPE_STORAGE_KEY = 'forecastChart.selectedType';
	let selectedType: Period = $state('current');

	function selectType(val: string) {
		selectedType = val as Period;
		// Save to localStorage
		try {
			localStorage.setItem(FORECAST_TYPE_STORAGE_KEY, selectedType);
		} catch {}
		// Notify parent component
		if (onTypeChange) {
			onTypeChange(selectedType);
		}
	}
</script>

<div class="relative bg-transparent h-[280px] md:h-[400px]">
	<canvas bind:this={canvas}></canvas>
</div>

<div class="bflex h-20 -mt-2.5 w-full pointer-events-none z-1 relative">
	<div class="flex justify-center items-center mt-5 absolute w-full left-0 top-0 z-1 pointer-events-auto">
		<div class="hidden sm:block">
			<PillToggle
				options={forecastType.map((opt) => ({ value: opt.value, label: opt.label }))}
				selected={selectedType}
				onSelect={selectType}
				direction="horizontal"
			/>
		</div>
		<div class="block sm:hidden">
			<DropDown
				options={forecastType.map((opt) => ({ value: opt.value, label: opt.label }))}
				value={selectedType}
				onchange={selectType}
				placeholder="Select period"
			/>
		</div>
	</div>
</div>
