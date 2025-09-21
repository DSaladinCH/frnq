<script lang="ts">
	import { ColorStyle } from '$lib/types/ColorStyle';
	import { ContentWidth } from '$lib/types/ContentSize';
	import { StylePadding } from '$lib/types/StylePadding';
	import { TextSize } from '$lib/types/TextSize';

	let {
		onclick,
		text = '',
		icon = 'fa-solid fa-arrow-right',
		textSize = TextSize.Large,
		style = ColorStyle.Primary,
		width = ContentWidth.Max,
		padding = StylePadding.Default,
		disabled = false,
		isLoading = false
	}: {
		onclick: () => void;
		text?: string;
		icon?: string;
		textSize?: TextSize;
		style?: ColorStyle;
		width?: ContentWidth;
		padding?: StylePadding;
		disabled?: boolean;
		isLoading?: boolean;
	} = $props();

	let styleClass = {
		[ColorStyle.Primary]: 'button-primary',
		[ColorStyle.Secondary]: 'button-secondary',
		[ColorStyle.Accent]: 'button-accent',
		[ColorStyle.Success]: 'button-success',
		[ColorStyle.Error]: 'button-error'
	};

	let widthClass = {
		[ContentWidth.Max]: 'w-max',
		[ContentWidth.Full]: 'w-full'
	};

	let paddingClass = {
		[StylePadding.Default]: 'px-4 py-2',
		[StylePadding.Reduced]: 'px-2 py-1'
	};
</script>

<button
	{onclick}
	disabled={disabled || isLoading}
	class="button color-default {styleClass[
		style
	]} inline-block cursor-pointer rounded-lg border-0 {paddingClass[padding]} font-bold decoration-0 {widthClass[width]} h-full"
>
	<div class="grid items-center justify-center {textSize}">
		{#if isLoading}
			<svg
				class="fa-spin h-5 w-5 text-white col-1 row-1 mx-auto"
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

		<div class="flex items-center leading-none gap-2 col-1 row-1 {isLoading ? 'invisible' : ''}">
			{#if icon}
				<i class={icon}></i>
			{/if}

			{#if text}
				<span>{text}</span>
			{/if}
		</div>
	</div>
</button>

<style>
	.button-primary {
		--btn-bg: var(--color-primary);
	}

	.button-secondary {
		--btn-bg: var(--color-secondary);
	}

	.button-accent {
		--btn-bg: var(--color-accent);
	}

	.button-success {
		--btn-bg: var(--color-success);
	}

	.button-error {
		--btn-bg: var(--color-error);
	}

	.button {
		--white-ratio: 15%;
		background-image: linear-gradient(
			to right,
			var(--btn-bg) 0%,
			color-mix(in srgb, var(--btn-bg), white var(--white-ratio)) 51%,
			var(--btn-bg) 100%
		);
		background-size: 200% auto;
		transition: background-position 0.5s;
	}

	.button:hover {
		background-position: right center;
	}
</style>
