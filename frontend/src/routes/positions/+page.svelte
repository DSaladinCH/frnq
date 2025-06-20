<script lang="ts">
	import { onMount } from 'svelte';
	import PortfolioChart from '$lib/components/PortfolioChart.svelte';
	import { getPositionSnapshots, type PositionSnapshot } from '$lib/services/positionService';
	import { writable } from 'svelte/store';

	const snapshots = writable<PositionSnapshot[]>([]);
	const loading = writable(true);
	const error = writable<string | null>(null);

	import { tick } from 'svelte';

	let showLoading = true;
	let fadeOut = false;

	$: if (!$loading && showLoading && !fadeOut) {
		// Start fade-out
		fadeOut = true;
	}

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

{#if showLoading}
	<div
		class="loading-screen"
		class:fade-out={fadeOut}
		on:transitionend={() => {
			if (fadeOut) showLoading = false;
		}}
	>
		<div class="bouncing-dots">
			<div class="dot"></div>
			<div class="dot"></div>
			<div class="dot"></div>
		</div>
		<p class="fade-in pulse">Loading positions...</p>
	</div>
{/if}
{#if !$loading && !showLoading}
	{#if $error}
		<div class="error-screen">
			   <div class="sad-icon" aria-hidden="true">
				   <svg width="64" height="64" viewBox="0 0 64 64" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
					   <circle cx="32" cy="32" r="28" stroke="#ffb3c6" stroke-width="6" fill="none"/>
					   <line x1="20" y1="20" x2="44" y2="44" stroke="#ffb3c6" stroke-width="6" stroke-linecap="round"/>
					   <line x1="44" y1="20" x2="20" y2="44" stroke="#ffb3c6" stroke-width="6" stroke-linecap="round"/>
				   </svg>
			   </div>
			<h2>There was an error fetching the data</h2>
			<!-- <p class="error-message">{$error}</p> -->
			<button class="btn btn-error" on:click={() => location.reload()}>Reload</button>
		</div>
	{:else if $snapshots.length === 0}
		<p>No data available.</p>
	{:else if $snapshots.length}
		<PortfolioChart snapshots={$snapshots} />
	{/if}
{/if}

<style>
	/* Error screen styles */
	.error-screen {
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
		background: rgba(24, 24, 32, 0.96);
		color: #ffb3c6;
		text-align: center;
		animation: fadeIn 0.8s;
	}

	.sad-icon {
		font-size: 4rem;
		margin-bottom: 18px;
	   animation: shake 1.2s infinite alternate;
	}

	.error-screen h2 {
		margin: 0 0 1.5rem 0;
		font-size: 1.6rem;
		font-weight: 600;
	}

   @keyframes shake {
	   0% {
		   transform: translateX(0);
	   }
	   15% {
		   transform: translateX(-2px) rotate(-2deg);
	   }
	   30% {
		   transform: translateX(2px) rotate(2deg);
	   }
	   45% {
		   transform: translateX(-1.5px) rotate(-1.5deg);
	   }
	   60% {
		   transform: translateX(1px) rotate(1deg);
	   }
	   75% {
		   transform: translateX(0);
	   }
	   100% {
		   transform: translateX(0);
	   }
   }

	/* Fade-out effect for loading screen */
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
		background: rgba(24, 24, 32, 0.96);
		color: #f3f3f3;
		opacity: 1;
		transition: opacity 0.6s cubic-bezier(0.4, 0, 0.2, 1);
		pointer-events: all;
	}
	.loading-screen.fade-out {
		opacity: 0;
		pointer-events: none;
	}
	.bouncing-dots {
		display: flex;
		gap: 10px;
		margin-bottom: 18px;
	}
	.bouncing-dots .dot {
		width: 16px;
		height: 16px;
		background: linear-gradient(135deg, #8e44ad 60%, #3498db 100%);
		border-radius: 50%;
		animation: bounce 1.2s infinite ease-in-out;
	}
	.bouncing-dots .dot:nth-child(2) {
		animation-delay: 0.2s;
	}
	.bouncing-dots .dot:nth-child(3) {
		animation-delay: 0.4s;
	}
	@keyframes bounce {
		0%,
		80%,
		100% {
			transform: translateY(0);
		}
		40% {
			transform: translateY(-24px);
		}
	}
	.fade-in {
		animation: fadeIn 1.2s ease-in;
	}
	@keyframes fadeIn {
		from {
			opacity: 0;
		}
		to {
			opacity: 1;
		}
	}
	.pulse {
		animation: pulse 1.8s infinite;
	}
	@keyframes pulse {
		0% {
			opacity: 1;
		}
		50% {
			opacity: 0.5;
		}
		100% {
			opacity: 1;
		}
	}
</style>
