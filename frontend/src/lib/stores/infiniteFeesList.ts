import { fetchWithAuth } from '$lib/services/authService';
import type { GeneralFeeViewDto } from '$lib/services/positionService';

export interface PaginatedFeesResponse {
	items: GeneralFeeViewDto[];
	totalCount: number;
}

export class InfiniteFeesList {
	private _items: GeneralFeeViewDto[] = [];
	private _totalCount = 0;
	private _loading = false;
	private _hasMore = true;
	private _pageSize = 25;
	private _listeners = new Set<() => void>();

	get items() {
		return this._items;
	}

	get totalCount() {
		return this._totalCount;
	}

	get loading() {
		return this._loading;
	}

	get hasMore() {
		return this._hasMore;
	}

	subscribe(listener: () => void) {
		this._listeners.add(listener);
		return () => this._listeners.delete(listener);
	}

	private notify() {
		this._listeners.forEach((listener) => listener());
	}

	async initialize(pageSize: number = 25) {
		this._pageSize = pageSize;
		this._items = [];
		this._totalCount = 0;
		this._hasMore = true;
		this._loading = false;
		await this.loadMore();
	}

	async loadMore() {
		if (this._loading || !this._hasMore) {
			return;
		}

		this._loading = true;
		this.notify();

		try {
			const skip = this._items.length;
			const url = `/api/general-fees?skip=${skip}&take=${this._pageSize}`;
			const response = await fetchWithAuth(url);

			if (!response.ok) throw new Error('Failed to load fees');

			const data = await response.json();
			
			// Support both direct array and paginated response
			const items = Array.isArray(data) ? data : data.items || [];
			const totalCount = Array.isArray(data) ? data.length : data.totalCount || 0;

			this._items = [...this._items, ...items];
			this._totalCount = totalCount;
			this._hasMore = this._items.length < totalCount;
		} catch (error) {
			console.error('Error loading fees:', error);
			throw error;
		} finally {
			this._loading = false;
			this.notify();
		}
	}

	reset() {
		this._items = [];
		this._totalCount = 0;
		this._hasMore = true;
		this._loading = false;
		this.notify();
	}
}

export const infiniteFeesList = new InfiniteFeesList();
