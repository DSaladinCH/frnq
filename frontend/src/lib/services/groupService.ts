import type { QuoteGroup } from '$lib/Models/QuoteGroup';
import { fetchWithAuth } from './authService';

export async function getQuoteGroups(): Promise<QuoteGroup[]> {
	const res = await fetchWithAuth('/api/groups');

	if (!res.ok) throw new Error('Failed to fetch quote groups');
	return res.json();
}

export async function createQuoteGroup(name: string): Promise<void> {
	const quoteGroup: QuoteGroup = { id: 0, name }; // id will be set by the server

	const res = await fetchWithAuth('/api/groups', {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(quoteGroup)
	});

	if (!res.ok) throw new Error('Failed to add quote group');
	return;
}

export async function updateQuoteGroup(id: number, name: string): Promise<QuoteGroup> {
	const quoteGroup: QuoteGroup = { id, name };

	const res = await fetchWithAuth(`/api/groups/${quoteGroup.id}`, {
		method: 'PUT',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(quoteGroup)
	});

	if (!res.ok) throw new Error('Failed to update quote group');
	return res.json();
}

export async function deleteQuoteGroup(id: number): Promise<void> {
	const res = await fetchWithAuth(`/api/groups/${id}`, {
		method: 'DELETE'
	});

	if (!res.ok) throw new Error('Failed to delete quote group');
	return;
}

export async function assignQuoteToGroup(quoteId: number, groupId: number): Promise<void> {
	const res = await fetchWithAuth(`/api/groups/${groupId}/${quoteId}`, {
		method: 'POST'
	});

	if (!res.ok) throw new Error('Failed to assign quote to group');
	return;
}

export async function removeQuoteFromGroup(quoteId: number, groupId: number): Promise<void> {
	const res = await fetchWithAuth(`/api/groups/${groupId}/${quoteId}`, {
		method: 'DELETE'
	});

	if (!res.ok) throw new Error('Failed to remove quote from group');
	return;
}
