<script lang="ts">
	import Button from '$lib/components/Button.svelte';
	import IconButton from '$lib/components/IconButton.svelte';
	import Input from '$lib/components/Input.svelte';
	import type { QuoteGroup } from '$lib/Models/QuoteGroup';
	import { createQuoteGroup, deleteQuoteGroup, getQuoteGroups, updateQuoteGroup } from '$lib/services/groupService';
	import { dataStore } from '$lib/stores/dataStore';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import { ContentWidth } from '$lib/types/ContentSize';
	import { StylePadding } from '$lib/types/StylePadding';
	import { TextSize } from '$lib/types/TextSize';
	import { onMount, tick } from 'svelte';

	// Reactive values that track the store
	let loading = $state(dataStore.loading);
	let error = $state(dataStore.error);
	let loadingGroups = $state(false);

	let groups = $state<QuoteGroup[]>([]);

	let newGroupName = $state('');

	let groupsElement: HTMLDivElement = $state()!;
	let editMode: number | null = $state(null);

	onMount(async () => {
		await reloadSettingsData();
	});

	async function reloadSettingsData() {
		loadingGroups = true;
		groups = await getQuoteGroups();
		loadingGroups = false;
	}

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

	function toggleEdit(groupId: number | null = null) {
		editMode = groupId;

		// Get current group name if editing existing group
		if (editMode && editMode !== 0) {
			const group = groups.find((g) => g.id === editMode);
			newGroupName = group ? group.name : '';
		}

		if (editMode === null) {
			newGroupName = '';
		}
	}

	async function saveGroup() {
		try {
			loadingGroups = true;

			if (newGroupName.trim() !== '') {
				if (editMode === 0) {
					await createQuoteGroup(newGroupName.trim());
				} else if (editMode !== null) {
					await updateQuoteGroup(editMode, newGroupName.trim());
				}

				await reloadSettingsData();
				newGroupName = '';
				editMode = null;
				await tick();

				if (groupsElement) {
					groupsElement.scrollTop = groupsElement.scrollHeight;
				}
			}
		} finally {
			loadingGroups = false;
		}
	}

	async function deleteGroup(groupId: number) {
		try {
			loadingGroups = true;
			await deleteQuoteGroup(groupId);

			await reloadSettingsData();
			newGroupName = '';
			editMode = null;
			await tick();

			if (groupsElement) {
				groupsElement.scrollTop = groupsElement.scrollHeight;
			}
		} finally {
			loadingGroups = false;
		}
	}
</script>

<div class="container mx-auto p-6">
	<h1 class="mb-6 text-3xl font-bold">Settings</h1>

	<div class="bg-card rounded-lg p-6 shadow-lg">
		<h2 class="mb-4 text-xl font-semibold">Data Management</h2>
		<p class="mb-4 text-gray-600">Refresh your portfolio data to get the latest information.</p>

		<button class="btn btn-primary" onclick={refreshData} disabled={loading}>
			{loading ? 'Refreshing...' : 'Refresh Data'}
		</button>

		{#if error}
			<div class="mt-4 rounded border border-red-400 bg-red-100 p-3 text-red-700">
				Error: {error}
			</div>
		{/if}
	</div>

	<div class="bg-card mt-6 rounded-lg p-6 shadow-lg">
		<h2 class="mb-2 text-xl font-semibold">Groups</h2>
		<p class="color-muted mb-4">Manage your quote groups here.</p>
		<div class="bg-background md:w-md relative mt-2 h-60 w-full overflow-hidden rounded-lg">
			{#if loadingGroups}
				<div class="flex h-full w-full scale-125 items-center justify-center">
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
				</div>
			{:else}
				<div bind:this={groupsElement} class="grid max-h-60 w-full grid-cols-1 overflow-y-auto">
					<div class="group-item h-12 px-4 {editMode === 0 ? 'py-1' : 'py-3'} my-auto">
						{#if editMode === 0}
							<div class="flex h-full gap-4">
								<div class="flex-1">
									<Input type="text" placeholder="New Group Name" bind:value={newGroupName} />
								</div>
								<div class="my-auto flex gap-1">
									<div class="w-6">
										<IconButton
											icon="fa-solid fa-plus"
											tooltip="Add Group"
											hoverColor={ColorStyle.Success}
											onclick={saveGroup}
										/>
									</div>
									<div class="w-6">
										<IconButton
											icon="fa-solid fa-xmark"
											tooltip="Cancel"
											hoverColor={ColorStyle.Error}
											onclick={toggleEdit}
										/>
									</div>
								</div>
							</div>
						{:else}
							<button class="btn-fake flex w-full items-center" onclick={() => toggleEdit(0)}>
								<span class="">New Group</span>
								<i class="fa-solid fa-plus color-muted ml-auto text-xs"></i>
							</button>
						{/if}
					</div>
					<hr class="color-muted mx-4 my-1 w-5 border-t" />

					{#each groups as group, i}
						<div class="group-item flex h-12 items-center justify-center px-4 py-1">
							{#if editMode === group.id}
								<div class="flex h-full w-full gap-4">
									<div class="flex-1 text-left">
										<Input type="text" placeholder="New Group Name" bind:value={newGroupName} />
									</div>
									<div class="my-auto flex gap-1">
										<div class="w-6">
											<IconButton
												icon="fa-solid fa-check"
												tooltip="Update Group"
												hoverColor={ColorStyle.Success}
												onclick={saveGroup}
											/>
										</div>
										<div class="w-6">
											<IconButton
												icon="fa-solid fa-xmark"
												tooltip="Cancel"
												hoverColor={ColorStyle.Error}
												onclick={toggleEdit}
											/>
										</div>
									</div>
								</div>
							{:else}
								<span class="line-clamp-1 flex-1 text-left">{group.name}</span>
								<div class="my-auto flex gap-1">
									<div class="w-8">
										<IconButton
											icon="fa-solid fa-pen"
											tooltip="Edit Group"
											hoverColor={ColorStyle.Success}
											onclick={() => toggleEdit(group.id)}
										/>
									</div>
									<div class="w-8">
										<IconButton
											icon="fa-solid fa-trash"
											tooltip="Delete Group"
											hoverColor={ColorStyle.Error}
											onclick={() => deleteGroup(group.id)}
										/>
									</div>
								</div>
							{/if}
						</div>
						{#if i < groups.length - 1}
							<hr class="color-muted mx-4 my-1 w-5 border-t" />
						{/if}
					{/each}
				</div>
			{/if}
		</div>
	</div>

	<div class="bg-card mt-6 rounded-lg p-6 shadow-lg">
		<h2 class="mb-4 text-xl font-semibold">About</h2>
		<p class="text-gray-600">FRNQ Portfolio Management System</p>
		<p class="mt-2 text-sm text-gray-500">
			Data is cached and persists across navigation for better performance.
		</p>
	</div>
</div>

<style>
	.group-item:hover {
		background-color: var(--color-background-backdrop);
	}
</style>
