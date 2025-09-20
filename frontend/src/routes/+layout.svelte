<script lang="ts">
	import '../app.css';
	import { onMount } from 'svelte';
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { dataStore } from '$lib/stores/dataStore';
	import { isLoggedIn } from '$lib/services/authService';
	import type { Snippet } from 'svelte';

	let { children }: { children: Snippet } = $props();

	const links = [
		{ key: '/portfolio', icon: 'fa-solid fa-chart-line', label: 'Portfolio', showWhenLoggedIn: true, inFooter: false },
		{ key: '/investments', icon: 'fa-solid fa-coins', label: 'Investments', showWhenLoggedIn: true, inFooter: false },
		{ key: '/wallet', icon: 'fa-solid fa-wallet', label: 'Wallet', showWhenLoggedIn: true, inFooter: false },
		{ key: '/settings', icon: 'fa-solid fa-gear', label: 'Settings', showWhenLoggedIn: true, inFooter: true },
		{ key: '/logout', icon: 'fa-solid fa-sign-out-alt', label: 'Logout', showWhenLoggedIn: true, inFooter: true },
		{ key: '/login', icon: 'fa-solid fa-sign-in-alt', label: 'Login', showWhenLoggedIn: false, inFooter: true }
	];

	// Filter links based on login status
	let visibleLinks = $derived(links.filter(link => link.showWhenLoggedIn === $isLoggedIn));
	let mainLinks = $derived(visibleLinks.filter(link => !link.inFooter));
	let footerLinks = $derived(visibleLinks.filter(link => link.inFooter));

	let showLoading = $state(true);
	let fadeOut = $state(false);
	let mobileMenuOpen = $state(false);

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
			// Start fade-out when loading completes
			fadeOut = true;
		} else if (loading && !showLoading) {
			// Reset loading screen when loading starts again
			showLoading = true;
			fadeOut = false;
		}
	});

	onMount(async () => {
		await dataStore.initialize();
	});

	function navigateTo(path: string) {
		goto(path);
		mobileMenuOpen = false; // Close mobile menu after navigation
	}

	function toggleMobileMenu() {
		mobileMenuOpen = !mobileMenuOpen;
	}

	// Get current path for active state
	let currentPath = $derived($page.url.pathname);
</script>

<!-- Mobile Header with Hamburger Menu -->
<header class="bg-card sticky top-0 z-40 md:hidden">
	<div class="flex items-center justify-between px-4 py-3">
		<div>
			<img class="h-8.5" src="/banner.png" alt="Portfolio Logo" />
		</div>
		<button
			type="button"
			class="hamburger-menu rounded-md p-2 transition-colors"
			onclick={toggleMobileMenu}
			aria-label="Toggle navigation menu"
		>
			<div class="hamburger-icon {mobileMenuOpen ? 'open' : ''}">
				<span></span>
				<span></span>
				<span></span>
			</div>
		</button>
	</div>
</header>

<!-- Mobile Menu Overlay -->
{#if mobileMenuOpen}
	<!-- svelte-ignore a11y_click_events_have_key_events -->
	<!-- svelte-ignore a11y_no_static_element_interactions -->
	<div
		class="bg-background-backdrop fixed inset-0 z-50 md:hidden"
		onclick={toggleMobileMenu}
		onkeydown={(e) => {
			if (e.key === 'Enter' || e.key === ' ') {
				e.preventDefault();
				toggleMobileMenu();
			}
		}}
		role="button"
		tabindex="-1"
		aria-label="Close menu overlay"
	></div>
{/if}

<!-- Mobile Navigation Menu -->
<nav
	class="bg-card fixed right-0 top-0 z-50 h-full w-64 transform transition-transform duration-300 ease-in-out md:hidden {mobileMenuOpen
		? 'translate-x-0'
		: 'translate-x-full'}"
