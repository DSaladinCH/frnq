<script lang="ts">
	import Button from '$lib/components/Button.svelte';
	import { login } from '$lib/services/authService';

	let email = $state('');
	let password = $state('');

    async function loginAsync() {
        try {
            await login(email, password);
            // Redirect or show success message
            window.location.href = '/portfolio'; // Example redirect after login
        } catch (error) {
            console.error('Login failed:', error);
            // Show error message to user
        }
    }
</script>

<div>
	<h1 class="text-3xl font-bold mb-8">Login</h1>
	<label>
		Email:
		<input type="text" bind:value={email} class="textbox" />
	</label>
	<br />
	<label>
		Password:
		<input type="password" bind:value={password} class="textbox" />
	</label>
	<br />
	<Button onclick={loginAsync} text="Login" />
</div>
