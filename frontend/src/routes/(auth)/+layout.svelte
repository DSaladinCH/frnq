<script lang="ts">
	import { onMount } from 'svelte';
	import { dataStore } from '$lib/stores/dataStore';
	import type { Snippet } from 'svelte';

	let { children }: { children: Snippet } = $props();

	let primaryLoading = $state(dataStore.primaryLoading);
	let showLoading = $state(dataStore.primaryLoading);
	let fadeOut = $state(false);

	// Reactive values that track the store
	let error = $state(dataStore.error);

	// Subscribe to store changes
	$effect(() => {
		const unsubscribe = dataStore.subscribe(() => {
			primaryLoading = dataStore.primaryLoading;
			error = dataStore.error;
		});
		return unsubscribe;
	});

	$effect(() => {
		if (!primaryLoading && showLoading && !fadeOut) {
			// Start fade-out when loading completes
			fadeOut = true;
		} else if (primaryLoading && !showLoading) {
			// Reset loading screen when loading starts again
			showLoading = true;
			fadeOut = false;
		}
	});

	// Initialize data store for authenticated routes
	onMount(async () => {
		await dataStore.initialize();
	});
</script>

{#if showLoading}
	<div
		class="loading-screen bg-background-backdrop color-default fixed left-0 top-0 z-50 flex h-screen w-full flex-col items-center justify-center md:absolute md:h-full"
		class:fade-out={fadeOut}
		onanimationend={() => {
			if (fadeOut) {
				showLoading = false;
			}
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

{#if !showLoading}
	{#if error}
		<div class="error-screen">
			<div class="sad-icon" aria-hidden="true">
				<svg
					width="64"
					height="64"
					viewBox="0 0 64 64"
					fill="none"
					xmlns="http://www.w3.org/2000/svg"
					aria-hidden="true"
				>
					<circle cx="32" cy="32" r="28" stroke="#ffb3c6" stroke-width="6" fill="none" />
					<line
						x1="20"
						y1="20"
						x2="44"
						y2="44"
						stroke="#ffb3c6"
						stroke-width="6"
						stroke-linecap="round"
					/>
					<line
						x1="44"
						y1="20"
						x2="20"
						y2="44"
						stroke="#ffb3c6"
						stroke-width="6"
						stroke-linecap="round"
					/>
				</svg>
			</div>
			<h2>There was an error fetching the data</h2>
			<!-- <p class="error-message">{error}</p> -->
			<button class="btn btn-big btn-error" onclick={() => dataStore.refreshData()}
				>Retry</button>
		</div>
	{:else}
		{@render children?.()}
	{/if}
{/if}

<style>
	.loading-screen {
		backdrop-filter: blur(8px);
		background-color: rgba(var(--color-background-rgb), 0.8);
	}

	.loading-screen.fade-out {
		animation: fadeOut 0.8s forwards;
	}

	@keyframes fadeOut {
		to {
			opacity: 0;
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
		background: linear-gradient(135deg, var(--color-primary) 60%, var(--color-secondary) 100%);
		border-radius: 50%;
		animation: bounce 1.2s infinite ease-in-out;
	}

	.dot:nth-child(1) { animation-delay: 0s; }
	.dot:nth-child(2) { animation-delay: 0.2s; }
	.dot:nth-child(3) { animation-delay: 0.4s; }

	@keyframes bounce {
		0%, 80%, 100% {
			transform: translateY(0);
		}
		40% {
			transform: translateY(-24px);
		}
	}

	.fade-in {
		opacity: 0;
		animation: fadeIn 1s forwards;
	}

	@keyframes fadeIn {
		to {
			opacity: 1;
		}
	}

	.pulse {
		animation: pulse 2s infinite;
	}

	@keyframes pulse {
		0% {
			opacity: 0.4;
		}
		50% {
			opacity: 1;
		}
		100% {
			opacity: 0.4;
		}
	}

	.error-screen {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		height: 100vh;
		text-align: center;
		padding: 2rem;
		gap: 1rem;
	}

	.sad-icon {
		margin-bottom: 1rem;
	}

	.error-screen h2 {
		color: var(--color-error);
		margin: 0;
	}

	.btn {
		padding: 0.5rem 1rem;
		border: none;
		border-radius: 0.25rem;
		cursor: pointer;
		font-weight: 500;
		transition: all 0.2s;
	}

	.btn-big {
		padding: 0.75rem 1.5rem;
		font-size: 1.1rem;
	}

	.btn-error {
		background-color: var(--color-error);
		color: white;
	}

	.btn-error:hover {
		background-color: var(--color-error-dark);
	}
</style>