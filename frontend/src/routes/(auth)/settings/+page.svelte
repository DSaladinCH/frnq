<script lang="ts">
	import { dataStore } from '$lib/stores/dataStore';
	
	// Reactive values that track the store
	let loading = $state(dataStore.loading);
	let error = $state(dataStore.error);

	// Subscribe to store changes
	$effect(() => {
		const unsubscribe = dataStore.subscribe(() => {
			loading = dataStore.loading;
			error = dataStore.error;
		});
		return unsubscribe;
	});
	
	async function refreshData() {
		await dataStore.refreshData();
	}
</script>

<div class="container mx-auto p-6">
	<h1 class="text-3xl font-bold mb-6">Settings</h1>
	
	<div class="bg-card p-6 rounded-lg shadow-lg">
		<h2 class="text-xl font-semibold mb-4">Data Management</h2>
		<p class="text-gray-600 mb-4">Refresh your portfolio data to get the latest information.</p>
		
		<button 
			class="btn btn-primary"
			onclick={refreshData}
			disabled={loading}
		>
			{loading ? 'Refreshing...' : 'Refresh Data'}
		</button>
		
		{#if error}
			<div class="mt-4 p-3 bg-red-100 border border-red-400 text-red-700 rounded">
				Error: {error}
			</div>
		{/if}
	</div>
	
	<div class="bg-card p-6 rounded-lg shadow-lg mt-6">
		<h2 class="text-xl font-semibold mb-4">About</h2>
		<p class="text-gray-600">
			FRNQ Portfolio Management System
		</p>
		<p class="text-gray-500 text-sm mt-2">
			Data is cached and persists across navigation for better performance.
		</p>
	</div>
</div>
