<script lang="ts">
    let { title } : { title?: string } = $props();

    let fullTitle = $derived(title ? `${title} - FRNQ` : "FRNQ");
</script>

<svelte:head>
    <title>{fullTitle}</title>
</svelte:head>