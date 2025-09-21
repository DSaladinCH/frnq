// This file wraps the stores to be compatible with Svelte 5 runes
import { 
	snapshots as snapshotsStore,
	quotes as quotesStore,
	investments as investmentsStore,
	groups as groupsStore,
	loading as loadingStore,
	error as errorStore,
	initialized as initializedStore,
	initializeData,
	refreshData,
	addInvestment,
	updateInvestment,
	deleteInvestment,
	addQuoteGroup,
	updateQuoteGroup,
	deleteQuoteGroup
} from './dataStore';

// Convert stores to runes-compatible reactive values
export const dataStore = {
	get snapshots() {
		return $state.snapshot(snapshotsStore);
	},
	get quotes() {
		return $state.snapshot(quotesStore);
	},
	get investments() {
		return $state.snapshot(investmentsStore);
	},
	get groups() {
		return $state.snapshot(groupsStore);
	},
	get loading() {
		return $state.snapshot(loadingStore);
	},
	get error() {
		return $state.snapshot(errorStore);
	},
	get initialized() {
		return $state.snapshot(initializedStore);
	},
	initialize: initializeData,
	refreshData,
	addInvestment,
	updateInvestment,
	deleteInvestment,
	addQuoteGroup,
	updateQuoteGroup,
	deleteQuoteGroup
};
