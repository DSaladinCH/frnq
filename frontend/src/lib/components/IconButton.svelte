<script lang="ts">
	import type { ColorStyle } from '$lib/types/ColorStyle';

	let {
		icon,
		tooltip,
		hoverColor,
		isLoading = false,
		onclick
	}: {
		icon: string;
		tooltip: string;
		hoverColor: ColorStyle;
		isLoading?: boolean;
		onclick: () => void;
	} = $props();
</script>

<button
	class="icon-button h-full w-full cursor-pointer"
	{onclick}
	title={tooltip}
	aria-label={tooltip}
	style="--hover-color: var(--color-{hoverColor});"
>
	<div class="grid items-center justify-center">
		<div class:hidden={isLoading}>
			<i class={icon}></i>
		</div>

		{#if isLoading}
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
		{/if}
	</div>
</button>

<style>
	.icon-button:hover {
		color: var(--hover-color);
	}
</style>
