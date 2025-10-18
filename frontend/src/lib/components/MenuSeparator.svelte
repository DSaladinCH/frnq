<script lang="ts">
	let {
		visible = true
	}: {
		visible?: boolean;
	} = $props();
</script>

{#if visible}
	<div class="mx-auto my-1 h-px w-10 bg-button"></div>
{/if}
