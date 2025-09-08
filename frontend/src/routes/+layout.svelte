<script lang="ts">
	import '../app.css';
	import { onMount } from 'svelte';
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { dataStore } from '$lib/stores/dataStore';
	import type { Snippet } from 'svelte';

	let { children }: { children: Snippet } = $props();

	const links = [
		{ key: '/portfolio', icon: 'fa-solid fa-chart-line', label: 'Portfolio' },
		{ key: '/investments', icon: 'fa-solid fa-money-bill', label: 'Investments' },
		{ key: '/settings', icon: 'fa-solid fa-gear', label: 'Settings' }
	];

	let showLoading = $state(true);
	let fadeOut = $state(false);

	// Reactive values that track the store
	let loading = $state(dataStore.loading);
	let error = $state(dataStore.error);

	// Subscribe to store changes
	$effect(() => {
		const unsubscribe = dataStore.subscribe(() => {
			loading = dataStore.loading;
			error = dataStore.error;
		});
		return unsubscribe;
	});

	$effect(() => {
		if (!loading && showLoading && !fadeOut) {
			// Start fade-out
			fadeOut = true;
		}
	});

	onMount(async () => {
		await dataStore.initialize();
	});

	function navigateTo(path: string) {
		goto(path);
	}

	// Get current path for active state
	let currentPath = $derived($page.url.pathname);
</script>

<div class="grid min-h-screen grid-cols-[70px_1fr]">
	<div
		class="bg-card fixed bottom-0 left-0 top-0 flex w-[70px] flex-col items-center justify-between p-1"
	>
		<div class="flex h-full flex-col">
			{#each links as { key, icon, label }, i}
				<button
					type="button"
					class="nav-item flex h-[50px] w-[50px] flex-col items-center justify-center text-2xl {key ===
					'/settings'
						? 'mt-auto'
						: 'my-1'} hover:scale-115 transition-transform hover:cursor-pointer {currentPath === key
						? 'active'
						: ''}"
					aria-label={label}
					title={label}
					onclick={() => navigateTo(key)}
				>
					<i class={icon}></i>
					<span class="display md:hidden">{label}</span>
				</button>
				{#if i < links.length - 2}
					<hr class="color-muted mx-auto my-1 w-4 border-t" />
				{/if}
			{/each}
		</div>
	</div>
	<div class="col-2 relative">
		{#if showLoading}
			<div
				class="loading-screen absolute top-0 left-0 z-50 flex flex-col items-center justify-center bg-background-backdrop color-default w-full h-full"
				class:fade-out={fadeOut}
				ontransitionend={() => {
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
		{#if !loading && !showLoading}
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
					<button class="btn btn-big btn-error" onclick={() => dataStore.refreshData()}>Retry</button>
				</div>
			{:else}
				{@render children?.()}
			{/if}
		{/if}
	</div>
</div>

<style>
	.nav-item.active {
		color: var(--color-accent);
	}

	.nav-item:hover {
		color: var(--color-primary);
	}

	/* Fade-out effect for loading screen */
	.loading-screen {
		opacity: 1;
		transition: opacity 0.6s cubic-bezier(0.4, 0, 0.2, 1);
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
		background: linear-gradient(135deg, var(--color-primary) 60%, var(--color-secondary) 100%);
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
</style>
