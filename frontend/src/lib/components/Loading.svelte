<script lang="ts">
	let { text }: { text?: string } = $props();
</script>

<div class="flex flex-col items-center justify-center py-8">
	<div class="flex gap-2.5 {text ? 'mb-4.5' : ''}">
		<div class="dot w-4 h-4 rounded-full"></div>
		<div class="dot w-4 h-4 rounded-full"></div>
		<div class="dot w-4 h-4 rounded-full"></div>
	</div>
	{#if text}
		<p class="fade-in pulse">Loading positions...</p>
	{/if}
</div>

<style>
	.dot {
		background: linear-gradient(135deg, var(--color-primary) 60%, var(--color-secondary) 100%);
		animation: bounce 1.2s infinite ease-in-out;
	}

	.dot:nth-child(1) { animation-delay: 0s; }
	.dot:nth-child(2) { animation-delay: 0.2s; }
	.dot:nth-child(3) { animation-delay: 0.4s; }

	@keyframes bounce {
		0%, 80%, 100% {
			transform: translateY(0);
		}
		40% {
			transform: translateY(-24px);
		}
	}

	.fade-in {
		opacity: 0;
		animation: fadeIn 1s forwards;
	}

	@keyframes fadeIn {
		to {
			opacity: 1;
		}
	}

	.pulse {
		animation: pulse 2s infinite;
	}

	@keyframes pulse {
		0% {
			opacity: 0.4;
		}
		50% {
			opacity: 1;
		}
		100% {
			opacity: 0.4;
		}
	}
</style>
