import type { InvestmentModel, PaginatedInvestmentsResponse } from '$lib/services/investmentService';
import type { GeneralFeeModel, PaginatedFeesResponse, GroupFeesSummary } from '$lib/services/feeService';
import type { PositionSnapshot } from '$lib/services/positionService';
import type { QuoteModel } from '$lib/Models/QuoteModel';
import { getInvestments } from '$lib/services/investmentService';
import { getFees } from '$lib/services/feeService';
import { getPositionSnapshots } from '$lib/services/positionService';
import type { QuoteGroup } from '$lib/Models/QuoteGroup';
import { getQuoteGroups } from '$lib/services/groupService';
import { infiniteFeesList } from './infiniteFeesList';
import { getForecast, type ForecastDayDto } from '$lib/services/forecastService';

// Simple reactive data store without using runes in TS file
export class DataStore {
	private _snapshots: PositionSnapshot[] = [];
	private _quotes: QuoteModel[] = [];
	private _investments: InvestmentModel[] = [];
	private _investmentsTotalCount = 0;
	private _fees: GeneralFeeModel[] = [];
	private _feesTotalCount = 0;
	private _groups: QuoteGroup[] = [];
	private _groupFeesSummaries: GroupFeesSummary[] = [];
	private _overallFees = 0;
	private _forecast: ForecastDayDto[] = [];
	private _primaryLoading = true;
	private _secondaryLoading = false;
	private _fetchLoading = false;
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
	get investmentsTotalCount() {
		return this._investmentsTotalCount;
	}
	get fees() {
		return this._fees;
	}
	get feesTotalCount() {
		return this._feesTotalCount;
	}
	get groups() {
		return this._groups;
	}
	get groupFeesSummaries() {
		return this._groupFeesSummaries;
	}
	get overallFees() {
		return this._overallFees;
	}
	get forecast() {
		return this._forecast;
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
	get fetchLoading() {
		return this._fetchLoading;
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

	getSavedForecastType(): boolean {
		try {
			const saved = localStorage.getItem('forecastChart.selectedType');
			return saved === 'predict';
		} catch {
			return false; // Default to false if localStorage is unavailable
		}
	}

	getSavedForecastDuration(): number {
		try {
			const saved = localStorage.getItem('forecastChart.selectedDuration');
			return parseInt(saved || '1', 10); // Default to 1 if not set
		} catch {
			return 1; // Default to 1 if localStorage is unavailable
		}
	}

	private async fetchAllData(includeContributions: boolean = false, years: number = 1) {
		const [positionsData, investmentsData, feesData, groupsData, forecastData] = await Promise.all([
			getPositionSnapshots(null, null),
			getInvestments(0, 25), // Get all investments for initial load
			getFees(0, 25), // Get all fees for initial load
			getQuoteGroups(),
			getForecast(includeContributions, years)
		]);

		this._snapshots = positionsData.snapshots;
		this._quotes = positionsData.quotes;
		this._groupFeesSummaries = positionsData.groupFeesSummaries || [];
		this._overallFees = positionsData.overallFees || 0;
		this._investments = investmentsData.items;
		this._investmentsTotalCount = investmentsData.totalCount;
		this._fees = feesData.items;
		this._feesTotalCount = feesData.totalCount;
		this._groups = groupsData;
		this._forecast = forecastData;
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

	// Runs a data refresh in the background without blocking the caller. Tracked via
	// `fetchLoading` instead of `secondaryLoading` so mutating actions (add/update/delete)
	// can resolve as soon as the write itself completes.
	private async runFetchInBackground(action: () => Promise<void>) {
		this._fetchLoading = true;
		this.notify();

		void action()
			.then(() => {
				this._error = null;
			})
			.catch((e) => {
				this._error = (e as Error).message;
			})
			.finally(() => {
				this._fetchLoading = false;
				this.notify();
			});
	}

	private fetchAllDataInBackground() {
		this.runFetchInBackground(() => this.fetchAllData());
	}

	async initialize() {
		if (this._initialized) return;

		this._primaryLoading = true;
		this._error = null;
		this.notify();

		try {
			const includeContributions = this.getSavedForecastType();
			const forecastDuration = this.getSavedForecastDuration();
			await this.fetchAllData(includeContributions, forecastDuration);
			this._initialized = true;
		} catch (e) {
			this._error = (e as Error).message;
		} finally {
			this._primaryLoading = false;
			this.notify();
		}
	}

	// Pass `background: true` to refresh data without blocking the caller (tracked via
	// `fetchLoading`) instead of blocking via `secondaryLoading`.
	async refreshData(background: boolean = false) {
		if (background) {
			this.fetchAllDataInBackground();
			return;
		}

		try {
			await this.runWithSecondaryLoading(() => this.fetchAllData());
		} catch (e) {}
	}

	// Method to load more investments for infinite scrolling
	async loadMoreInvestments(skip: number, take: number = 25): Promise<PaginatedInvestmentsResponse> {
		const investmentsData = await getInvestments(skip, take);
		return investmentsData;
	}

	// Method to add new investment and refresh data in the background
	async addInvestment(investment: Omit<InvestmentModel, 'id'>) {
		await this.runWithSecondaryLoading(async () => {
			const { addInvestment: addInvestmentAPI } = await import('$lib/services/investmentService');
			await addInvestmentAPI(investment as any);
		});
		this.fetchAllDataInBackground();
	}

	// Method to update investment and refresh data in the background
	async updateInvestment(investment: InvestmentModel) {
		await this.runWithSecondaryLoading(async () => {
			const { updateInvestment: updateInvestmentAPI } = await import(
				'$lib/services/investmentService'
			);
			await updateInvestmentAPI(investment);
		});
		this.fetchAllDataInBackground();
	}

	async deleteInvestment(investmentId: number) {
		await this.runWithSecondaryLoading(async () => {
			const { deleteInvestment: deleteInvestmentAPI } = await import(
				'$lib/services/investmentService'
			);
			await deleteInvestmentAPI(investmentId);
		});
		this.fetchAllDataInBackground();
	}

	async addQuoteGroup(name: string) {
		await this.runWithSecondaryLoading(async () => {
			const { createQuoteGroup: addQuoteGroupAPI } = await import('$lib/services/groupService');
			await addQuoteGroupAPI(name);
		});
		this.fetchAllDataInBackground();
	}

	async updateQuoteGroup(groupId: number, name: string) {
		await this.runWithSecondaryLoading(async () => {
			const { updateQuoteGroup: updateQuoteGroupAPI } = await import('$lib/services/groupService');
			await updateQuoteGroupAPI(groupId, name);
		});
		this.fetchAllDataInBackground();
	}

	async deleteQuoteGroup(groupId: number) {
		await this.runWithSecondaryLoading(async () => {
			const { deleteQuoteGroup: deleteQuoteGroupAPI } = await import('$lib/services/groupService');
			await deleteQuoteGroupAPI(groupId);
		});
		this.fetchAllDataInBackground();
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
		});
		this.fetchAllDataInBackground();
	}

	async updateQuoteCustomName(quoteId: number, customName: string) {
		await this.runWithSecondaryLoading(async () => {
			const { updateQuoteCustomName: updateCustomQuoteNameAPI } = await import(
				'$lib/services/quoteService'
			);
			await updateCustomQuoteNameAPI(quoteId, customName);
		});
		this.fetchAllDataInBackground();
	}

	async removeQuoteCustomName(quoteId: number) {
		await this.runWithSecondaryLoading(async () => {
			const { removeQuoteCustomName: removeCustomQuoteNameAPI } = await import(
				'$lib/services/quoteService'
			);
			await removeCustomQuoteNameAPI(quoteId);
		});
		this.fetchAllDataInBackground();
	}

	async refreshForecast() {
		await this.runFetchInBackground(async () => {
			const includeContributions = this.getSavedForecastType();
			const forecastDuration = this.getSavedForecastDuration();
			const forecastData = await getForecast(includeContributions, forecastDuration);
			this._forecast = forecastData;
		});
	}

	// Method to load more fees for infinite scrolling
	async loadMoreFees(skip: number, take: number = 25): Promise<PaginatedFeesResponse> {
		const feesData = await getFees(skip, take);
		return feesData;
	}

	// Method to add new fee and refresh data in the background
	async addFee(fee: Omit<GeneralFeeModel, 'id' | 'userId' | 'createdAt'>) {
		await this.runWithSecondaryLoading(async () => {
			const { addFee: addFeeAPI } = await import('$lib/services/feeService');
			await addFeeAPI(fee);
		});
		this.fetchAllDataInBackground();
	}

	// Method to update fee and refresh data in the background
	async updateFee(fee: GeneralFeeModel) {
		await this.runWithSecondaryLoading(async () => {
			const { updateFee: updateFeeAPI } = await import('$lib/services/feeService');
			await updateFeeAPI(fee);
		});
		this.fetchAllDataInBackground();
	}

	async deleteFee(feeId: number) {
		await this.runWithSecondaryLoading(async () => {
			const { deleteFee: deleteFeeAPI } = await import('$lib/services/feeService');
			await deleteFeeAPI(feeId);
		});
		this.fetchAllDataInBackground();
	}
}

// Export a singleton instance
export const dataStore = new DataStore();
