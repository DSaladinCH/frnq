<script lang="ts">
	import { onMount } from 'svelte';
	import { fetchWithAuth } from '$lib/services/authService';
	import type { GeneralFeeViewDto, GroupFeesSummary } from '$lib/services/positionService';
	import Button from './Button.svelte';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import { TextSize } from '$lib/types/TextSize';
	import { formatDateTime, formatDateForDisplay } from '$lib/utils/dateFormat';
	import { userPreferences } from '$lib/stores/userPreferences';
	import { StylePadding } from '$lib/types/StylePadding';
	import { ContentWidth } from '$lib/types/ContentSize';

	let {
		feesSummaries = [],
		overallFees = 0,
		groupId = null
	}: {
		feesSummaries: GroupFeesSummary[];
		overallFees: number;
		groupId: number | null;
	} = $props();

	let preferences = $state($userPreferences);
	let fees: GeneralFeeViewDto[] = $state([]);
	let isLoading = $state(false);
	let error: string | null = $state(null);
	let showAddForm = $state(false);
	let selectedFeeForEdit: GeneralFeeViewDto | null = $state(null);

	// Form fields
	let amount = $state('');
	let date = $state(new Date().toISOString().split('T')[0]);
	let description = $state('');
	let selectedGroupId = $state<number | null>(groupId);

	// Subscribe to user preferences changes
	$effect(() => {
		const unsubscribe = userPreferences.subscribe((prefs) => {
			preferences = prefs;
		});
		return unsubscribe;
	});

	async function loadFees() {
		isLoading = true;
		error = null;
		try {
			const url = groupId ? `/api/general-fees?groupId=${groupId}` : '/api/general-fees';
			const res = await fetchWithAuth(url);
			if (!res.ok) throw new Error('Failed to load fees');
			fees = await res.json();
		} catch (err) {
			error = err instanceof Error ? err.message : 'Failed to load fees';
		} finally {
			isLoading = false;
		}
	}

	async function handleAddFee(e: SubmitEvent) {
		e.preventDefault();
		error = null;

		if (!amount || !date || !description) {
			error = 'All fields are required';
			return;
		}

		try {
			const res = await fetchWithAuth('/api/general-fees', {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({
					amount: parseFloat(amount),
					date: new Date(date).toISOString(),
					description,
					groupId: selectedGroupId
				})
			});

			if (!res.ok) throw new Error('Failed to create fee');

			// Reset form and reload
			amount = '';
			date = new Date().toISOString().split('T')[0];
			description = '';
			showAddForm = false;
			await loadFees();
		} catch (err) {
			error = err instanceof Error ? err.message : 'Failed to create fee';
		}
	}

	async function handleUpdateFee(e: SubmitEvent) {
		e.preventDefault();
		if (!selectedFeeForEdit) return;

		error = null;

		try {
			const res = await fetchWithAuth(`/api/general-fees/${selectedFeeForEdit.id}`, {
				method: 'PUT',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({
					amount: amount ? parseFloat(amount) : undefined,
					date: date ? new Date(date).toISOString() : undefined,
					description: description || undefined,
					groupId: selectedGroupId
				})
			});

			if (!res.ok) throw new Error('Failed to update fee');

			// Reset and reload
			selectedFeeForEdit = null;
			amount = '';
			date = new Date().toISOString().split('T')[0];
			description = '';
			await loadFees();
		} catch (err) {
			error = err instanceof Error ? err.message : 'Failed to update fee';
		}
	}

	async function handleDeleteFee(feeId: number) {
		if (!confirm('Are you sure you want to delete this fee?')) return;

		error = null;
		try {
			const res = await fetchWithAuth(`/api/general-fees/${feeId}`, {
				method: 'DELETE'
			});

			if (!res.ok) throw new Error('Failed to delete fee');

			await loadFees();
		} catch (err) {
			error = err instanceof Error ? err.message : 'Failed to delete fee';
		}
	}

	function startEdit(fee: GeneralFeeViewDto) {
		selectedFeeForEdit = fee;
		amount = fee.amount.toString();
		date = new Date(fee.date).toISOString().split('T')[0];
		description = fee.description;
		selectedGroupId = fee.groupId;
	}

	function cancelEdit() {
		selectedFeeForEdit = null;
		amount = '';
		date = new Date().toISOString().split('T')[0];
		description = '';
	}

	function formatCurrency(value: number): string {
		return value.toLocaleString(undefined, { style: 'currency', currency: 'CHF' });
	}

	onMount(() => {
		loadFees();
	});
</script>

