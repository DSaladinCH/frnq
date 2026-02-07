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
		background: 
			linear-gradient(135deg, 
				hsl(from var(--btn-bg) h s calc(l * 1.15)) 0%,
				var(--btn-bg) 50%,
				hsl(from var(--btn-bg) h s calc(l * 0.85)) 100%
			);
		position: relative;
		overflow: hidden;
		transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
		transform: translate3d(0, 0, 0) scale(1);
		transform-origin: center center;
		border: none;
		box-shadow: 
			0 4px 20px hsl(from var(--color-background) h s calc(l * 0.5) / 0.6),
			0 0 40px hsl(from var(--btn-bg) h s l / 0.05),
			inset 0 1px 0 hsl(from var(--btn-bg) h s calc(l * 1.5) / 0.15);
		will-change: transform, box-shadow;
		backface-visibility: hidden;
		-webkit-font-smoothing: subpixel-antialiased;
	}

	.button::before {
		content: '';
		position: absolute;
		inset: 0;
		border-radius: 0.5rem;
		padding: 2px;
		background: linear-gradient(
			135deg, 
			hsl(from var(--btn-bg) h s l / 0.3),
			hsl(from var(--btn-bg) h s l / 0.2),
			hsl(from var(--btn-bg) h s l / 0.3)
		);
		-webkit-mask: 
			linear-gradient(#fff 0 0) content-box, 
			linear-gradient(#fff 0 0);
		-webkit-mask-composite: xor;
		mask-composite: exclude;
		opacity: 0.6;
		transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
		will-change: opacity;
	}

	.button::after {
		content: '';
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		height: 40%;
		background: linear-gradient(
			180deg,
			hsl(from var(--btn-bg) h s l / 0.08) 0%,
			transparent 100%
		);
		border-radius: 0.5rem 0.5rem 0 0;
		pointer-events: none;
	}

	.button.button-card {
		background: 
			linear-gradient(135deg, 
				hsl(from var(--btn-bg) h s calc(l * 1.15)) 0%,
				var(--btn-bg) 50%,
				hsl(from var(--btn-bg) h s calc(l * 0.85)) 100%
			);
		box-shadow: 
			0 4px 20px hsl(from var(--color-background) h s calc(l * 0.5) / 0.5),
			0 0 30px hsl(from var(--btn-bg) h s l / 0.03),
			inset 0 1px 0 hsl(from var(--btn-bg) h s calc(l * 1.5) / 0.1);
	}

	.button-secondary {
		box-shadow:
			0 4px 20px hsl(from var(--color-background) h s calc(l * 0.5) / 0.6),
			0 0 40px hsl(from var(--color-secondary) h s l / 0.1),
			0 0 60px hsl(from var(--color-accent) h s l / 0.05),
			inset 0 1px 0 hsl(from var(--color-secondary) h s calc(l * 1.5) / 0.15);
	}

	.button-primary {
		box-shadow:
			0 4px 20px hsl(from var(--color-background) h s calc(l * 0.5) / 0.6),
			0 0 40px hsl(from var(--color-primary) h s l / 0.1),
			inset 0 1px 0 hsl(from var(--color-primary) h s calc(l * 1.5) / 0.15);
	}

	.button-success {
		box-shadow:
			0 4px 20px hsl(from var(--color-background) h s calc(l * 0.5) / 0.6),
			0 0 40px hsl(from var(--color-success) h s l / 0.1),
			inset 0 1px 0 hsl(from var(--color-success) h s calc(l * 1.5) / 0.15);
	}

	.button-error {
		box-shadow:
			0 4px 20px hsl(from var(--color-background) h s calc(l * 0.5) / 0.6),
			0 0 40px hsl(from var(--color-error) h s l / 0.1),
			inset 0 1px 0 hsl(from var(--color-error) h s calc(l * 1.5) / 0.15);
	}

	.button:hover:not(:disabled) {
		background: 
			linear-gradient(135deg, 
				hsl(from var(--btn-bg) h s calc(l * 1.25)) 0%,
				hsl(from var(--btn-bg) h s calc(l * 1.1)) 50%,
				var(--btn-bg) 100%
			);
		transform: translate3d(0, 0, 0) scale(1.02);
		cursor: pointer;
		box-shadow: 
			0 8px 30px hsl(from var(--color-background) h s calc(l * 0.3) / 0.8),
			0 0 60px hsl(from var(--btn-bg) h s l / 0.15),
			0 0 100px hsl(from var(--btn-bg) h s l / 0.08),
			inset 0 1px 0 hsl(from var(--btn-bg) h s calc(l * 2) / 0.25);
	}

	.button:hover:not(:disabled)::before {
		opacity: 1;
		background: linear-gradient(
			135deg, 
			hsl(from var(--btn-bg) h s l / 0.5),
			hsl(from var(--btn-bg) h s l / 0.4),
			hsl(from var(--btn-bg) h s l / 0.5)
		);
	}

	.button:hover:not(:disabled)::after {
		transform: translateX(100%);
	}

	.button:active:not(:disabled) {
		transform: translateY(0);
		transition: all 0.1s cubic-bezier(0.4, 0, 0.2, 1);
	}

	.button-secondary:hover:not(:disabled) {
		box-shadow:
			0 8px 32px hsl(from var(--color-secondary) h s l / 0.4),
			0 0 80px hsl(from var(--color-secondary) h s l / 0.25),
			0 0 120px hsl(from var(--color-accent) h s l / 0.15),
			inset 0 1px 0 hsl(from var(--color-secondary) h s calc(l * 1.8) / 0.3);
	}

	.button-primary:hover:not(:disabled) {
		box-shadow:
			0 8px 32px hsl(from var(--color-primary) h s l / 0.4),
			0 0 80px hsl(from var(--color-primary) h s l / 0.25),
			inset 0 1px 0 hsl(from var(--color-primary) h s calc(l * 1.8) / 0.3);
	}

	.button-success:hover:not(:disabled) {
		box-shadow:
			0 8px 32px hsl(from var(--color-success) h s l / 0.4),
			0 0 80px hsl(from var(--color-success) h s l / 0.25),
			inset 0 1px 0 hsl(from var(--color-success) h s calc(l * 1.8) / 0.3);
	}

	.button-error:hover:not(:disabled) {
		box-shadow:
			0 8px 32px hsl(from var(--color-error) h s l / 0.4),
			0 0 80px hsl(from var(--color-error) h s l / 0.25),
			inset 0 1px 0 hsl(from var(--color-error) h s calc(l * 1.8) / 0.3);
	}

	.button:disabled {
		color: var(--color-muted);
	}
</style>
