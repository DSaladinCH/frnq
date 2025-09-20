import type { QuoteGroup } from "$lib/Models/QuoteGroup";
import { fetchWithAuth } from "./authService";

// src/lib/services/positionService.ts
const baseUrl = import.meta.env.VITE_API_BASE_URL;

export async function getQuoteGroups(): Promise<QuoteGroup[]> {
    const startTime = Date.now();

    const url = `${baseUrl}/api/groups`;
    const res = await fetchWithAuth(url);

    // wait at least 1 second, to improve UX
    await new Promise((resolve) => setTimeout(resolve, Math.max(0, 1000 - (Date.now() - startTime))));

    if (!res.ok) throw new Error('Failed to fetch quote groups');
    return res.json();
}

export async function createQuoteGroup(name: string): Promise<void> {
    const startTime = Date.now();

    const quoteGroup: QuoteGroup = { id: 0, name }; // id will be set by the server

    const url = `${baseUrl}/api/groups`;
    const res = await fetchWithAuth(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(quoteGroup)
    });

    // wait at least 1 second, to improve UX
    await new Promise((resolve) => setTimeout(resolve, Math.max(0, 1000 - (Date.now() - startTime))));

    if (!res.ok) throw new Error('Failed to add quote group');
    return;
}

export async function updateQuoteGroup(id: number, name: string): Promise<QuoteGroup> {
    const startTime = Date.now();

    const quoteGroup: QuoteGroup = { id, name };
    console.log('Updating quote group:', quoteGroup);

    const url = `${baseUrl}/api/groups/${quoteGroup.id}`;
    const res = await fetchWithAuth(url, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(quoteGroup)
    });

    // wait at least 1 second, to improve UX
    await new Promise((resolve) => setTimeout(resolve, Math.max(0, 1000 - (Date.now() - startTime))));

    if (!res.ok) throw new Error('Failed to update quote group');
    return res.json();
}

export async function deleteQuoteGroup(id: number): Promise<void> {
    const startTime = Date.now();

    const url = `${baseUrl}/api/groups/${id}`;
    const res = await fetchWithAuth(url, {
        method: 'DELETE'
    });

    // wait at least 1 second, to improve UX
    await new Promise((resolve) => setTimeout(resolve, Math.max(0, 1000 - (Date.now() - startTime))));

    if (!res.ok) throw new Error('Failed to delete quote group');
    return;
}

export async function assignQuoteToGroup(quoteId: number, groupId: number): Promise<void> {
    const startTime = Date.now();

    const url = `${baseUrl}/api/groups/${groupId}/${quoteId}`;
    const res = await fetchWithAuth(url, {
        method: 'POST'
    });

    // wait at least 1 second, to improve UX
    await new Promise((resolve) => setTimeout(resolve, Math.max(0, 1000 - (Date.now() - startTime))));

    if (!res.ok) throw new Error('Failed to assign quote to group');
    return;
}

export async function removeQuoteFromGroup(quoteId: number, groupId: number): Promise<void> {
    const startTime = Date.now();

    const url = `${baseUrl}/api/groups/${groupId}/${quoteId}`;
    const res = await fetchWithAuth(url, {
        method: 'DELETE'
    });

    // wait at least 1 second, to improve UX
    await new Promise((resolve) => setTimeout(resolve, Math.max(0, 1000 - (Date.now() - startTime))));

    if (!res.ok) throw new Error('Failed to remove quote from group');
    return;
}
