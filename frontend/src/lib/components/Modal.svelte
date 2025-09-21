<script lang="ts">
	import { onMount } from 'svelte';
	import Button from './Button.svelte';
	import { ColorStyle } from '$lib/types/ColorStyle';

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

	let mouseDownOutside = false;
	onMount(() => {
		dialog.addEventListener('mousedown', (e: MouseEvent) => {
			const rect = dialog.getBoundingClientRect();
			mouseDownOutside =
				e.clientX < rect.left ||
				e.clientX > rect.right ||
				e.clientY < rect.top ||
				e.clientY > rect.bottom;
		});

		dialog.addEventListener('mouseup', (e: MouseEvent) => {
			const rect = dialog.getBoundingClientRect();
			const mouseUpOutside =
				e.clientX < rect.left ||
				e.clientX > rect.right ||
				e.clientY < rect.top ||
				e.clientY > rect.bottom;
			if (mouseDownOutside && mouseUpOutside) {
				dialog.close();
			}
			mouseDownOutside = false;
		});
	});
</script>

<!-- svelte-ignore a11y_click_events_have_key_events, a11y_no_noninteractive_element_interactions -->
<!-- <dialog
	class="{showModal ? '' : 'hidden'} overflow-hidden bg-transparent"
	bind:this={dialog}
	onclose={onClose}
>
	<div class="modal-overlay" aria-hidden="true">
		<div
			class="bg-background color-default max-h-[calc(100vh-3.5rem)] w-auto max-w-[min(96vw,900px)] overflow-hidden rounded-3xl shadow-lg"
		>
			<div class="max-h-[calc(100vh-3.5rem)] overflow-y-auto p-4">
				<div class="relative flex flex-col">
					{@render children?.()}

					<div class="absolute right-0 top-0">
						<button class="btn btn-primary" onclick={onClose}>Close</button>
					</div>
				</div>
			</div>
		</div>
	</div>
</dialog> -->

<dialog bind:this={dialog} class="{showModal ? '' : 'hidden'} bg-transparent" onclose={onClose}>
	<div id="dialog-content" class="fixed inset-0 m-4 flex items-center justify-center p-4">
		<div
			class="bg-background color-default relative flex h-max max-h-full w-max max-w-full flex-col overflow-hidden rounded-2xl md:p-0 lg:p-0"
		>
			<!-- Fixed Header with Title and Close Button -->
			<div class="flex gap-4 items-center justify-between border-b border-button p-5">
				{#if title}
					<h2 class="text-2xl font-semibold">{title}</h2>
				{:else}
					<div></div>
				{/if}
				<Button text="" icon="fa fa-xmark" style={ColorStyle.Secondary} onclick={onClose} />
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
