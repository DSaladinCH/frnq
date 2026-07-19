import { fetchWithAuth } from './authService';

// Helper to create a default GeneralFeeModel
export function createDefaultFee(): GeneralFeeModel {
	return {
		id: 0,
		userId: '',
		date: new Date().toISOString().slice(0, 10),
		amount: 0,
		description: '',
		groupId: null,
		createdAt: new Date().toISOString()
	};
}

export interface GeneralFeeModel {
	id: number;
	userId: string;
	date: string;
	amount: number;
	description: string;
	groupId: number | null;
	createdAt: string;
}

export interface GroupFeesSummary {
	groupId: number | null;
	groupName: string | null;
	totalGeneralFees: number;
	feeDetails: GeneralFeeModel[];
}

export interface PaginatedFeesResponse {
	items: GeneralFeeModel[];
	totalCount: number;
}

type FeeRequest = Omit<GeneralFeeModel, 'id' | 'userId' | 'createdAt'>;

export interface FeeFilters {
	groupId?: number;
}

export function feeValuesValid(fee: GeneralFeeModel): boolean {
	const hasValidAmount = fee.amount > 0;
	const hasValidDate = fee.date !== undefined && fee.date !== '';
	const hasValidDescription = fee.description !== undefined && fee.description.trim() !== '';

	return hasValidAmount && hasValidDate && hasValidDescription;
}

export async function getFees(
	skip: number = 0,
	take: number = 25,
	filters?: FeeFilters
): Promise<PaginatedFeesResponse> {
	let url = `/api/general-fees?skip=${skip}&take=${take}`;

	if (filters?.groupId !== undefined) url += `&groupId=${filters.groupId}`;

	const res = await fetchWithAuth(url);

	if (!res.ok) throw new Error('Failed to fetch fees');
	return res.json();
}

export async function addFee(fee: Omit<GeneralFeeModel, 'id' | 'userId' | 'createdAt'>): Promise<GeneralFeeModel> {
	const request: FeeRequest = { ...fee };

	const res = await fetchWithAuth('/api/general-fees', {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(request)
	});

	if (!res.ok) throw new Error('Failed to add fee');
	return res.json();
}

export async function updateFee(fee: GeneralFeeModel): Promise<GeneralFeeModel> {
	const request: FeeRequest = {
		date: fee.date,
		amount: fee.amount,
		description: fee.description,
		groupId: fee.groupId
	};

	const res = await fetchWithAuth(`/api/general-fees/${fee.id}`, {
		method: 'PUT',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(request)
	});

	if (!res.ok) throw new Error('Failed to update fee');
	return res.json();
}

export async function deleteFee(feeId: number): Promise<void> {
	const res = await fetchWithAuth(`/api/general-fees/${feeId}`, {
		method: 'DELETE'
	});

	if (!res.ok) throw new Error('Failed to delete fee');
	return;
}
