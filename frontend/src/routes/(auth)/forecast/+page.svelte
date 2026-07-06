<script lang="ts">
	import { onMount } from 'svelte';
	import PageHead from '$lib/components/PageHead.svelte';
	import ForecastChart from '$lib/components/ForecastChart.svelte';
	import { getForecast, type ForecastDto } from '$lib/services/forecastService';
	import { dataStore } from '$lib/stores/dataStore';
	import PageTitle from '$lib/components/PageTitle.svelte';

	// Use $state for reactive tracking and explicit subscription for reliable updates
	let forecast = $state(dataStore.forecast);
	let secondaryLoading = $state(dataStore.secondaryLoading);

	// Subscribe to store changes for reliable reactivity
	$effect(() => {
		const unsubscribeData = dataStore.subscribe(() => {
			forecast = dataStore.forecast;
			secondaryLoading = dataStore.secondaryLoading;
		});

		return () => {
			unsubscribeData();
		};
	});

	// Calculate statistics
	let stats = $derived.by(() => {
		if (forecast.length === 0) return null;

		const medians = forecast.map((f) => f.median);
		const lowers = forecast.map((f) => f.lower);
		const uppers = forecast.map((f) => f.upper);

		const latestMedian = forecast[forecast.length - 1].median;
		const firstMedian = forecast[0].median;
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
</script>

<PageHead title="Forecast" />

<div class="xs:p-8 p-4">
	<PageTitle title="Forecast" icon="fa-solid fa-compass" />
	
	<div class="mb-6">
		<ForecastChart {forecast} />
	</div>

	{#if stats}
		<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
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
</div>

<style>
	:global(body) {
		margin-bottom: 4.5rem;
	}
</style>
