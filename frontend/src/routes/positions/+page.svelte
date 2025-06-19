<script lang="ts">
	import { onMount } from 'svelte';
	import PortfolioChart from '$lib/components/PortfolioChart.svelte';
	import { getPositionSnapshots, type PositionSnapshot } from '$lib/services/positionService';
	import { writable } from 'svelte/store';

	const snapshots = writable<PositionSnapshot[]>([]);
	const loading = writable(true);
	const error = writable<string | null>(null);

	onMount(async () => {
		loading.set(true);
		try {
			const data = await getPositionSnapshots(null, null);
			snapshots.set(data);
			error.set(null);
		} catch (e) {
			error.set((e as Error).message);
		} finally {
			loading.set(false);
		}
	});
</script>

{#if $loading}
	<p>Loading...</p>
{:else if $error}
	<p style="color: red">Error: {$error}</p>
{:else if $snapshots.length === 0}
	<p>No data available.</p>
{:else}
	{#if $snapshots.length}
		<PortfolioChart snapshots={$snapshots} />
	{/if}
{/if}
