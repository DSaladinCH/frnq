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
	private _primaryLoading = true;
	private _secondaryLoading = false;
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
		return this._primaryLoading || this._secondaryLoading;
	}
	get primaryLoading() {
		return this._primaryLoading;
	}
	get secondaryLoading() {
		return this._secondaryLoading;
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

	private async fetchAllData() {
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
		this.notify();
	}

	private async runWithSecondaryLoading(action: () => Promise<void>) {
		const wasLoading = this._secondaryLoading;
		if (!wasLoading) {
			this._secondaryLoading = true;
			this._error = null;
			this.notify();
		}

		try {
			await action();
		} catch (e) {
			this._error = (e as Error).message;
			throw e;
		} finally {
			if (!wasLoading) {
				this._secondaryLoading = false;
				this.notify();
			}
		}
	}

	async initialize() {
		if (this._initialized) return;

		this._primaryLoading = true;
		this._error = null;
		this.notify();

		try {
			await this.fetchAllData();
			this._initialized = true;
		} catch (e) {
			console.log('DataStore: API calls failed:', e);
			this._error = (e as Error).message;
		} finally {
			this._primaryLoading = false;
			this.notify();
		}
	}

	async refreshData() {
		try {
			await this.runWithSecondaryLoading(() => this.fetchAllData());
		} catch (e) {
			console.log('DataStore: refreshData failed:', e);
		}
	}

	// Method to add new investment and refresh data
	async addInvestment(investment: Omit<InvestmentModel, 'id'>) {
		await this.runWithSecondaryLoading(async () => {
			const { addInvestment: addInvestmentAPI } = await import('$lib/services/investmentService');
			await addInvestmentAPI(investment as any);
			await this.fetchAllData();
		});
	}

	// Method to update investment and refresh data
	async updateInvestment(investment: InvestmentModel) {
		await this.runWithSecondaryLoading(async () => {
			const { updateInvestment: updateInvestmentAPI } = await import(
				'$lib/services/investmentService'
			);
			await updateInvestmentAPI(investment);
			await this.fetchAllData();
		});
	}

	async deleteInvestment(investmentId: number) {
		await this.runWithSecondaryLoading(async () => {
			const { deleteInvestment: deleteInvestmentAPI } = await import(
				'$lib/services/investmentService'
			);
			await deleteInvestmentAPI(investmentId);
			await this.fetchAllData();
		});
	}

	async addQuoteGroup(name: string) {
		await this.runWithSecondaryLoading(async () => {
			const { createQuoteGroup: addQuoteGroupAPI } = await import('$lib/services/groupService');
			await addQuoteGroupAPI(name);
			await this.fetchAllData();
		});
	}

	async updateQuoteGroup(groupId: number, name: string) {
		await this.runWithSecondaryLoading(async () => {
			const { updateQuoteGroup: updateQuoteGroupAPI } = await import('$lib/services/groupService');
			await updateQuoteGroupAPI(groupId, name);
			await this.fetchAllData();
		});
	}

	async deleteQuoteGroup(groupId: number) {
		await this.runWithSecondaryLoading(async () => {
			const { deleteQuoteGroup: deleteQuoteGroupAPI } = await import('$lib/services/groupService');
			await deleteQuoteGroupAPI(groupId);
			await this.fetchAllData();
		});
	}

	async assignQuoteToGroup(quote: QuoteModel, groupId: number) {
		await this.runWithSecondaryLoading(async () => {
			const {
				assignQuoteToGroup: assignQuoteToGroupAPI,
				removeQuoteFromGroup: removeQuoteFromGroupAPI
			} = await import('$lib/services/groupService');

			if (groupId === 0) {
				await removeQuoteFromGroupAPI(quote.id, quote.group?.id);
			} else {
				await assignQuoteToGroupAPI(quote.id, groupId);
			}
			await this.fetchAllData();
		});
	}

	async updateQuoteCustomName(quoteId: number, customName: string) {
		await this.runWithSecondaryLoading(async () => {
			const { updateQuoteCustomName: updateCustomQuoteNameAPI } = await import(
				'$lib/services/quoteService'
			);
			await updateCustomQuoteNameAPI(quoteId, customName);
			await this.fetchAllData();
		});
	}

	async removeQuoteCustomName(quoteId: number) {
		await this.runWithSecondaryLoading(async () => {
			const { removeQuoteCustomName: removeCustomQuoteNameAPI } = await import(
				'$lib/services/quoteService'
			);
			await removeCustomQuoteNameAPI(quoteId);
			await this.fetchAllData();
		});
	}
}

// Export a singleton instance
export const dataStore = new DataStore();
