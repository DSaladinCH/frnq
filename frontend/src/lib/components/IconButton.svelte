<script lang="ts">
	import type { ColorStyle } from '$lib/types/ColorStyle';

	let {
		icon,
		tooltip,
		hoverColor,
		onclick
	}: { icon: string; tooltip: string; hoverColor: ColorStyle; onclick: () => void } = $props();
</script>

<button
	class="icon-button cursor-pointer w-full h-full"
	{onclick}
	title={tooltip}
	aria-label={tooltip}
	style="--hover-color: var(--color-{hoverColor});"
>
	<i class={icon}></i>
</button>

<style>
	.icon-button:hover {
		color: var(--hover-color);
	}
</style>
