<script lang="ts">
	import { onMount } from 'svelte';
	import Button from './Button.svelte';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import { StylePadding } from '$lib/types/StylePadding';

	let {
		showModal = $bindable(),
		children,
		onClose,
		title = ''
	}: { showModal: Boolean; children: any; onClose: () => void; title?: string } = $props();

	let dialog: any = $state();

	$effect(() => {
		if (showModal) dialog.showModal();
		else dialog.close();
	});

	let mouseDownOnBackdrop = false;
	onMount(() => {
		dialog.addEventListener('mousedown', (e: MouseEvent) => {
			// Check if the click is directly on the dialog element (backdrop)
			mouseDownOnBackdrop = (e.target as HTMLElement).id === 'dialog-content';
			console.log(e.target);
		});

		dialog.addEventListener('mouseup', (e: MouseEvent) => {
			console.log(e.target);
			// Only close if both mousedown and mouseup happened on the backdrop
			if (mouseDownOnBackdrop && (e.target as HTMLElement).id === 'dialog-content') {
				dialog.close();
			}
			mouseDownOnBackdrop = false;
		});
	});
</script>

<dialog bind:this={dialog} class="{showModal ? '' : 'hidden'} bg-transparent" onclose={onClose}>
	<div id="dialog-content" class="fixed inset-0 m-4 flex items-center justify-center p-4">
		<div
			class="bg-background color-default relative flex h-max max-h-full w-full xs:w-max max-w-full flex-col overflow-hidden rounded-2xl md:p-0 lg:p-0"
		>
			<!-- Fixed Header with Title and Close Button -->
			<div class="flex gap-4 items-center justify-between border-b border-button p-5">
				{#if title}
					<h2 class="text-2xl font-semibold">{title}</h2>
				{:else}
					<div></div>
				{/if}
				<Button text="" icon="fa fa-xmark" style={ColorStyle.Secondary} padding={StylePadding.Reduced} onclick={onClose} />
			</div>

			<!-- Scrollable Content Area -->
			<div class="max-h-full flex-1 overflow-y-auto p-4 xs:p-7">
				<div class="max-h-full justify-center">
					{@render children?.()}
				</div>
			</div>
		</div>
	</div>
</dialog>

<style>
	dialog {
		border-radius: 1rem;
		border: none;
		min-height: 200px;
	}

	/* Force the native dialog to act like an absolute fullscreen layer and center its contents */
	dialog[open] {
		position: absolute;
		left: 0;
		top: 0;
		width: 100vw;
		height: 100vh;
		max-width: 100%;
		max-height: 100%;
		display: flex;
		align-items: center;
		justify-content: center;
		padding: 1.5rem; /* matches p-6 approx */
		background: transparent; /* keep backdrop separate */
	}

	/* Remove default UA margin/positioning that can place dialog top-left */
	dialog {
		margin: 0;
	}

	dialog[open] {
		z-index: 9999;
	}

	dialog::backdrop {
		background-color: var(--color-background-backdrop);
	}

	dialog[open] {
		animation: zoom 0.3s cubic-bezier(0.34, 1, 0.64, 1);
	}

	@keyframes zoom {
		from {
			transform: scale(0.95);
		}
		to {
			transform: scale(1);
		}
	}

	dialog[open]::backdrop {
		animation: fade 0.2s ease-out;
	}

	@keyframes fade {
		from {
			opacity: 0;
		}
		to {
			opacity: 1;
		}
	}
</style>
