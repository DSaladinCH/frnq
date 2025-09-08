import type { InvestmentModel } from '$lib/services/investmentService';
import type { PositionSnapshot } from '$lib/services/positionService';
import type { QuoteModel } from '$lib/Models/QuoteModel';
import { getInvestments } from '$lib/services/investmentService';
import { getPositionSnapshots } from '$lib/services/positionService';

// Simple reactive data store without using runes in TS file
export class DataStore {
	private _snapshots: PositionSnapshot[] = [];
	private _quotes: QuoteModel[] = [];
	private _investments: InvestmentModel[] = [];
	private _loading = true;
	private _error: string | null = null;
	private _initialized = false;
	private _listeners = new Set<() => void>();

	// Getters
	get snapshots() { return this._snapshots; }
	get quotes() { return this._quotes; }
	get investments() { return this._investments; }
	get loading() { return this._loading; }
	get error() { return this._error; }
	get initialized() { return this._initialized; }

	// Subscribe to changes (for reactivity)
	subscribe(listener: () => void) {
		this._listeners.add(listener);
		return () => this._listeners.delete(listener);
	}

	private notify() {
		this._listeners.forEach(listener => listener());
	}

	async initialize() {
		if (this._initialized) return;
		
		this._loading = true;
		this._error = null;
		this.notify();
		
		try {
			const [positionsData, investmentsData] = await Promise.all([
				getPositionSnapshots(null, null),
				getInvestments()
			]);
			
			this._snapshots = positionsData.snapshots;
			this._quotes = positionsData.quotes;
			this._investments = investmentsData;
			this._initialized = true;
			this._error = null;
		} catch (e) {
			this._error = (e as Error).message;
		} finally {
			this._loading = false;
			this.notify();
		}
	}

	async refreshData() {
		this._loading = true;
		this._error = null;
		this.notify();
		
		try {
			const [positionsData, investmentsData] = await Promise.all([
				getPositionSnapshots(null, null),
				getInvestments()
			]);
			
			this._snapshots = positionsData.snapshots;
			this._quotes = positionsData.quotes;
			this._investments = investmentsData;
			this._error = null;
		} catch (e) {
			this._error = (e as Error).message;
		} finally {
			this._loading = false;
			this.notify();
		}
	}

	// Method to add new investment and refresh data
	async addInvestment(investment: Omit<InvestmentModel, 'id'>) {
		// This would be implemented when you have the API endpoint
		// After adding, refresh the data
		await this.refreshData();
	}
}

// Export a singleton instance
export const dataStore = new DataStore();
