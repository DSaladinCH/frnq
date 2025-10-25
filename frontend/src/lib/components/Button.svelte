<script lang="ts">
	import { ColorStyle } from '$lib/types/ColorStyle';
	import { ContentWidth } from '$lib/types/ContentSize';
	import { StylePadding } from '$lib/types/StylePadding';
	import { TextSize } from '$lib/types/TextSize';
	import type { Snippet } from 'svelte';

	let {
		onclick,
		text = '',
		icon = 'fa-solid fa-arrow-right',
		children,
		textSize = TextSize.Large,
		style = ColorStyle.Primary,
		width = ContentWidth.Max,
		padding = StylePadding.Default,
		disabled = false,
		isLoading = false
	}: {
		onclick: (event: MouseEvent) => void;
		text?: string;
		icon?: string;
		children?: Snippet;
		textSize?: TextSize;
		style?: ColorStyle;
		width?: ContentWidth;
		padding?: StylePadding;
		disabled?: boolean;
		isLoading?: boolean;
	} = $props();

	let widthClass = {
		[ContentWidth.Max]: 'w-max',
		[ContentWidth.Full]: 'w-full'
	};

	let paddingClass = {
		[StylePadding.Default]: 'px-6 py-4',
		[StylePadding.Reduced]: 'px-6 py-3',
		[StylePadding.None]: 'px-2 py-2'
	};

	let customStyles = {
		[ColorStyle.Primary]: '',
		[ColorStyle.Secondary]: '',
		[ColorStyle.Accent]: '',
		[ColorStyle.Success]: '',
		[ColorStyle.Error]: '',
		[ColorStyle.Control]: '',
		[ColorStyle.Card]: 'border-1 border-button'
	};
</script>

<button
	{onclick}
	disabled={disabled || isLoading}
	class="button color-default button-{style} {customStyles[
		style
	]} inline-block rounded-lg border-0 {paddingClass[padding]} font-bold decoration-0 {widthClass[
		width
	]} h-full"
>
	<div class="grid items-center justify-center {textSize}">
		{#if isLoading}
			<svg
				class="fa-spin col-1 row-1 mx-auto h-5 w-5 text-white"
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

		<div class="col-1 row-1 flex items-center gap-2 leading-none {isLoading ? 'invisible' : ''}">
			{#if children}
				{@render children()}
			{:else}
				{#if icon}
					<i class={icon}></i>
				{/if}

				{#if text}
					<span>{text}</span>
				{/if}
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

	.button-button {
		--btn-bg: var(--color-button);
	}

	.button-card {
		--btn-bg: var(--color-card);
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

	.button.button-card {
		--white-ratio: 10%;
	}

	.button:hover:not(:disabled) {
		background-position: right center;
		cursor: pointer;
	}

	.button:disabled {
		color: var(--color-muted);
	}
</style>
