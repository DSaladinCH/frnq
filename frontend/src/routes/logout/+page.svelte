<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { logout } from '$lib/services/authService';
	import { notify } from '$lib/services/notificationService';
	import Loading from '$lib/components/Loading.svelte';

	onMount(async () => {
		try {
			await logout();
			notify.success('Successfully logged out');
			goto('/login');
		} catch (error) {
			console.error('Logout failed:', error);
			notify.error('Logout failed. Please try again.');
			goto('/login');
		}
	});
</script>

<div class="flex h-screen items-center justify-center">
	<Loading text="Logging out..." />
</div>
