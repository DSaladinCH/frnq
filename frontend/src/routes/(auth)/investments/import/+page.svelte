<script lang="ts">
	import PageHead from '$lib/components/PageHead.svelte';
	import PageTitle from '$lib/components/PageTitle.svelte';
	import ImportWizard from '$lib/components/ImportWizard.svelte';
	import { addInvestmentsBulk, InvestmentType } from '$lib/services/investmentService';
	import type { InvestmentModel } from '$lib/services/investmentService';
	import { onMount } from 'svelte';

	interface ProcessedInvestment {
		symbol: string;
		type: string;
		datetime: string;
		amount: number;
		unitPrice: number;
		feePrice: number;
		isValid: boolean;
		errors: string[];
		originalRow: string[];
	}

	let { onImportInvestments }: { onImportInvestments: (data: ProcessedInvestment[]) => void } = $props();

	let isImporting = $state(false);
	let importSuccess = $state(false);
	let importedCount = $state(0);
	let importError = $state<string | null>(null);

	function convertProcessedToInvestmentModel(processed: ProcessedInvestment): InvestmentModel {
		// Convert string type to enum
		let investmentType = InvestmentType.Buy;
		switch (processed.type.toLowerCase()) {
			case 'sell':
				investmentType = InvestmentType.Sell;
				break;
			case 'dividend':
				investmentType = InvestmentType.Dividend;
				break;
			default:
				investmentType = InvestmentType.Buy;
				break;
		}

		return {
			id: 0,
			quoteId: 0,
			providerId: 'yahoo-finance', // Set default provider
			quoteSymbol: processed.symbol,
			date: processed.datetime,
			type: investmentType,
			amount: processed.amount,
			pricePerUnit: processed.unitPrice,
			totalFees: processed.feePrice
		};
	}

	async function handleImportInvestments(data: ProcessedInvestment[]) {
		isImporting = true;
		importError = null;
		
		try {
			// Filter only valid investments
			const validInvestments = data.filter(item => item.isValid);
			
			if (validInvestments.length === 0) {
				throw new Error('No valid investments to import');
			}

			// Convert ProcessedInvestment to InvestmentModel
			const investmentModels = validInvestments.map(convertProcessedToInvestmentModel);
			
			// Call the API to bulk create investments
			await addInvestmentsBulk(investmentModels);
			
			// Call the parent component's import function if it exists
			if (onImportInvestments) {
				onImportInvestments(data);
			}
			
			importedCount = validInvestments.length;
			importSuccess = true;
			
		} catch (error) {
			console.error('Import failed:', error);
			importError = error instanceof Error ? error.message : 'An unknown error occurred';
		} finally {
			isImporting = false;
		}
	}

	function startNewImport() {
		importSuccess = false;
		importedCount = 0;
		importError = null;
	}

	onMount(() => {
		// ensure callbacks exist
		if (!onImportInvestments) {
			console.warn('InvestmentImport: `onImportInvestments` prop is not provided.');
		}
	});
</script>

<PageHead title="Import Investments" />

