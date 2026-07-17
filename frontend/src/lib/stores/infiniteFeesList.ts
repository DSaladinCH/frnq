import { getFees, type GeneralFeeModel, type PaginatedFeesResponse } from '$lib/services/feeService';

export class InfiniteFeesList {
	private _items: GeneralFeeModel[] = [];
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
			const response: PaginatedFeesResponse = await getFees(this._items.length, this._pageSize);

			this._items = [...this._items, ...response.items];
			this._totalCount = response.totalCount;
			this._hasMore = this._items.length < response.totalCount;
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

	async refresh() {
		this.reset();
		await this.loadMore();
	}
}

export const infiniteFeesList = new InfiniteFeesList();
