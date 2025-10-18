<script lang="ts">
	import { goto } from '$app/navigation';
	import Button from '$lib/components/Button.svelte';
	import Input from '$lib/components/Input.svelte';
	import PasswordInput from '$lib/components/PasswordInput.svelte';
	import { signup } from '$lib/services/authService';
	import { notify } from '$lib/services/notificationService';
	import { ContentWidth } from '$lib/types/ContentSize';

	let email = $state('');
	let password = $state('');
	let firstname = $state('');
	let isLoading = $state(false);
	let errors = $state({
		firstname: '',
		email: '',
		password: ''
	});

	function validatePassword(pwd: string): { valid: boolean; message: string } {
		if (pwd.length < 12) {
			return { valid: false, message: 'Password must be at least 12 characters long' };
		}
		if (!/[a-z]/.test(pwd)) {
			return { valid: false, message: 'Password must contain at least one lowercase letter' };
		}
		if (!/[A-Z]/.test(pwd)) {
			return { valid: false, message: 'Password must contain at least one uppercase letter' };
		}
		if (!/[0-9]/.test(pwd)) {
			return { valid: false, message: 'Password must contain at least one digit' };
		}
		if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(pwd)) {
			return { valid: false, message: 'Password must contain at least one special character' };
		}
		return { valid: true, message: '' };
	}

	function validateEmail(email: string): boolean {
		const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
		return emailRegex.test(email);
	}

	function validateForm(): boolean {
		errors = { firstname: '', email: '', password: '' };
		let isValid = true;

		// Validate firstname
		if (!firstname.trim()) {
			errors.firstname = 'First name is required';
			isValid = false;
		}

		// Validate email
		if (!email.trim()) {
			errors.email = 'Email is required';
			isValid = false;
		} else if (!validateEmail(email)) {
			errors.email = 'Please enter a valid email address';
			isValid = false;
		}

		// Validate password
		if (!password) {
			errors.password = 'Password is required';
			isValid = false;
		} else {
			const passwordValidation = validatePassword(password);
			if (!passwordValidation.valid) {
				errors.password = passwordValidation.message;
				isValid = false;
			}
		}

		return isValid;
	}

	async function signupAsync() {
		if (!validateForm()) {
			notify.error('Please fix the errors in the form');
			return;
		}

		try {
			isLoading = true;
			await signup(email, password, firstname);

			notify.success('Successfully signed up! Welcome aboard!');
			goto('/portfolio');
		} catch (error) {
			console.error('Signup failed:', error);
			notify.error('Signup failed. Please check your information and try again.');
		} finally {
			isLoading = false;
		}
	}

	async function handleKeyPress(event: KeyboardEvent) {
		if (event.key === 'Enter') {
			await signupAsync();
		}
	}
</script>

<div class="signup-background flex min-h-screen items-center justify-center p-4">
	<div class="w-full max-w-md">
		<!-- Card Container with enhanced styling -->
		<div
			class="bg-card border-button signup-card overflow-hidden rounded-2xl border shadow-2xl backdrop-blur-sm"
		>
			<!-- Header Section with gradient -->
			<div class="signup-header border-button border-b p-4 text-center sm:p-8">
				<div class="mb-4">
					<img src="/banner.png" alt="Portfolio Logo" class="mx-auto h-12" />
				</div>
				<h1 class="color-default mb-2 text-3xl font-bold">Create Account</h1>
				<p class="color-muted text-sm">Join us to start managing your portfolio</p>
			</div>

			<!-- Form Section -->
			<div class="p-4 sm:p-8">
				<div class="grid gap-5">
					<div class="grid gap-1">
						<Input
							bind:value={firstname}
							type="text"
							autocomplete="given-name"
							title="First Name"
							placeholder="Enter your first name"
						/>
						{#if errors.firstname}
							<p class="color-error text-xs">{errors.firstname}</p>
						{/if}
					</div>

					<div class="grid gap-1">
						<Input
							bind:value={email}
							type="email"
							autocomplete="email"
							title="Email"
							placeholder="Enter your email"
						/>
						{#if errors.email}
							<p class="color-error text-xs">{errors.email}</p>
						{/if}
					</div>

					<div class="grid gap-1">
						<PasswordInput
							bind:value={password}
							onkeypress={handleKeyPress}
							autocomplete="new-password"
							title="Password"
							placeholder="Create a password"
						/>
						{#if errors.password}
							<p class="color-error text-xs">{errors.password}</p>
						{/if}
						<p class="color-muted text-xs opacity-70">
							Must be at least 12 characters with uppercase, lowercase, number, and special
							character
						</p>
					</div>

					<div>
						<Button
							{isLoading}
							onclick={signupAsync}
							text="Create Account"
							icon="fa-solid fa-user-plus"
							width={ContentWidth.Full}
						/>
					</div>
				</div>
			</div>

			<!-- Footer Section -->
			<div class="border-button border-t p-4 text-center">
				<p class="color-muted text-sm">
					Already have an account?
					<a href="/login" class="link-button ml-1 font-bold"> Sign in </a>
				</p>
			</div>
		</div>

		<!-- Additional Info -->
		<div class="mt-6 text-center">
			<p class="color-muted text-xs opacity-70">
				By signing up, you agree to our Terms of Service and Privacy Policy
			</p>
		</div>
	</div>
</div>

<style>
	.signup-background {
		background: linear-gradient(
			135deg,
			var(--color-background) 0%,
			var(--color-background) 50%,
			var(--color-card) 100%
		);
	}

	.signup-header {
		background: linear-gradient(
			to right,
			color-mix(in srgb, var(--color-primary) 20%, transparent),
			color-mix(in srgb, var(--color-accent) 20%, transparent)
		);
	}

	.signup-card {
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
	.signup-card:hover {
		box-shadow: 0 25px 50px -12px color-mix(in srgb, var(--color-primary) 15%, transparent);
		transition: box-shadow 0.3s ease;
	}
</style>