<div class="general-fees-panel">
	<div class="fees-header">
		<h3>General Fees</h3>
		{#if overallFees > 0}
			<div class="total-fees">Total: {formatCurrency(overallFees)}</div>
		{/if}
		<Button onclick={() => (showAddForm = !showAddForm)} variant={ColorStyle.Primary} size={TextSize.Small}>
			{showAddForm ? 'Cancel' : 'Add Fee'}
		</Button>
	</div>

	{#if error}
		<div class="error-message">{error}</div>
	{/if}

	{#if showAddForm}
		<form onsubmit={handleAddFee} class="fee-form">
			<div class="form-group">
				<label for="amount">Amount</label>
				<input type="number" id="amount" bind:value={amount} step="0.01" required />
			</div>
			<div class="form-group">
				<label for="date">Date</label>
				<input type="date" id="date" bind:value={date} required />
			</div>
			<div class="form-group">
				<label for="description">Description</label>
				<input type="text" id="description" bind:value={description} required />
			</div>
			<div class="form-actions">
				<Button type="submit" variant={ColorStyle.Success} size={TextSize.Small}>Save</Button>
				<Button
					onclick={() => (showAddForm = false)}
					variant={ColorStyle.Secondary}
					size={TextSize.Small}
				>
					Cancel
				</Button>
			</div>
		</form>
	{/if}

	{#if isLoading}
		<div class="loading">Loading fees...</div>
	{:else if fees.length === 0}
		<div class="no-fees">No general fees recorded</div>
	{:else}
		<div class="fees-table">
			<table>
				<thead>
					<tr>
						<th>Date</th>
						<th>Description</th>
						<th>Amount</th>
						<th>Actions</th>
					</tr>
				</thead>
				<tbody>
					{#each fees as fee (fee.id)}
						{#if selectedFeeForEdit?.id === fee.id}
							<tr class="edit-row">
								<td colspan="4">
									<form onsubmit={handleUpdateFee} class="inline-edit-form">
										<div class="form-group">
											<input
												type="date"
												bind:value={date}
												value={new Date(fee.date).toISOString().split('T')[0]}
											/>
										</div>
										<div class="form-group">
											<input
												type="text"
												bind:value={description}
												value={fee.description}
											/>
										</div>
										<div class="form-group">
											<input
												type="number"
												bind:value={amount}
												value={fee.amount}
												step="0.01"
											/>
										</div>
										<div class="form-actions">
											<Button type="submit" variant={ColorStyle.Success} size={TextSize.Small}>
												Save
											</Button>
											<Button
												onclick={cancelEdit}
												variant={ColorStyle.Secondary}
												size={TextSize.Small}
											>
												Cancel
											</Button>
										</div>
									</form>
								</td>
							</tr>
						{:else}
							<tr>
								<td>{formatDateForDisplay(new Date(fee.date))}</td>
								<td>{fee.description}</td>
								<td>{formatCurrency(fee.amount)}</td>
								<td>
									<Button
										onclick={() => startEdit(fee)}
										variant={ColorStyle.Secondary}
										size={TextSize.Small}
									>
										Edit
									</Button>
									<Button
										onclick={() => handleDeleteFee(fee.id)}
										variant={ColorStyle.Danger}
										size={TextSize.Small}
									>
										Delete
									</Button>
								</td>
							</tr>
						{/if}
					{/each}
				</tbody>
			</table>
		</div>
	{/if}
</div>

<style>
	.general-fees-panel {
		padding: 1rem;
		border: 1px solid var(--border-color);
		border-radius: 0.5rem;
		background-color: var(--background-secondary);
	}

	.fees-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 1rem;
	}

	.fees-header h3 {
		margin: 0;
		font-size: 1.25rem;
	}

	.total-fees {
		font-weight: bold;
		font-size: 1rem;
	}

	.error-message {
		color: var(--error-color);
		padding: 0.5rem;
		margin-bottom: 1rem;
		border-radius: 0.25rem;
		background-color: var(--error-background);
	}

	.fee-form,
	.inline-edit-form {
		display: flex;
		flex-direction: column;
		gap: 1rem;
		padding: 1rem;
		background-color: var(--background-primary);
		border-radius: 0.5rem;
		margin-bottom: 1rem;
	}

	.form-group {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
	}

	.form-group label {
		font-weight: 500;
		font-size: 0.875rem;
	}

	.form-group input {
		padding: 0.5rem;
		border: 1px solid var(--border-color);
		border-radius: 0.25rem;
		font-size: 1rem;
	}

	.form-actions {
		display: flex;
		gap: 0.5rem;
	}

	.loading {
		text-align: center;
		padding: 1rem;
		color: var(--text-secondary);
	}

	.no-fees {
		text-align: center;
		padding: 1rem;
		color: var(--text-secondary);
	}

	.fees-table {
		overflow-x: auto;
	}

	table {
		width: 100%;
		border-collapse: collapse;
	}

	th,
	td {
		padding: 0.75rem;
		text-align: left;
		border-bottom: 1px solid var(--border-color);
	}

	th {
		background-color: var(--background-tertiary);
		font-weight: 600;
	}

	.edit-row {
		background-color: var(--background-tertiary);
	}
</style>