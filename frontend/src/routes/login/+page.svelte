<script lang="ts">
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';
	import { page } from '$app/stores';
	import Button from '$lib/components/Button.svelte';
	import Input from '$lib/components/Input.svelte';
	import PasswordInput from '$lib/components/PasswordInput.svelte';
	import {
		login,
		checkSignupEnabled,
		getOidcProviders,
		initiateOidcLogin,
		type OidcProvider
	} from '$lib/services/authService';
	import { notify } from '$lib/services/notificationService';
	import { ContentWidth } from '$lib/types/ContentSize';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import { StylePadding } from '$lib/types/StylePadding';
	import { TextSize } from '$lib/types/TextSize';

	let email = $state('');
	let password = $state('');
	let isLoading = $state(false);
	let signupEnabled = $state(true);
	let oidcProviders = $state<OidcProvider[]>([]);
	let errors = $state({
		email: '',
		password: ''
	});

	onMount(async () => {
		// Check for error in URL
		const error = $page.url.searchParams.get('error');
		if (error) {
			notify.error(decodeURIComponent(error));
		}

		// Check if signup is enabled
		signupEnabled = await checkSignupEnabled();

		// Load OIDC providers
		oidcProviders = await getOidcProviders();

		// Auto-redirect if only one provider and it has autoRedirect enabled
		const autoRedirectProvider = oidcProviders.find((p) => p.autoRedirect);
		if (autoRedirectProvider && oidcProviders.length === 1) {
			initiateOidcLogin(autoRedirectProvider.providerId);
		}
	});

	function handleOidcLogin(providerId: string) {
		initiateOidcLogin(providerId);
	}

	function validateForm(): boolean {
		errors = { email: '', password: '' };
		let isValid = true;

		// Validate email
		if (!email.trim()) {
			errors.email = 'Email is required';
			isValid = false;
		}

		// Validate password
		if (!password) {
			errors.password = 'Password is required';
			isValid = false;
		}

		return isValid;
	}

	async function loginAsync() {
		if (!validateForm()) {
			notify.error('Please fill in all fields');
			return;
		}

		try {
			isLoading = true;
			await login(email, password);

			notify.success('Successfully logged in!');
			goto('/portfolio');
		} catch (error) {
			console.error('Login failed:', error);
			notify.error('Login failed. Please check your credentials and try again.');
		} finally {
			isLoading = false;
		}
	}

	async function handleKeyPress(event: KeyboardEvent) {
		if (event.key === 'Enter') {
			await loginAsync();
		}
	}
</script>

<div class="login-background flex min-h-screen justify-center p-4 md:items-center">
	<div class="w-full max-w-md">
		<!-- Card Container with enhanced styling -->
		<div
			class="bg-card border-button login-card overflow-hidden rounded-2xl border shadow-2xl backdrop-blur-sm"
		>
			<!-- Header Section with gradient -->
			<div class="login-header border-button border-b p-4 text-center sm:p-8">
				<div class="mb-4">
					<img src="/banner.png" alt="Portfolio Logo" class="mx-auto h-12" />
				</div>
				<h1 class="color-default mb-2 text-3xl font-bold">Welcome Back</h1>
				<p class="color-muted text-sm">Sign in to access your portfolio</p>
			</div>

			<!-- Form Section -->
			<div class="p-4 sm:p-8">
				<div class="grid gap-5">
					<div>
						<Input
							bind:value={email}
							type="text"
							autocomplete="email"
							title="Email"
							placeholder="Enter your email"
						/>
						{#if errors.email}
							<p class="color-error mt-1 text-xs">{errors.email}</p>
						{/if}
					</div>

					<div>
						<PasswordInput
							bind:value={password}
							onkeypress={handleKeyPress}
							autocomplete="current-password"
							title="Password"
							placeholder="Enter your password"
						/>
						{#if errors.password}
							<p class="color-error mt-1 text-xs">{errors.password}</p>
						{/if}
					</div>

					<!-- Forgot Password Link -->
					<div class="text-right">
						<button
							type="button"
							class="link-button text-sm font-bold"
							onclick={() => notify.info('Password reset functionality coming soon!')}
						>
							Forgot password?
						</button>
					</div>

					<div>
						<Button
							{isLoading}
							onclick={loginAsync}
							text="Sign In"
							icon="fa-solid fa-sign-in-alt"
							width={ContentWidth.Full}
						/>
					</div>

					<!-- OIDC Providers -->
					{#if oidcProviders.length > 0}
						<div class="relative">
							<div class="absolute inset-0 flex items-center">
								<div class="border-button w-full border-t"></div>
							</div>
							<div class="relative flex justify-center text-xs uppercase">
								<span class="bg-card color-muted px-2">Or continue with</span>
							</div>
						</div>

						<div class="grid gap-3">
							{#each oidcProviders as provider}
								<!-- TODO: Implement with Button component -->
								<Button
									style={ColorStyle.Card}
									padding={StylePadding.Reduced}
									width={ContentWidth.Full}
									textSize={TextSize.Medium}
									onclick={() => handleOidcLogin(provider.providerId)}
								>
									{#if provider.faviconUrl}
										<img
											src={provider.faviconUrl}
											alt={provider.displayName}
											class="h-5 w-5 object-contain"
										/>
									{:else}
										<i class="fas fa-building"></i>
									{/if}

									<span>Continue with {provider.displayName}</span>
								</Button>
							{/each}
						</div>
					{/if}
				</div>
			</div>

			<!-- Footer Section -->
			{#if signupEnabled}
				<div class="border-button border-t p-4 text-center">
					<p class="color-muted text-sm">
						Don't have an account?
						<a href="/signup" class="link-button ml-1 font-bold"> Sign up </a>
					</p>
				</div>
			{/if}
		</div>

		<!-- Additional Info -->
		<div class="mt-6 text-center">
			<p class="color-muted text-xs opacity-70">
				By signing in, you agree to our Terms of Service and Privacy Policy
			</p>
		</div>
	</div>
</div>

<style>
	.login-background {
		background: linear-gradient(
			135deg,
			var(--color-background) 0%,
			var(--color-background) 50%,
			var(--color-card) 100%
		);
	}

	.login-header {
		background: linear-gradient(
			to right,
			color-mix(in srgb, var(--color-primary) 20%, transparent),
			color-mix(in srgb, var(--color-accent) 20%, transparent)
		);
	}

	.login-card {
		animation: fadeInUp 0.6s ease-out;
	}

	@keyframes fadeInUp {
		from {
			opacity: 0;
			transform: translateY(20px);
		}
		to {
			opacity: 1;
			transform: translateY(0);
		}
	}

	/* Add subtle hover effect to the card */
	.login-card:hover {
		box-shadow: 0 25px 50px -12px color-mix(in srgb, var(--color-primary) 15%, transparent);
		transition: box-shadow 0.3s ease;
	}
</style>
