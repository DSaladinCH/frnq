<script lang="ts">
	let {
		icon,
		text,
		onclick,
		visible = true,
		danger = false,
		isLoading = false
	}: {
		icon: string;
		text: string;
		onclick: () => void;
		visible?: boolean;
		danger?: boolean;
		isLoading?: boolean;
	} = $props();
</script>

{#if visible}
	<button
		type="button"
		class="flex w-full items-center gap-3 rounded-md border-none bg-transparent px-3 py-2.5 text-left text-sm transition-all duration-150 ease-in-out hover:cursor-pointer focus:outline-none"
		class:text-error={danger}
		class:text-default={!danger}
		class:hover:color-primary={!danger}
		{onclick}
	>
		<div class:hidden={isLoading}>
			<i
				class="{icon} w-5 text-center transition-colors duration-150"
				class:color-error={danger}
				class:color-muted={!danger}
			></i>
		</div>

		{#if isLoading}
			<div class="w-5">
				<svg
					class="fa-spin col-1 row-1 mx-auto my-auto h-5 w-5 text-white"
					xmlns="http://www.w3.org/2000/svg"
					fill="none"
					viewBox="0 0 24 24"
				>
					<circle class="opacity-50" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"
					></circle>
					<path class="opacity-100" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"
					></path>
				</svg>
			</div>
		{/if}
		<span>{text}</span>
	</button>
{/if}

<style>
	button:hover i {
		color: inherit;
	}

	button:hover:not(:disabled) {
		background: hsl(from var(--color-primary) h s l / 0.3);
	}
</style>