>
	<div class="flex h-full flex-col">
		<div
			class="flex items-center justify-between border-b border-gray-200 px-4 py-3 dark:border-gray-700"
		>
			<h2 class="text-lg font-semibold">Menu</h2>
			<button
				type="button"
				class="rounded-md p-2 transition-colors hover:bg-gray-100 dark:hover:bg-gray-700"
				onclick={toggleMobileMenu}
				aria-label="Close menu"
			>
				<i class="fa-solid fa-times text-xl"></i>
			</button>
		</div>
		<div class="flex-1 px-4 py-6">
			{#each visibleLinks as { key, icon, label }}
				<button
					type="button"
					class="mobile-nav-item mb-2 flex w-full items-center gap-3 rounded-md px-3 py-3 text-left transition-colors hover:bg-gray-100 dark:hover:bg-gray-700 {currentPath ===
					key
						? 'active'
						: ''}"
					onclick={() => navigateTo(key)}
				>
					<i class="{icon} text-xl"></i>
					<span class="text-base">{label}</span>
				</button>
			{/each}
		</div>
	</div>
</nav>

<!-- Desktop and Main Layout -->
<div class="min-h-screen md:grid md:grid-cols-[70px_1fr]">
	<!-- Desktop Sidebar -->
	<div
		class="bg-card fixed bottom-0 left-0 top-0 hidden w-[70px] flex-col items-center justify-between p-1 md:flex"
	>
		<div class="flex h-full flex-col">
			<button class="flex items-center justify-center h-12.5 w-12.5 mt-2 hover:scale-105 transition-transform hover:cursor-pointer" onclick={() => navigateTo($isLoggedIn ? '/portfolio' : '/login')}>
				<img class="w-10" src="/logo.png" alt="Portfolio Logo" />
			</button>

			{#each mainLinks as { key, icon, label }, i}
				<button
					type="button"
					class="nav-item flex h-12.5 w-12.5 flex-col items-center justify-center text-2xl my-1 hover:scale-115 transition-transform hover:cursor-pointer {currentPath === key
						? 'active'
						: ''}"
					aria-label={label}
					title={label}
					onclick={() => navigateTo(key)}
				>
					<i class={icon}></i>
					<span class="display md:hidden">{label}</span>
				</button>
				{#if i < mainLinks.length - 1}
					<hr class="color-muted mx-auto my-1 w-4 border-t" />
				{/if}
			{/each}

			{#each footerLinks as { key, icon, label }, i}
				<button
					type="button"
					class="nav-item flex h-12.5 w-12.5 flex-col items-center justify-center text-2xl {i === 0 ? 'mt-auto' : 'my-1'} hover:scale-115 transition-transform hover:cursor-pointer {currentPath === key
						? 'active'
						: ''}"
					aria-label={label}
					title={label}
					onclick={() => navigateTo(key)}
				>
					<i class={icon}></i>
					<span class="display md:hidden">{label}</span>
				</button>
				{#if i < footerLinks.length - 1}
					<hr class="color-muted mx-auto my-1 w-4 border-t" />
				{/if}
			{/each}
		</div>
	</div>

	<!-- Main Content Area -->
	<div class="relative md:col-start-2">
		{#if showLoading}
			<div
				class="loading-screen bg-background-backdrop color-default fixed left-0 top-0 z-50 flex h-screen w-full flex-col items-center justify-center md:absolute md:h-full"
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
					<button class="btn btn-big btn-error" onclick={() => dataStore.refreshData()}
						>Retry</button
					>
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

	/* Mobile navigation styles */
	.mobile-nav-item.active {
		background-color: var(--color-primary);
		color: white;
	}

	.mobile-nav-item.active:hover {
		background-color: var(--color-primary);
	}

	/* Hamburger menu styles */
	.hamburger-icon {
		position: relative;
		width: 24px;
		height: 18px;
		transform: rotate(0deg);
		transition: 0.3s ease-in-out;
		cursor: pointer;
	}

	.hamburger-icon span {
		display: block;
		position: absolute;
		height: 2px;
		width: 100%;
		background: currentColor;
		border-radius: 2px;
		opacity: 1;
		left: 0;
		transform: rotate(0deg);
		transition: 0.25s ease-in-out;
	}

	.hamburger-icon span:nth-child(1) {
		top: 0px;
	}

	.hamburger-icon span:nth-child(2) {
		top: 8px;
	}

	.hamburger-icon span:nth-child(3) {
		top: 16px;
	}

	.hamburger-icon.open span:nth-child(1) {
		top: 8px;
		transform: rotate(135deg);
	}

	.hamburger-icon.open span:nth-child(2) {
		opacity: 0;
		left: -60px;
	}

	.hamburger-icon.open span:nth-child(3) {
		top: 8px;
		transform: rotate(-135deg);
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
