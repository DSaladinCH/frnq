<script lang="ts">
	import '../app.css';
	import { onMount } from 'svelte';
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { dataStore } from '$lib/stores/dataStore';
	import { isLoggedIn } from '$lib/services/authService';
	import NotificationContainer from '$lib/components/NotificationContainer.svelte';
	import type { Snippet } from 'svelte';

	let { children }: { children: Snippet } = $props();

	const links = [
		{
			key: '/portfolio',
			icon: 'fa-solid fa-chart-line',
			label: 'Portfolio',
			showWhenLoggedIn: true,
			inFooter: false
		},
		{
			key: '/investments',
			icon: 'fa-solid fa-coins',
			label: 'Investments',
			showWhenLoggedIn: true,
			inFooter: false
		},
		{
			key: '/wallet',
			icon: 'fa-solid fa-wallet',
			label: 'Wallet',
			showWhenLoggedIn: true,
			inFooter: false
		},
		{
			key: '/settings',
			icon: 'fa-solid fa-gear',
			label: 'Settings',
			showWhenLoggedIn: true,
			inFooter: true
		},
		{
			key: '/logout',
			icon: 'fa-solid fa-sign-out-alt',
			label: 'Logout',
			showWhenLoggedIn: true,
			inFooter: true
		},
		{
			key: '/login',
			icon: 'fa-solid fa-sign-in-alt',
			label: 'Login',
			showWhenLoggedIn: false,
			inFooter: true
		}
	];

	// Filter links based on login status
	let visibleLinks = $derived(links.filter((link) => link.showWhenLoggedIn === $isLoggedIn));
	let mainLinks = $derived(visibleLinks.filter((link) => !link.inFooter));
	let footerLinks = $derived(visibleLinks.filter((link) => link.inFooter));

	let mobileMenuOpen = $state(false);

	// No data initialization in main layout - that's handled by (auth) routes

	function navigateTo(path: string) {
		goto(path);
		mobileMenuOpen = false; // Close mobile menu after navigation
	}

	function toggleMobileMenu() {
		mobileMenuOpen = !mobileMenuOpen;
	}

	let keyActive = (key: string) => (currentPath.startsWith(key) ? 'active' : '');

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
					class="{keyActive(key)} mobile-nav-item mb-2 flex w-full items-center gap-3 rounded-md px-3 py-3 text-left transition-colors hover:bg-gray-100 dark:hover:bg-gray-700"
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
		class="bg-card fixed bottom-0 left-0 top-0 hidden w-17.5 flex-col items-center justify-between p-1 md:flex"
	>
		<div class="flex h-full flex-col">
			<button
				class="h-12.5 w-12.5 mt-2 flex items-center justify-center transition-transform hover:scale-105 hover:cursor-pointer"
				onclick={() => navigateTo($isLoggedIn ? '/portfolio' : '/login')}
			>
				<img class="w-10" src="/logo.png" alt="Portfolio Logo" />
			</button>

			{#each mainLinks as { key, icon, label }, i}
				<button
					type="button"
					class="{keyActive(key)} nav-item h-12.5 w-12.5 hover:scale-115 my-1 flex flex-col items-center justify-center text-2xl transition-transform hover:cursor-pointer"
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
					class="nav-item h-12.5 w-12.5 flex flex-col items-center justify-center text-2xl {i === 0
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
				{#if i < footerLinks.length - 1}
					<hr class="color-muted mx-auto my-1 w-4 border-t" />
				{/if}
			{/each}
		</div>
	</div>

	<!-- Main Content Area -->
	<div class="relative md:col-start-2">
		{@render children?.()}
	</div>
</div>

<!-- Notification Container -->
<NotificationContainer />

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
</style>
