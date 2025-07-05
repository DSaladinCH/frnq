<script lang="ts">
	import type { PositionSnapshot, QuoteModel } from '$lib/services/positionService';

	let { positionSnapshots, quotes }: { positionSnapshots: PositionSnapshot[], quotes: QuoteModel[] } = $props();
</script>

<h1>Investment List</h1>

<div class="positions-list grid gap-2">
    {#each positionSnapshots as snapshot}
        <div class="position-item card card-slim card-reactive">
            <h2>{snapshot.amount}</h2>
        </div>
    {/each}
</div>

<style>
</style>