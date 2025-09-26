import type { InvestmentModel } from '$lib/services/investmentService';
import type { PositionSnapshot } from '$lib/services/positionService';
import type { QuoteModel } from '$lib/Models/QuoteModel';
import { getInvestments } from '$lib/services/investmentService';
import { getPositionSnapshots } from '$lib/services/positionService';
import type { QuoteGroup } from '$lib/Models/QuoteGroup';
import { getQuoteGroups } from '$lib/services/groupService';

// Simple reactive data store without using runes in TS file
export class DataStore {
	private _snapshots: PositionSnapshot[] = [];
	private _quotes: QuoteModel[] = [];
	private _investments: InvestmentModel[] = [];
	private _groups: QuoteGroup[] = [];
	private _loading = true;
	private _error: string | null = null;
	private _initialized = false;
	private _listeners = new Set<() => void>();

	// Getters
	get snapshots() {
		return this._snapshots;
	}
	get quotes() {
		return this._quotes;
	}
	get investments() {
		return this._investments;
	}
	get groups() {
		return this._groups;
	}
	get loading() {
		return this._loading;
	}
	get error() {
		return this._error;
	}
	get initialized() {
		return this._initialized;
	}

	// Subscribe to changes (for reactivity)
	subscribe(listener: () => void) {
		this._listeners.add(listener);
		return () => this._listeners.delete(listener);
	}

	private notify() {
		this._listeners.forEach((listener) => listener());
	}

	async initialize() {
		if (this._initialized) return;

		this._loading = true;
		this._error = null;
		this.notify();

		try {
			const [positionsData, investmentsData, groupsData] = await Promise.all([
				getPositionSnapshots(null, null),
				getInvestments(),
				getQuoteGroups()
			]);

			this._snapshots = positionsData.snapshots;
			this._quotes = positionsData.quotes;
			this._investments = investmentsData;
			this._groups = groupsData;
			this._initialized = true;
			this._error = null;
		} catch (e) {
			console.log('DataStore: API calls failed:', e);
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
			const [positionsData, investmentsData, groupsData] = await Promise.all([
				getPositionSnapshots(null, null),
				getInvestments(),
				getQuoteGroups()
			]);

			this._snapshots = positionsData.snapshots;
			this._quotes = positionsData.quotes;
			this._investments = investmentsData;
			this._groups = groupsData;
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
		const { addInvestment: addInvestmentAPI } = await import('$lib/services/investmentService');
		await addInvestmentAPI(investment as any); // Cast needed due to id being 0 vs omitted
		await this.refreshData();
	}

	// Method to update investment and refresh data
	async updateInvestment(investment: InvestmentModel) {
		const { updateInvestment: updateInvestmentAPI } = await import(
			'$lib/services/investmentService'
		);
		await updateInvestmentAPI(investment);
		await this.refreshData();
	}

	async deleteInvestment(investmentId: number) {
		const { deleteInvestment: deleteInvestmentAPI } = await import(
			'$lib/services/investmentService'
		);
		await deleteInvestmentAPI(investmentId);
		await this.refreshData();
	}

	async addQuoteGroup(name: string) {
		const { createQuoteGroup: addQuoteGroupAPI } = await import('$lib/services/groupService');
		await addQuoteGroupAPI(name);
		await this.refreshData();
	}

	async updateQuoteGroup(groupId: number, name: string) {
		const { updateQuoteGroup: updateQuoteGroupAPI } = await import('$lib/services/groupService');
		await updateQuoteGroupAPI(groupId, name);
		await this.refreshData();
	}

	async deleteQuoteGroup(groupId: number) {
		const { deleteQuoteGroup: deleteQuoteGroupAPI } = await import('$lib/services/groupService');
		await deleteQuoteGroupAPI(groupId);
		await this.refreshData();
	}

	async assignQuoteToGroup(quote: QuoteModel, groupId: number) {
		const {
			assignQuoteToGroup: assignQuoteToGroupAPI,
			removeQuoteFromGroup: removeQuoteFromGroupAPI
		} = await import('$lib/services/groupService');

		if (groupId === 0) {
			await removeQuoteFromGroupAPI(quote.id, quote.group?.id);
		} else {
			await assignQuoteToGroupAPI(quote.id, groupId);
		}
		await this.refreshData();
	}
}

// Export a singleton instance
export const dataStore = new DataStore();
