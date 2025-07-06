<script lang="ts">
	let {
		showModal = $bindable(),
		children,
		popModal
	}: { showModal: Boolean; children: any; popModal: () => void } = $props();

	let dialog: any = $state();

	$effect(() => {
		if (showModal) dialog.showModal();
		else dialog.close();
	});
</script>

<!-- svelte-ignore a11y_click_events_have_key_events, a11y_no_noninteractive_element_interactions -->
<dialog
	class="flex bg-background color-default w-full lg:w-3/4 2xl:w-1/2"
	bind:this={dialog}
	onclose={() => popModal()}
	onclick={(e) => {
		if (e.target === dialog) popModal();
	}}
>
	<div class="relative flex flex-1 flex-col overflow-hidden">
		{@render children?.()}

		<div class="absolute right-0 top-0">
			<button class="btn btn-primary" onclick={() => popModal()}>Close</button>
		</div>
	</div>
</dialog>

<style>
	dialog {
		border-radius: 1rem;
		border: none;
		padding: 1.5rem;
		margin: auto auto;
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
