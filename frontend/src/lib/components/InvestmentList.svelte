<script lang="ts">
	import type { PositionSnapshot } from '$lib/services/positionService';

	let { positionSnapshots }: { positionSnapshots: PositionSnapshot[] } = $props();
</script>

<h1>Investment List</h1>

{#each positionSnapshots as snapshot}
    <div class="snapshot">
        <h2>{snapshot.amount}</h2>
    </div>
{/each}