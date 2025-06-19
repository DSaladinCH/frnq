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
	<div class="loading-screen">
		<div class="bouncing-dots">
			<div class="dot"></div>
			<div class="dot"></div>
			<div class="dot"></div>
		</div>
		<p class="fade-in pulse">Loading positions...</p>
	</div>
{:else if $error}
	<p style="color: red">Error: {$error}</p>
{:else if $snapshots.length === 0}
	<p>No data available.</p>
{:else}
	{#if $snapshots.length}
		<PortfolioChart snapshots={$snapshots} />
	{/if}
{/if}

<style>
.loading-screen {
	position: fixed;
	top: 0;
	left: 0;
	width: 100vw;
	height: 100vh;
	z-index: 1000;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	background: rgba(255, 255, 255, 0.92);
	color: #222;
}
@media (prefers-color-scheme: dark) {
	.loading-screen {
		background: rgba(24, 24, 32, 0.96);
		color: #f3f3f3;
	}
	.bouncing-dots .dot {
		background: linear-gradient(135deg, #8e44ad 60%, #3498db 100%);
	}
}
.bouncing-dots {
	display: flex;
	gap: 10px;
	margin-bottom: 18px;
}
.dot {
	width: 16px;
	height: 16px;
	background: linear-gradient(135deg, #3498db 60%, #8e44ad 100%);
	border-radius: 50%;
	animation: bounce 1.2s infinite ease-in-out;
}
.dot:nth-child(2) {
	animation-delay: 0.2s;
}
.dot:nth-child(3) {
	animation-delay: 0.4s;
}
@keyframes bounce {
	0%, 80%, 100% { transform: translateY(0); }
	40% { transform: translateY(-24px); }
}
.fade-in {
	animation: fadeIn 1.2s ease-in;
}
@keyframes fadeIn {
	from { opacity: 0; }
	to { opacity: 1; }
}
.pulse {
	animation: pulse 1.8s infinite;
}
@keyframes pulse {
	0% { opacity: 1; }
	50% { opacity: 0.5; }
	100% { opacity: 1; }
}
</style>
