<script lang="ts">
	import { onMount } from 'svelte';
	import PortfolioChart from '$lib/components/PortfolioChart.svelte';
	import { getPositionSnapshots, type PositionSnapshot } from '$lib/services/positionService';
	import { writable } from 'svelte/store';

	const snapshots = writable<PositionSnapshot[]>([]);
	const loading = writable(true);
	const error = writable<string | null>(null);

	// Example: last 30 days
	const to = new Date();
	const from = new Date();
	from.setDate(to.getDate() - 30);

	onMount(async () => {
		loading.set(true);
		try {
			const data = await getPositionSnapshots(from.toISOString(), to.toISOString());
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

	<!-- <table>
		<thead>
			<tr>
				<th>Date</th>
				<th>Symbol</th>
				<th>Amount</th>
				<th>Invested</th>
				<th>Market Price</th>
				<th>Total Value</th>
				<th>Unrealized Gain</th>
			</tr>
		</thead>
		<tbody>
			{#each $snapshots as snap}
				<tr>
					<td>{snap.date.slice(0, 10)}</td>
					<td>{snap.quoteSymbol}</td>
					<td>{snap.amount}</td>
					<td>{snap.invested}</td>
					<td>{snap.marketPricePerUnit}</td>
					<td>{snap.totalValue}</td>
					<td>{snap.unrealizedGain}</td>
				</tr>
			{/each}
		</tbody>
	</table> -->
{/if}

<style>
	table {
		border-collapse: collapse;
		width: 100%;
		margin-bottom: 2rem;
	}
	th,
	td {
		border: 1px solid #ccc;
		padding: 0.5rem;
		text-align: center;
	}
</style>
