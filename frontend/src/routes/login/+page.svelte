<script lang="ts">
	import { goto } from '$app/navigation';
	import Button from '$lib/components/Button.svelte';
	import Input from '$lib/components/Input.svelte';
	import PasswordInput from '$lib/components/PasswordInput.svelte';
	import { login } from '$lib/services/authService';
	import { notify } from '$lib/services/notificationService';
	import { ContentWidth } from '$lib/types/ContentSize';

	let email = $state('');
	let password = $state('');
	let isLoading = $state(false);

	async function loginAsync() {
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

<div class="min-h-screen flex items-center justify-center p-4 login-background">
	<div class="w-full max-w-md">
		<!-- Card Container with enhanced styling -->
		<div class="bg-card backdrop-blur-sm rounded-2xl shadow-2xl border border-button overflow-hidden login-card">
			<!-- Header Section with gradient -->
			<div class="login-header p-4 sm:p-8 text-center border-b border-button">
				<div class="mb-4">
					<img src="/banner.png" alt="Portfolio Logo" class="h-12 mx-auto" />
				</div>
				<h1 class="text-3xl font-bold color-default mb-2">Welcome Back</h1>
				<p class="color-muted text-sm">Sign in to access your portfolio</p>
			</div>

			<!-- Form Section -->
			<div class="p-4 sm:p-8">
				<div class="grid gap-5">
					<Input 
						bind:value={email} 
						type="text" 
						autocomplete="email" 
						title="Email" 
						placeholder="Enter your email" 
					/>
					<PasswordInput 
						bind:value={password} 
						onkeypress={handleKeyPress} 
						autocomplete="current-password" 
						title="Password" 
						placeholder="Enter your password" 
					/>
					
					<!-- Forgot Password Link -->
					<div class="text-right">
						<button 
							type="button"
							class="text-sm link-button font-bold"
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
				</div>
			</div>

			<!-- Footer Section -->
			<div class="p-4 text-center border-t border-button">
				<p class="text-sm color-muted">
					Don't have an account?
					<button 
						class="link-button font-bold ml-1"
						onclick={() => notify.info('Sign up functionality coming soon!')}
					>
						Sign up
					</button>
				</p>
			</div>
		</div>

		<!-- Additional Info -->
		<div class="mt-6 text-center">
			<p class="text-xs color-muted opacity-70">
				By signing in, you agree to our Terms of Service and Privacy Policy
			</p>
		</div>
	</div>
</div>

<style>
	.login-background {
		background: linear-gradient(135deg, var(--color-background) 0%, var(--color-background) 50%, var(--color-card) 100%);
	}

	.login-header {
		background: linear-gradient(to right, 
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

	/* Hover effect for links */
	.hover-link:hover {
		color: var(--color-accent);
	}
</style>
