<script lang="ts">
	import IconButton from '$lib/components/IconButton.svelte';
	import Input from '$lib/components/Input.svelte';
	import PageHead from '$lib/components/PageHead.svelte';
	import PageTitle from '$lib/components/PageTitle.svelte';
	import ExternalAccountsSection from '$lib/components/ExternalAccountsSection.svelte';
	import DropDown from '$lib/components/DropDown.svelte';
	import { dataStore } from '$lib/stores/dataStore';
	import { userPreferences } from '$lib/stores/userPreferences';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import { notify } from '$lib/services/notificationService';
	import { tick } from 'svelte';
	import { DateFormatType } from '$lib/utils/dateFormat';
	import { NumberFormatType } from '$lib/utils/numberFormat';

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

	// User preferences
	let preferences = $state($userPreferences);
	let savingDateFormat = $state(false);
	let savingNumberFormat = $state(false);
	let savingForecastNumber = $state(false);

	// Subscribe to store changes
	$effect(() => {
		const unsubscribe = dataStore.subscribe(() => {
			groups = dataStore.groups;
			secondaryLoading = dataStore.secondaryLoading;
			error = dataStore.error;
		});
		return unsubscribe;
	});

	// Subscribe to user preferences changes
	$effect(() => {
		const unsubscribe = userPreferences.subscribe((prefs) => {
			preferences = prefs;
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

	async function handleSettingsUpdate(dateFormat: string, numberFormat: string, forecastNumberOfInvestments: number) {
		if (savingDateFormat || savingNumberFormat || savingForecastNumber) return;

		savingDateFormat = preferences.dateFormat !== dateFormat as DateFormatType;
		savingNumberFormat = preferences.numberFormat !== numberFormat as NumberFormatType;
		savingForecastNumber = preferences.forecastNumberOfInvestments !== forecastNumberOfInvestments;

		try {
			const success = await userPreferences.updateUser(dateFormat as DateFormatType, numberFormat as NumberFormatType, forecastNumberOfInvestments);
			if (success) {
				notify.success('Settings updated successfully');
			} else {
				notify.error('Failed to update settings');
			}
		} catch (error) {
			notify.error('Failed to update settings');
		} finally {
			savingDateFormat = false;
			savingNumberFormat = false;
			savingForecastNumber = false;
		}
	}

	const dateFormatOptions = [
		{ value: DateFormatType.English, label: 'English (MM/DD/YYYY)' },
		{ value: DateFormatType.German, label: 'German (DD.MM.YYYY)' }
	];

	const numberFormatOptions = [
		{ value: NumberFormatType.English, label: 'English (1,234.56)' },
		{ value: NumberFormatType.German, label: 'German (1.234,56)' },
		{ value: NumberFormatType.Swiss, label: "Swiss (1'234.56)" }
	];
</script>

<PageHead title="Settings" />

<div class="xs:p-8 w-full p-4">
	<PageTitle title="Settings" icon="fa-solid fa-gear" />

	<div class="grid grid-cols-1 xl:grid-cols-2 gap-6">
		<div class="bg-card rounded-lg p-6 shadow-lg">
			<h2 class="mb-2 text-xl font-semibold">Groups</h2>
			<p class="color-muted mb-4">Manage your quote groups here.</p>
			<div class="bg-background relative mt-2 h-60 w-full overflow-hidden rounded-lg">
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

		<div class="bg-card rounded-lg p-6 shadow-lg">
			<h2 class="mb-2 text-xl font-semibold">Customization</h2>
			<p class="color-muted mb-4">
				Configure various application settings to customize your experience.
			</p>
			<div class="flex flex-col gap-4">
				<div class="flex flex-col gap-2">
					<span class="color-default font-medium">Date Format:</span>
					<div class="xs:w-64">
						<DropDown
							options={dateFormatOptions}
							value={preferences.dateFormat}
							disabled={savingDateFormat}
							isLoading={savingDateFormat}
							onchange={(e) => handleSettingsUpdate(e, preferences.numberFormat, preferences.forecastNumberOfInvestments)}
						/>
					</div>
				</div>
				<div class="flex flex-col gap-2">
					<span class="color-default font-medium">Number Format:</span>
					<div class="xs:w-64">
						<DropDown
							options={numberFormatOptions}
							value={preferences.numberFormat}
							disabled={savingNumberFormat}
							isLoading={savingNumberFormat}
							onchange={(e) => handleSettingsUpdate(preferences.dateFormat, e, preferences.forecastNumberOfInvestments)}
						/>
					</div>
				</div>
				<div class="flex flex-col gap-2">
					<span class="color-default font-medium">Investments in Forecast (per Quote):</span>
					<div class="xs:w-32">
						<Input
							type="number"
							min="2"
							max="100"
							bind:value={preferences.forecastNumberOfInvestments}
							disabled={savingForecastNumber}
							onchange={(e) => handleSettingsUpdate(preferences.dateFormat, preferences.numberFormat, preferences.forecastNumberOfInvestments)}
						/>
					</div>
				</div>
			</div>
		</div>

		<ExternalAccountsSection />
	</div>
</div>

<style>
	.group-item:hover {
		background-color: var(--color-background-backdrop);
	}
</style>
