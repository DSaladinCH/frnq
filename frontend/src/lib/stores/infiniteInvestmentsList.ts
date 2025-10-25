import { getInvestments, type InvestmentModel, type PaginatedInvestmentsResponse, type InvestmentFilters } from '$lib/services/investmentService';

export class InfiniteInvestmentsList {
	private _items: InvestmentModel[] = [];
	private _totalCount = 0;
	private _loading = false;
	private _hasMore = true;
	private _pageSize = 25;
	private _filters: InvestmentFilters | undefined;
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

	get filters() {
		return this._filters;
	}

	subscribe(listener: () => void) {
		this._listeners.add(listener);
		return () => this._listeners.delete(listener);
	}

	private notify() {
		this._listeners.forEach((listener) => listener());
	}

	async initialize(pageSize: number = 25, filters?: InvestmentFilters) {
		this._pageSize = pageSize;
		this._filters = filters;
		this._items = [];
		this._totalCount = 0;
		this._hasMore = true;
		await this.loadMore();
	}

	async updateFilters(filters?: InvestmentFilters) {
		this._filters = filters;
		this._items = [];
		this._totalCount = 0;
		this._hasMore = true;
		await this.loadMore();
	}

	async loadMore() {
		if (this._loading || !this._hasMore) {
			return;
		}

		this._loading = true;
		this.notify();

		try {
			const response: PaginatedInvestmentsResponse = await getInvestments(
				this._items.length,
				this._pageSize,
				this._filters
			);

			this._items = [...this._items, ...response.items];
			this._totalCount = response.totalCount;
			this._hasMore = this._items.length < response.totalCount;
		} catch (error) {
			console.error('Error loading investments:', error);
			throw error;
		} finally {
			this._loading = false;
			this.notify();
		}
	}

	async refresh() {
		this._items = [];
		this._totalCount = 0;
		this._hasMore = true;
		await this.loadMore();
	}

	// Update an investment in the list
	updateItem(updatedInvestment: InvestmentModel) {
		const index = this._items.findIndex((item) => item.id === updatedInvestment.id);
		if (index !== -1) {
			this._items[index] = updatedInvestment;
			this.notify();
		}
	}

	// Add a new investment to the beginning of the list
	addItem(newInvestment: InvestmentModel) {
		this._items = [newInvestment, ...this._items];
		this._totalCount++;
		this.notify();
	}

	// Remove an investment from the list
	removeItem(investmentId: number) {
		this._items = this._items.filter((item) => item.id !== investmentId);
		this._totalCount--;
		this.notify();
	}
}
