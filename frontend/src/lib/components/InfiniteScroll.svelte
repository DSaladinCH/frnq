<script lang="ts">
	import { onMount, onDestroy } from 'svelte';

	interface Props {
		onLoadMore: () => void | Promise<void>;
		hasMore: boolean;
		loading: boolean;
		threshold?: number; // pixels from bottom to trigger load
	}

	let { onLoadMore, hasMore, loading, threshold = 300 }: Props = $props();

	let scrollContainer: HTMLElement | null = null;
	let observer: IntersectionObserver | null = null;
	let sentinel: HTMLElement | null = null;
	let isLoading = false;

	async function handleIntersection(entries: IntersectionObserverEntry[]) {
		const entry = entries[0];
		if (entry.isIntersecting && hasMore && !loading && !isLoading) {
			isLoading = true;
			try {
				await onLoadMore();
			} finally {
				isLoading = false;
			}
		}
	}

	async function handleScroll() {
		if (!scrollContainer || !hasMore || loading || isLoading) return;

		const scrollTop = scrollContainer.scrollTop;
		const scrollHeight = scrollContainer.scrollHeight;
		const clientHeight = scrollContainer.clientHeight;

		// Check if we're near the bottom
		if (scrollTop + clientHeight >= scrollHeight - threshold) {
			isLoading = true;
			try {
				await onLoadMore();
			} finally {
				isLoading = false;
			}
		}
	}

	onMount(() => {
		// Find the scrollable parent
		let parent = sentinel?.parentElement;
		while (parent && parent !== document.body) {
			const style = window.getComputedStyle(parent);
			const overflow = style.overflowY;
			if (overflow === 'auto' || overflow === 'scroll') {
				scrollContainer = parent;
				break;
			}
			parent = parent.parentElement;
		}

		// If no scrollable parent found, use the window
		if (!scrollContainer) {
			scrollContainer = document.documentElement;
		}

		// Create intersection observer
		observer = new IntersectionObserver(handleIntersection, {
			root: scrollContainer === document.documentElement ? null : scrollContainer,
			rootMargin: `0px 0px ${threshold}px 0px`,
			threshold: 0
		});

		if (sentinel) {
			observer.observe(sentinel);
		}

		// Add scroll listener as backup
		scrollContainer.addEventListener('scroll', handleScroll);
	});

	onDestroy(() => {
		if (observer && sentinel) {
			observer.unobserve(sentinel);
		}
		observer?.disconnect();
		
		if (scrollContainer) {
			scrollContainer.removeEventListener('scroll', handleScroll);
		}
	});
</script>

<!-- Sentinel element that triggers loading when visible -->
<div bind:this={sentinel} class="h-1"></div>

{#if loading}
	<div class="flex justify-center py-4">
		<div class="text-gray-500">
			<i class="fa fa-spinner fa-spin mr-2"></i>
			Loading more...
		</div>
	</div>
{/if}
