<script lang="ts">
	import IconButton from '$lib/components/IconButton.svelte';
	import Input from '$lib/components/Input.svelte';
	import PageHead from '$lib/components/PageHead.svelte';
	import PageTitle from '$lib/components/PageTitle.svelte';
	import { dataStore } from '$lib/stores/dataStore';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import { tick } from 'svelte';

	// Reactive values that track the store
	let secondaryLoading = $state(dataStore.secondaryLoading);
	let error = $state(dataStore.error);
	let groups = $state(dataStore.groups);

	let newGroupName = $state('');

	let groupsElement: HTMLDivElement = $state()!;
	let editMode: number | null = $state(null);

	let addingGroup = $state(false);
	let savingGroup = $state(false);
	let deletingGroup = $state<number | null>(null);

	// Subscribe to store changes
	$effect(() => {
		const unsubscribe = dataStore.subscribe(() => {
			groups = dataStore.groups;
			secondaryLoading = dataStore.secondaryLoading;
			error = dataStore.error;
		});
		return unsubscribe;
	});

	async function refreshData() {
		if (secondaryLoading) return;
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
		if (secondaryLoading) return;

		try {
			if (newGroupName.trim() !== '') {
				if (editMode === 0) {
					addingGroup = true;
					await dataStore.addQuoteGroup(newGroupName.trim());
				} else if (editMode !== null) {
					savingGroup = true;
					await dataStore.updateQuoteGroup(editMode, newGroupName.trim());
				}

				newGroupName = '';
				editMode = null;
				await tick();

				if (groupsElement) {
					groupsElement.scrollTop = groupsElement.scrollHeight;
				}
			}
		} finally {
			addingGroup = false;
			savingGroup = false;
		}
	}

	async function deleteGroup(groupId: number) {
		if (secondaryLoading) return;
		try {
			deletingGroup = groupId;
			await dataStore.deleteQuoteGroup(groupId);

			newGroupName = '';
			editMode = null;
			await tick();

			if (groupsElement) {
				groupsElement.scrollTop = groupsElement.scrollHeight;
			}
		} finally {
			deletingGroup = null;
		}
	}
</script>

<PageHead title="Settings" />

<div class="xs:p-8 w-full p-4">
	<PageTitle title="Settings" icon="fa-solid fa-gear" />

	<div class="bg-card rounded-lg p-6 shadow-lg">
		<h2 class="mb-4 text-xl font-semibold">Data Management</h2>
		<p class="mb-4 text-gray-600">Refresh your portfolio data to get the latest information.</p>

		<button class="btn btn-primary" onclick={refreshData} disabled={secondaryLoading}>
			{secondaryLoading ? 'Refreshing…' : 'Refresh Data'}
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
										isLoading={addingGroup}
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
							<i class="fa-solid fa-plus color-muted ml-auto text-base"></i>
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
											isLoading={savingGroup && editMode === group.id}
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
										isLoading={deletingGroup === group.id}
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