<div class="xs:p-8 p-4">
	<PageTitle title="Import Investments" icon="fa-solid fa-file-import" />

	{#if isImporting}
		<div class="import-progress">
			<div class="progress-content">
				<div class="progress-icon">
					<i class="fa-solid fa-spinner fa-spin"></i>
				</div>
				<h2>Importing Your Data...</h2>
				<p>Please wait while we process and save your investment data.</p>
				<div class="progress-bar-loading">
					<div class="progress-fill"></div>
				</div>
			</div>
		</div>
	{:else if importError}
		<div class="import-error">
			<div class="error-content">
				<div class="error-icon">
					<i class="fa-solid fa-exclamation-triangle"></i>
				</div>
				<h2>Import Failed</h2>
				<p class="error-message">{importError}</p>
				<div class="error-actions">
					<button class="btn btn-primary btn-big" onclick={startNewImport}>
						Try Again
					</button>
				</div>
			</div>
		</div>
	{:else if importSuccess}
		<div class="import-success">
			<div class="success-content">
				<div class="success-icon">
					<i class="fa-solid fa-check-circle"></i>
				</div>
				<h2>Import Successful!</h2>
				<p>Successfully imported <strong>{importedCount}</strong> investment records.</p>
				<div class="success-actions">
					<button class="btn btn-primary btn-big" onclick={startNewImport}>
						Import More Data
					</button>
					<a href="/investments" class="btn btn-secondary btn-big">
						View Investments
					</a>
				</div>
			</div>
		</div>
	{:else}
		<ImportWizard onImportInvestments={handleImportInvestments} />
	{/if}
</div>

<style>
	.import-progress {
		display: flex;
		align-items: center;
		justify-content: center;
		min-height: 400px;
		background: var(--color-card);
		border-radius: 12px;
		margin: 2rem 0;
	}

	.import-error {
		display: flex;
		align-items: center;
		justify-content: center;
		min-height: 400px;
		background: var(--color-card);
		border-radius: 12px;
		margin: 2rem 0;
	}

	.progress-content,
	.error-content {
		text-align: center;
		max-width: 400px;
	}

	.progress-icon {
		font-size: 4rem;
		color: var(--color-primary);
		margin-bottom: 1.5rem;
	}

	.error-icon {
		font-size: 4rem;
		color: var(--color-danger);
		margin-bottom: 1.5rem;
	}

	.progress-content h2,
	.error-content h2 {
		font-size: 1.5rem;
		color: var(--color-text);
		margin: 0 0 0.5rem 0;
	}

	.progress-content p,
	.error-content p {
		color: var(--color-muted);
		margin: 0 0 2rem 0;
		line-height: 1.5;
	}

	.error-message {
		color: var(--color-danger) !important;
		font-weight: 500;
	}

	.error-actions {
		display: flex;
		gap: 1rem;
		justify-content: center;
		flex-wrap: wrap;
	}

	.progress-bar-loading {
		width: 100%;
		height: 6px;
		background: var(--color-button);
		border-radius: 3px;
		overflow: hidden;
		position: relative;
	}

	.progress-fill {
		height: 100%;
		background: linear-gradient(90deg, var(--color-primary), var(--color-secondary));
		width: 100%;
		border-radius: 3px;
		animation: progressAnimation 2s ease-in-out infinite;
	}

	@keyframes progressAnimation {
		0% {
			transform: translateX(-100%);
		}
		50% {
			transform: translateX(0%);
		}
		100% {
			transform: translateX(100%);
		}
	}

	.import-success {
		display: flex;
		align-items: center;
		justify-content: center;
		min-height: 400px;
		background: var(--color-card);
		border-radius: 12px;
		margin: 2rem 0;
	}

	.success-content {
		text-align: center;
		max-width: 500px;
	}

	.success-icon {
		font-size: 4rem;
		color: var(--color-success);
		margin-bottom: 1.5rem;
		animation: successPulse 1s ease-out;
	}

	@keyframes successPulse {
		0% {
			transform: scale(0.8);
			opacity: 0;
		}
		50% {
			transform: scale(1.1);
		}
		100% {
			transform: scale(1);
			opacity: 1;
		}
	}

	.success-content h2 {
		font-size: 1.5rem;
		color: var(--color-text);
		margin: 0 0 0.5rem 0;
	}

	.success-content p {
		color: var(--color-muted);
		margin: 0 0 2rem 0;
		line-height: 1.5;
	}

	.success-content strong {
		color: var(--color-success);
		font-weight: 600;
	}

	.success-actions {
		display: flex;
		gap: 1rem;
		justify-content: center;
		flex-wrap: wrap;
	}

	/* Responsive design */
	@media (max-width: 768px) {
		.success-actions,
		.error-actions {
			flex-direction: column;
			align-items: center;
		}

		.progress-icon,
		.success-icon,
		.error-icon {
			font-size: 3rem;
		}

		.progress-content h2,
		.success-content h2,
		.error-content h2 {
			font-size: 1.3rem;
		}
	}
</style>
