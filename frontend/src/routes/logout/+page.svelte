<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { logout } from '$lib/services/authService';
	import { notify } from '$lib/services/notificationService';

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
	<div class="text-center">
		<div class="bouncing-dots">
			<div class="dot"></div>
			<div class="dot"></div>
			<div class="dot"></div>
		</div>
		<p class="text-muted-foreground text-lg">Logging out...</p>
	</div>
</div>

<style>
	.bouncing-dots {
		display: flex;
		gap: 10px;
		margin-bottom: 18px;
		justify-content: center;
	}

	.dot {
		width: 16px;
		height: 16px;
		background: linear-gradient(135deg, var(--color-primary) 60%, var(--color-secondary) 100%);
		border-radius: 50%;
		animation: bounce 1.2s infinite ease-in-out;
	}

	.dot:nth-child(1) {
		animation-delay: 0s;
	}
	.dot:nth-child(2) {
		animation-delay: 0.2s;
	}
	.dot:nth-child(3) {
		animation-delay: 0.4s;
	}

	@keyframes bounce {
		0%,
		80%,
		100% {
			transform: scale(0);
		}
		40% {
			transform: scale(1);
		}
	}
</style>
