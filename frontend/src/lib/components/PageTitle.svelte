<script lang="ts">
	let { title = '', icon = '' }: { title?: string; icon?: string } = $props();
</script>

<h1 class="title mb-4 flex items-center gap-4 text-3xl font-bold">
	<i class={`${icon} text-2xl`}></i>
	{title}
</h1>
