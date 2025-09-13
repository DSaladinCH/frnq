<script lang="ts">
	import { onMount, setContext } from 'svelte';

	let {
		showModal = $bindable(),
		children,
		onClose
	}: { showModal: Boolean; children: any; onClose: () => void } = $props();

	let dialog: any = $state();

	$effect(() => {
		if (showModal) dialog.showModal();
		else dialog.close();
	});

	onMount(() => {
		dialog.addEventListener('click', (e: MouseEvent) => {
			const rect = dialog.getBoundingClientRect();
			const clickedOutside =
				e.clientX < rect.left ||
				e.clientX > rect.right ||
				e.clientY < rect.top ||
				e.clientY > rect.bottom;

			if (clickedOutside) {
				dialog.close();
			}
		});
	});
</script>

<!-- svelte-ignore a11y_click_events_have_key_events, a11y_no_noninteractive_element_interactions -->
<dialog
	class="{showModal ? 'flex' : 'hidden'} bg-background color-default w-full max-xs:max-w-full lg:w-3/4 2xl:w-1/2 p-6 my-auto mx-0 xs:m-auto"
	bind:this={dialog}
	onclose={onClose}
>
	<div class="relative flex flex-1 flex-col overflow-hidden">
		{@render children?.()}

		<div class="absolute right-0 top-0">
			<button class="btn btn-primary" onclick={onClose}>Close</button>
		</div>
	</div>
</dialog>

<style>
	dialog {
		border-radius: 1rem;
		border: none;
		min-height: 200px;
		max-height: 80vh;
	}

	dialog::backdrop {
		background-color: var(--color-background-backdrop);
	}

	dialog[open] {
		animation: zoom 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
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
