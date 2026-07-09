<script lang="ts">
	import { onMount, tick, type Snippet } from 'svelte';

	type Option = { value: string; label: string };

	let {
		id,
		title = '',
		options,
		selected,
		onSelect,
		direction = 'horizontal'
	}: {
		id?: string;
		title?: string | Snippet;
		options: Option[];
		selected: string;
		onSelect: (value: string) => void;
		direction?: 'horizontal' | 'vertical';
	} = $props();

	id = id || `textbox-${Math.random().toString(36).slice(2, 11)}`;

	let containerEl = $state<HTMLDivElement>();
	let indicatorEl = $state<HTMLDivElement>();

	let resizeRAF = 0;

	// Wait for the relevant font to be loaded so widths are final before measuring.
	async function fontsReady(el: HTMLElement) {
		if (!('fonts' in document)) return;
		const cs = getComputedStyle(el);
		const spec = `${cs.fontStyle} ${cs.fontWeight} ${cs.fontSize} ${cs.fontFamily}`;
		try {
			await (document as any).fonts.load(spec);
		} catch {
			/* ignore */
		}
	}

	// offsetLeft/offsetTop are relative to the nearest positioned ancestor
	// (containerEl, since it's position:relative). Unlike getBoundingClientRect,
	// these are integer layout values unaffected by transforms/scale on any
	// ancestor, so no manual subtraction and no fudge-factor constant needed.
	function moveIndicator(target: HTMLButtonElement, animate: boolean) {
		if (!indicatorEl) return;
		const { offsetLeft, offsetTop, offsetWidth, offsetHeight } = target;

		// Corrective repositions (mount, resize, modal becoming visible,
		// ancestor animation settling) must be instant. Only an actual
		// user click should slide. Otherwise every recompute, including
		// the very first one, gets animated from the indicator's default
		// top-left/0-size state, producing a "flies in from the corner"
		// glitch on mount or whenever a hidden (display:none) modal reveals.
		indicatorEl.classList.toggle('animate-indicator', animate);
		indicatorEl.style.transform = `translate(${offsetLeft}px, ${offsetTop}px)`;
		indicatorEl.style.width = `${offsetWidth}px`;
		indicatorEl.style.height = `${offsetHeight}px`;
	}

	function activeButton(): HTMLButtonElement | null {
		if (!containerEl) return null;
		return containerEl.querySelector(`[data-value="${selected}"]`) as HTMLButtonElement | null;
	}

	async function updateIndicator() {
		await tick();
		const active = activeButton();
		if (!active) return;
		await fontsReady(active);
		moveIndicator(active, false);
	}

	const onResize = () => {
		if (resizeRAF) return;
		resizeRAF = requestAnimationFrame(() => {
			resizeRAF = 0;
			updateIndicator();
		});
	};

	onMount(() => {
		updateIndicator();

		// Catches container size changes directly.
		let ro: ResizeObserver | undefined;
		if (containerEl) {
			ro = new ResizeObserver(() => updateIndicator());
			ro.observe(containerEl);
		}

		// Catches layout-affecting animations/transitions on ANY ancestor
		// (e.g. a parent panel expanding), which ResizeObserver on the
		// container alone won't tell you about if containerEl's own box
		// doesn't change size but its position under an animating parent does.
		const onAncestorAnim = (e: Event) => {
			if (containerEl && e.target instanceof Node && containerEl.contains(e.target)) return;
			updateIndicator();
		};
		document.addEventListener('transitionend', onAncestorAnim, true);
		document.addEventListener('animationend', onAncestorAnim, true);

		window.addEventListener('resize', onResize);

		return () => {
			ro?.disconnect();
			document.removeEventListener('transitionend', onAncestorAnim, true);
			document.removeEventListener('animationend', onAncestorAnim, true);
			window.removeEventListener('resize', onResize);
			if (resizeRAF) cancelAnimationFrame(resizeRAF);
		};
	});

	async function handleSelect(value: string, el?: HTMLButtonElement) {
		onSelect(value);
		await tick();
		const target = el ?? activeButton();
		if (!target) return;
		await fontsReady(target);
		moveIndicator(target, true);
	}
</script>

<div class="flex h-full flex-col gap-1">
	{#if title}
		<!-- Check if title is string or snippet -->
		<label for={id} class="leading-none">
			{#if typeof title === 'string'}
				{title}
			{:else}
				{@render title()}
			{/if}
		</label>
	{/if}

	<div
		bind:this={containerEl}
		class="gradient-border flex bg-card relative gap-0.5 rounded-xl p-1 {direction === 'horizontal'
			? 'flex-row'
			: 'flex-col'}"
		style="--gradient-border-radius: 0.75rem;"
		{id}
	>
		<div
			bind:this={indicatorEl}
			class="bg-secondary z-1 pointer-events-none absolute left-0 top-0 rounded-lg"
		></div>

		{#each options as option}
			<button
				type="button"
				class="pill-button z-2 color-muted relative min-w-fit cursor-pointer whitespace-nowrap rounded-lg border-0 bg-transparent px-4 py-2.5
					text-sm font-medium leading-none transition-colors duration-200 ease-in-out {direction ===
				'vertical'
					? 'w-full text-center'
					: ''}"
				class:active={option.value === selected}
				data-value={option.value}
				onclick={(e) => handleSelect(option.value, e.currentTarget as HTMLButtonElement)}
			>
				{option.label}
			</button>
		{/each}
	</div>
</div>

<style>
	:global(.animate-indicator) {
		transition:
			transform 300ms ease-out,
			width 300ms ease-out,
			height 300ms ease-out;
	}

	.pill-button:hover {
		color: var(--color-text);
	}

	.pill-button.active {
		color: var(--color-text);
	}
</style>
