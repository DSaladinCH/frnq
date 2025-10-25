<script lang="ts">
	import { onMount, tick } from 'svelte';
	import DropDown from './DropDown.svelte';

	type Option = { value: string; label: string };

	let {
		options,
		selected,
		onSelect,
		direction = 'horizontal'
	}: {
		options: Option[];
		selected: string;
		onSelect: (value: string) => void;
		direction?: 'horizontal' | 'vertical';
	} = $props();

	let containerEl = $state<HTMLDivElement>();
	let indicatorEl = $state<HTMLDivElement>();

	// === Utilities ===
	const SHIFT = 5; // your extra shift

	const isH = () => direction === 'horizontal';

	// Throttled resize handler (rAF)
	let resizeRAF = 0;

	// One compact “fonts + layout flush”
	async function layoutReady(el?: HTMLElement) {
		if ('fonts' in document && el) {
			const cs = getComputedStyle(el);
			const spec = `${cs.fontStyle} ${cs.fontWeight} ${cs.fontSize} ${cs.fontFamily}`;
			try {
				await (document as any).fonts.load(spec);
			} catch {}
		}
		// rAF + forced read = paint & layout sync
		await new Promise<void>((r) =>
			requestAnimationFrame(() => {
				void document.body.offsetWidth;
				r();
			})
		);
	}

	// Measure helper
	function rects(target: HTMLElement) {
		const c = containerEl!.getBoundingClientRect();
		const b = target.getBoundingClientRect();
		return { c, b, left: b.left - c.left - SHIFT, top: b.top - c.top - SHIFT };
	}

	// Move/size indicator (axis-agnostic)
	function moveIndicator(target: HTMLElement) {
		const { b, left, top } = rects(target);

		if (isH()) {
			indicatorEl!.style.transform = `translateX(${left}px)`;
			indicatorEl!.style.width = `${b.width}px`;
			indicatorEl!.style.height = `${b.height}px`;
		} else {
			indicatorEl!.style.transform = `translateY(${top}px)`;
			indicatorEl!.style.height = `${b.height}px`;
			indicatorEl!.style.width = `${b.width}px`;
		}
	}

	// === Core actions ===
	async function updateIndicator() {
		if (!containerEl || !indicatorEl) return;

		// Wait for DOM update
		await tick();

		const active = containerEl.querySelector(
			`[data-value="${selected}"]`
		) as HTMLButtonElement | null;

		if (!active) return;

		await layoutReady(active);
		moveIndicator(active);
	}

	const onResize = () => {
		if (resizeRAF) return;

		resizeRAF = requestAnimationFrame(() => {
			resizeRAF = 0;
			updateIndicator();
		});
	};

	onMount(() => {
		// initial pass (no setTimeout needed)
		updateIndicator();

		window.addEventListener('resize', onResize);

		let ro: ResizeObserver | undefined;
		if (containerEl) {
			ro = new ResizeObserver(() => {
				updateIndicator();
			});
			ro.observe(containerEl);
		}

		return () => {
			window.removeEventListener('resize', onResize);
			ro?.disconnect();
			if (resizeRAF) cancelAnimationFrame(resizeRAF);
		};
	});

	// Public handler; prefer the clicked element to avoid re-querying
	async function handleSelect(value: string, el?: HTMLButtonElement) {
		onSelect(value);
		await tick();
		if (!containerEl || !indicatorEl) return;

		const target =
			el ?? (containerEl.querySelector(`[data-value="${value}"]`) as HTMLButtonElement | null);
		if (!target) return;

		await layoutReady(target);
		moveIndicator(target);
	}
</script>

<!-- Pill Toggle Display -->
<div
	bind:this={containerEl}
	class="flex bg-card border-1 border-button relative gap-0.5 rounded-xl p-1 {direction ===
	'horizontal'
		? 'flex-row'
		: 'flex-col'}"
>
	<div
		bind:this={indicatorEl}
		class="bg-secondary z-1 pointer-events-none absolute left-1 top-1 translate-x-0 rounded-lg transition-all duration-300 ease-out"
	></div>

	{#each options as option}
		<button
			type="button"
			class="pill-button z-2 color-muted relative min-w-fit cursor-pointer whitespace-nowrap rounded-lg border-0 bg-transparent px-4 py-2.5
					text-sm font-medium leading-none transition-colors duration-200 ease-in-out {direction ===
			'vertical'
				? 'w-full text-center'
				: ''}
					{option.value === selected ? 'font-semibold' : ''}"
			class:active={option.value === selected}
			data-value={option.value}
			onclick={(e) => handleSelect(option.value, e.currentTarget as HTMLButtonElement)}
		>
			{option.label}
		</button>
	{/each}
</div>

<style>
	.pill-button:hover {
		color: var(--color-text);
	}

	.pill-button.active {
		color: var(--color-text);
	}
</style>
