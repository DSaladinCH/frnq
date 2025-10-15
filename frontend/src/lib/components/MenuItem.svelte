<script lang="ts">
	let {
		icon,
		text,
		onclick,
		visible = true,
		danger = false
	}: {
		icon: string;
		text: string;
		onclick: () => void;
		visible?: boolean;
		danger?: boolean;
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
		<i
			class="{icon} w-5 text-center transition-colors duration-150"
			class:color-error={danger}
			class:color-muted={!danger}
		></i>
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
