import type { QuoteGroup } from '$lib/Models/QuoteGroup';
import { fetchWithAuth } from './authService';

// src/lib/services/positionService.ts
const baseUrl = import.meta.env.VITE_API_BASE_URL;

export async function getQuoteGroups(): Promise<QuoteGroup[]> {
	const url = `${baseUrl}/api/groups`;
	const res = await fetchWithAuth(url);

	if (!res.ok) throw new Error('Failed to fetch quote groups');
	return res.json();
}

export async function createQuoteGroup(name: string): Promise<void> {
	const quoteGroup: QuoteGroup = { id: 0, name }; // id will be set by the server

	const url = `${baseUrl}/api/groups`;
	const res = await fetchWithAuth(url, {
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

	const url = `${baseUrl}/api/groups/${quoteGroup.id}`;
	const res = await fetchWithAuth(url, {
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
	const url = `${baseUrl}/api/groups/${id}`;
	const res = await fetchWithAuth(url, {
		method: 'DELETE'
	});

	if (!res.ok) throw new Error('Failed to delete quote group');
	return;
}

export async function assignQuoteToGroup(quoteId: number, groupId: number): Promise<void> {
	const url = `${baseUrl}/api/groups/${groupId}/${quoteId}`;
	const res = await fetchWithAuth(url, {
		method: 'POST'
	});

	if (!res.ok) throw new Error('Failed to assign quote to group');
	return;
}

export async function removeQuoteFromGroup(quoteId: number, groupId: number): Promise<void> {
	const url = `${baseUrl}/api/groups/${groupId}/${quoteId}`;
	const res = await fetchWithAuth(url, {
		method: 'DELETE'
	});

	if (!res.ok) throw new Error('Failed to remove quote from group');
	return;
}
