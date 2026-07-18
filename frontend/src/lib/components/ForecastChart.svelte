<script lang="ts">
	import { onMount } from 'svelte';
	import Chart from 'chart.js/auto';
	import 'chartjs-adapter-date-fns';
	import type { ForecastDayDto } from '../services/forecastService';
	import PillToggle from './PillToggle.svelte';
	import DropDown from './DropDown.svelte';
	import { dataStore } from '$lib/stores/dataStore';

	let {
		forecast = [],
		filterMode = 'full',
		filterGroupId = null,
		filterQuoteId = null,
		onTypeChange,
		onDurationChange
	}: {
		forecast: ForecastDayDto[];
		filterMode?: 'full' | 'group' | 'quote';
		filterGroupId?: string | null;
		filterQuoteId?: number | null;
		onTypeChange?: (period: string) => void;
		onDurationChange?: (duration: Duration) => void;
	} = $props();
	let canvas: HTMLCanvasElement;
	let chart: Chart<'line', { x: string; y: number }[]>;

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

		const ctx = canvas.getContext('2d');
		if (!ctx) return;

		chart = new Chart(ctx, {
			type: 'line',
			data: {
				datasets: [
					{
						label: 'Upper Bound',
						data: dates.map((date, i) => ({ x: date, y: uppers[i] })),
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
						data: dates.map((date, i) => ({ x: date, y: lowers[i] })),
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
						data: dates.map((date, i) => ({ x: date, y: medians[i] })),
						borderColor: '#c14bac',
						backgroundColor: 'transparent',
						borderWidth: 2,
						fill: false,
						pointRadius: 0,
						pointHoverRadius: 6,
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
						padding: 12,
						displayColors: true,
						titleFont: {
							size: 12
						},
						bodyFont: {
							size: 11
						},
						callbacks: {
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
									notation: 'compact',
									maximumFractionDigits: 1
								});
							},
							color: 'rgba(255, 255, 255, 0.7)',
							font: {
								size: 12
							}
						},
						grid: {
							color: 'rgba(255, 255, 255, 0.1)'
						}
					},
					x: {
						type: 'time',
						time: {
							minUnit: 'month',
							tooltipFormat: 'MMM d, yyyy',
							displayFormats: {
								month: 'MMM yyyy',
								year: 'yyyy'
							}
						},
						display: true,
						grid: {
							display: false
						},
						ticks: {
							autoSkip: true,
							autoSkipPadding: 40,
							maxRotation: 0,
							font: {
								size: 12
							},
							color: 'rgba(255, 255, 255, 0.7)'
						}
					}
				}
			}
		});
	}

	onMount(() => {
		selectedType = dataStore.getSavedForecastType() ? 'predict' : 'current';
		selectedDuration = dataStore.getSavedForecastDuration().toString() as Duration;

		return () => {
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

	const forecastDuration = [
		{ value: '1', label: '1 Year' },
		{ value: '3', label: '3 Years' },
		{ value: '10', label: '10 Years' }
	] as const;

	type Period = (typeof forecastType)[number]['value'];
	type Duration = (typeof forecastDuration)[number]['value'];
	const FORECAST_TYPE_STORAGE_KEY = 'forecastChart.selectedType';
	const FORECAST_DURATION_STORAGE_KEY = 'forecastChart.selectedDuration';
	let selectedType: Period = $state('current');
	let selectedDuration: Duration = $state('1');

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

	function selectDuration(val: string) {
		selectedDuration = val as Duration;
		// Save to localStorage
		try {
			localStorage.setItem(FORECAST_DURATION_STORAGE_KEY, selectedDuration);
		} catch {}

		// Notify parent component
		if (onDurationChange) {
			onDurationChange(selectedDuration);
		}
	}
</script>

<div class="relative bg-transparent h-[280px] md:h-[400px]">
	<canvas bind:this={canvas}></canvas>
</div>

<div class="bflex h-max -mt-2.5 w-full pointer-events-none z-1 relative">
	<div class="flex justify-center items-center mt-5 w-full z-1 pointer-events-auto">
		<div class="hidden md:flex gap-4">
			<PillToggle
				options={forecastType.map((opt) => ({ value: opt.value, label: opt.label }))}
				selected={selectedType}
				onSelect={selectType}
				direction="horizontal"
			/>

			<PillToggle
				options={forecastDuration.map((opt) => ({ value: opt.value, label: opt.label }))}
				selected={selectedDuration}
				onSelect={selectDuration}
				direction="horizontal"
			/>
		</div>
		<div class="flex flex-wrap justify-center md:hidden gap-4">
			<DropDown
				options={forecastType.map((opt) => ({ value: opt.value, label: opt.label }))}
				value={selectedType}
				onchange={selectType}
				placeholder="Select period"
			/>

			<DropDown
				options={forecastDuration.map((opt) => ({ value: opt.value, label: opt.label }))}
				value={selectedDuration}
				onchange={selectDuration}
				placeholder="Select duration"
			/>
		</div>
	</div>
</div>
