<script lang="ts">
	import { goto } from '$app/navigation';
	import Button from '$lib/components/Button.svelte';
	import Input from '$lib/components/Input.svelte';
	import PasswordInput from '$lib/components/PasswordInput.svelte';
	import { login } from '$lib/services/authService';
	import { ContentWidth } from '$lib/types/ContentSize';

	let email = $state('');
	let password = $state('');
	let isLoading = $state(false);

	async function loginAsync() {
		try {
			isLoading = true;
			await login(email, password);

			goto('/portfolio');
		} catch (error) {
			console.error('Login failed:', error);
			// Show error message to user
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

<div class="mx-auto grid md:h-[stretch] max-w-sm items-center px-4 max-md:pt-12">
	<div>
		<div class="mb-8 grid justify-center">
			<h1 class="text-center text-3xl font-bold">Login</h1>
		</div>
		<div class="grid gap-4">
			<Input bind:value={email} type="text" autocomplete="email" title="Email" placeholder="Enter your email" />
			<PasswordInput bind:value={password} onkeypress={handleKeyPress} autocomplete="current-password" title="Password" placeholder="Enter your password" />
			<div class="mt-4">
				<Button {isLoading} onclick={loginAsync} text="Login" icon="fa-solid fa-sign-in-alt" width={ContentWidth.Full} />
			</div>
		</div>
	</div>
</div>
