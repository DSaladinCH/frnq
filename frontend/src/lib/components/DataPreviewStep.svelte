<script lang="ts">
	interface Props {
		csvData: string[][];
		csvHeaders: string[];
		columnMappings: Record<string, string>;
		valueMappings: Record<string, Record<string, string>>;
		useFixedValue: boolean;
		fixedTypeValue: string;
		onback?: () => void;
		onimport?: (data: { validatedData: ProcessedInvestment[] }) => void;
	}

	const {
		csvData,
		csvHeaders,
		columnMappings,
		valueMappings,
		useFixedValue,
		fixedTypeValue,
		onback,
		onimport
	}: Props = $props();

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

	interface ValidationResult {
		validRows: ProcessedInvestment[];
		invalidRows: ProcessedInvestment[];
		totalRows: number;
		validationErrors: Record<string, number>;
	}

	let validationResult: ValidationResult = $state({
		validRows: [],
		invalidRows: [],
		totalRows: 0,
		validationErrors: {}
	});
	let isProcessing = $state(true);
	let showInvalidRows = $state(false);

	function validateAndProcessData() {
		isProcessing = true;
		
		// Use the provided csvData and csvHeaders
		const validRows: ProcessedInvestment[] = [];
		const invalidRows: ProcessedInvestment[] = [];
		const validationErrors: Record<string, number> = {};

		for (const row of csvData) {
			const processedRow = processRow(row, csvHeaders);
			
			if (processedRow.isValid) {
				validRows.push(processedRow);
			} else {
				invalidRows.push(processedRow);
				// Count error types
				for (const error of processedRow.errors) {
					validationErrors[error] = (validationErrors[error] || 0) + 1;
				}
			}
		}

		validationResult = {
			validRows,
			invalidRows,
			totalRows: csvData.length,
			validationErrors
		};

		isProcessing = false;
	}

	function processRow(row: string[], headers: string[]): ProcessedInvestment {
		const errors: string[] = [];
		let isValid = true;

		// Helper function to get cell value by mapped column
		function getCellValue(fieldName: string): string {
			const csvColumn = columnMappings[fieldName];
			if (!csvColumn) return '';
			
			const columnIndex = headers.indexOf(csvColumn);
			return columnIndex !== -1 ? row[columnIndex] || '' : '';
		}

		// Extract values
		const symbolValue = getCellValue('symbol');
		const typeValue = getCellValue('type');
		const datetimeValue = getCellValue('datetime');
		const amountValue = getCellValue('amount');
		const unitPriceValue = getCellValue('unitPrice');
		const feePriceValue = getCellValue('feePrice');

		// Validate Symbol
		if (!symbolValue.trim()) {
			errors.push('Missing symbol');
			isValid = false;
		}

		// Validate and map Type
		let mappedType = typeValue;
		if (valueMappings.type) {
			// Check for fixed value first
			if (valueMappings.type['__FIXED__']) {
				mappedType = valueMappings.type['__FIXED__'];
			} else if (valueMappings.type[typeValue]) {
				mappedType = valueMappings.type[typeValue];
			}
		}
		if (!mappedType.trim()) {
			errors.push('Missing or unmapped transaction type');
			isValid = false;
		}

		// Validate DateTime
		let parsedDateTime = '';
		if (!datetimeValue.trim()) {
			errors.push('Missing date/time');
			isValid = false;
		} else {
			try {
				const date = new Date(datetimeValue);
				if (isNaN(date.getTime())) {
					errors.push('Invalid date format');
					isValid = false;
				} else {
					parsedDateTime = date.toISOString();
				}
			} catch {
				errors.push('Invalid date format');
				isValid = false;
			}
		}

		// Validate Amount
		let parsedAmount = 0;
		if (!amountValue.trim()) {
			errors.push('Missing amount');
			isValid = false;
		} else {
			const amount = parseFloat(amountValue.replace(',', '.'));
			if (isNaN(amount)) {
				errors.push('Invalid amount format');
				isValid = false;
			} else {
				parsedAmount = amount;
			}
		}

		// Validate Unit Price
		let parsedUnitPrice = 0;
		if (!unitPriceValue.trim()) {
			errors.push('Missing unit price');
			isValid = false;
		} else {
			const unitPrice = parseFloat(unitPriceValue.replace(',', '.'));
			if (isNaN(unitPrice)) {
				errors.push('Invalid unit price format');
				isValid = false;
			} else {
				parsedUnitPrice = unitPrice;
			}
		}

		// Validate Fee Price (optional)
		let parsedFeePrice = 0;
		if (feePriceValue.trim()) {
			const feePrice = parseFloat(feePriceValue.replace(',', '.'));
			if (isNaN(feePrice)) {
				errors.push('Invalid fee price format');
				isValid = false;
			} else {
				parsedFeePrice = feePrice;
			}
		}

		return {
			symbol: symbolValue,
			type: mappedType,
			datetime: parsedDateTime || datetimeValue,
			amount: parsedAmount,
			unitPrice: parsedUnitPrice,
			feePrice: parsedFeePrice,
			isValid,
			errors,
			originalRow: row
		};
	}

	function formatCurrency(value: number): string {
		return new Intl.NumberFormat('en-US', {
			style: 'currency',
			currency: 'USD',
			minimumFractionDigits: 2
		}).format(value);
	}

	function formatDate(datetime: string): string {
		try {
			const date = new Date(datetime);
			return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
		} catch {
			return datetime;
		}
	}

	function handleBack() {
		onback?.();
	}

	function handleImport() {
		onimport?.({ validatedData: validationResult.validRows });
	}

	// Start validation when component mounts
	validateAndProcessData();
</script>

<div class="max-w-5xl mx-auto">
	<div class="text-center mb-8">
		<h2 class="text-2xl font-semibold color-default mb-2">Preview Import Data</h2>
		<p class="color-muted leading-relaxed m-0">
			Review the processed data before importing. Check the validation results and ensure 
			all data looks correct.
		</p>
	</div>

	{#if isProcessing}
		<div class="text-center py-12 bg-card rounded-xl mb-8">
			<div class="text-5xl color-primary mb-4">
				<i class="fa-solid fa-spinner fa-spin"></i>
			</div>
			<h3 class="text-xl color-default mb-2">Processing Data...</h3>
			<p class="color-muted m-0">Validating and formatting your investment data</p>
		</div>
	{:else}
		<div class="bg-card rounded-xl p-6 mb-8">
			<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 mb-6">
				<div class="summary-card success">
					<div class="card-icon">
						<i class="fa-solid fa-check-circle"></i>
					</div>
					<div class="card-content">
						<div class="card-number">{validationResult.validRows.length}</div>
						<div class="card-label">Valid Records</div>
					</div>
				</div>

				<div class="summary-card {validationResult.invalidRows.length === 0 ? 'success' : 'error'}">
					<div class="card-icon">
						<i class="fa-solid fa-exclamation-triangle"></i>
					</div>
					<div class="card-content">
						<div class="card-number">{validationResult.invalidRows.length}</div>
						<div class="card-label">Invalid Records</div>
					</div>
				</div>

				<div class="summary-card {validationResult.invalidRows.length === 0 ? 'success' : 'info'}">
					<div class="card-icon">
						<i class="fa-solid fa-file-csv"></i>
					</div>
					<div class="card-content">
						<div class="card-number">{validationResult.totalRows}</div>
						<div class="card-label">Total Records</div>
					</div>
				</div>
			</div>

			{#if validationResult.invalidRows.length > 0}
				<div class="error-summary">
					<h4>Validation Issues Found:</h4>
					<div class="error-types">
						{#each Object.entries(validationResult.validationErrors) as [error, count]}
							<span class="error-badge">
								{error} ({count})
							</span>
						{/each}
					</div>
					<button 
						class="btn btn-small btn-secondary"
						onclick={() => showInvalidRows = !showInvalidRows}
					>
						{showInvalidRows ? 'Hide' : 'Show'} Invalid Records
						<i class="fa-solid {showInvalidRows ? 'fa-chevron-up' : 'fa-chevron-down'} ml-1"></i>
					</button>
				</div>
			{/if}
		</div>

		{#if showInvalidRows && validationResult.invalidRows.length > 0}
			<div class="invalid-rows-section">
				<h3>Invalid Records</h3>
				<div class="table-container">
					<table class="data-table invalid">
						<thead>
							<tr>
								<th>Symbol</th>
								<th>Type</th>
								<th>Date/Time</th>
								<th>Amount</th>
								<th>Unit Price</th>
								<th>Fee</th>
								<th>Errors</th>
							</tr>
						</thead>
						<tbody>
							{#each validationResult.invalidRows.slice(0, 10) as row}
								<tr>
									<td>{row.symbol || '-'}</td>
									<td>{row.type || '-'}</td>
									<td>{row.datetime ? formatDate(row.datetime) : '-'}</td>
									<td>{row.amount > 0 ? row.amount : '-'}</td>
									<td>{row.unitPrice > 0 ? formatCurrency(row.unitPrice) : '-'}</td>
									<td>{row.feePrice > 0 ? formatCurrency(row.feePrice) : '-'}</td>
									<td>
										<div class="errors-list">
											{#each row.errors as error}
												<span class="error-item">{error}</span>
											{/each}
										</div>
									</td>
								</tr>
							{/each}
						</tbody>
					</table>
					{#if validationResult.invalidRows.length > 10}
						<div class="table-footer">
							Showing first 10 of {validationResult.invalidRows.length} invalid records
						</div>
					{/if}
				</div>
			</div>
		{/if}

		{#if validationResult.validRows.length > 0}
			<div class="valid-rows-section">
				<h3>Valid Records Preview</h3>
				<div class="table-container">
					<table class="data-table valid">
						<thead>
							<tr>
								<th>Symbol</th>
								<th>Type</th>
								<th>Date/Time</th>
								<th>Amount</th>
								<th>Unit Price</th>
								<th>Fee</th>
								<th>Total Value</th>
							</tr>
						</thead>
						<tbody>
							{#each validationResult.validRows.slice(0, 10) as row}
								<tr>
									<td class="symbol">{row.symbol}</td>
									<td>
										<span class="type-badge type-{row.type.toLowerCase()}">
											{row.type}
										</span>
									</td>
									<td>{formatDate(row.datetime)}</td>
									<td class="amount">{row.amount}</td>
									<td class="price">{formatCurrency(row.unitPrice)}</td>
									<td class="price">{row.feePrice > 0 ? formatCurrency(row.feePrice) : '-'}</td>
									<td class="price total">{formatCurrency(row.amount * row.unitPrice)}</td>
								</tr>
							{/each}
						</tbody>
					</table>
					{#if validationResult.validRows.length > 10}
						<div class="table-footer">
							Showing first 10 of {validationResult.validRows.length} valid records
						</div>
					{/if}
				</div>
			</div>
		{/if}
	{/if}

	<div class="flex justify-between items-center gap-4 mt-8">
		<button class="btn btn-secondary" onclick={handleBack}>
			<i class="fa-solid fa-arrow-left mr-2"></i>
			Back to Mapping
		</button>
		
		<button 
			class="btn btn-success btn-big" 
			onclick={handleImport}
			disabled={isProcessing || validationResult.validRows.length === 0}
		>
			<i class="fa-solid fa-download mr-2"></i>
			Import {validationResult.validRows.length} Records
		</button>
	</div>
</div>

<style>
	/* Keep complex table and card styling that's difficult to replace with Tailwind */
	.summary-card {
		display: flex;
		align-items: center;
		gap: 1rem;
		padding: 1rem;
		border-radius: 8px;
		border: 1px solid var(--color-button);
	}

	.summary-card.success {
		background: color-mix(in srgb, var(--color-success), transparent 92%);
		border-color: color-mix(in srgb, var(--color-success), transparent 70%);
	}

	.summary-card.error {
		background: color-mix(in srgb, var(--color-error), transparent 92%);
		border-color: color-mix(in srgb, var(--color-error), transparent 70%);
	}

	.summary-card.info {
		background: color-mix(in srgb, var(--color-primary), transparent 92%);
		border-color: color-mix(in srgb, var(--color-primary), transparent 70%);
	}

	.card-icon {
		font-size: 1.5rem;
	}

	.summary-card.success .card-icon {
		color: var(--color-success);
	}

	.summary-card.error .card-icon {
		color: var(--color-error);
	}

	.summary-card.info .card-icon {
		color: var(--color-primary);
	}

	.card-content {
		display: flex;
		flex-direction: column;
	}

	.card-number {
		font-size: 1.5rem;
		font-weight: 700;
		color: var(--color-text);
	}

	.card-label {
		font-size: 0.85rem;
		color: var(--color-muted);
	}

	.error-types {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem;
		margin-bottom: 1rem;
	}

	.error-badge {
		background: color-mix(in srgb, var(--color-error), transparent 85%);
		color: var(--color-error);
		padding: 0.25rem 0.75rem;
		border-radius: 12px;
		font-size: 0.8rem;
		border: 1px solid color-mix(in srgb, var(--color-error), transparent 70%);
	}

	.invalid-rows-section,
	.valid-rows-section {
		background: var(--color-card);
		border-radius: 12px;
		padding: 1.5rem;
		margin-bottom: 2rem;
	}

	.table-container {
		overflow-x: auto;
		border-radius: 8px;
		border: 1px solid var(--color-button);
	}

	.data-table {
		width: 100%;
		border-collapse: collapse;
		background: var(--color-background);
	}

	.data-table th {
		background: var(--color-card);
		color: var(--color-text);
		font-weight: 600;
		padding: 0.75rem 0.5rem;
		text-align: left;
		border-bottom: 1px solid var(--color-button);
		font-size: 0.9rem;
	}

	.data-table td {
		padding: 0.75rem 0.5rem;
		border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);
		font-size: 0.9rem;
		color: var(--color-text);
	}

	.data-table.invalid td {
		color: var(--color-muted);
	}

	.symbol {
		font-family: 'Fira Mono', 'Consolas', monospace;
		font-weight: 600;
	}

	.type-badge {
		padding: 0.25rem 0.5rem;
		border-radius: 4px;
		font-size: 0.8rem;
		font-weight: 600;
		text-transform: uppercase;
	}

	.type-buy {
		background: color-mix(in srgb, var(--color-success), transparent 85%);
		color: var(--color-success);
	}

	.type-sell {
		background: color-mix(in srgb, var(--color-error), transparent 85%);
		color: var(--color-error);
	}

	.type-dividend {
		background: color-mix(in srgb, var(--color-primary), transparent 85%);
		color: var(--color-primary);
	}

	.amount {
		text-align: right;
		font-family: 'Fira Mono', 'Consolas', monospace;
	}

	.price {
		text-align: right;
		font-family: 'Fira Mono', 'Consolas', monospace;
	}

	.price.total {
		font-weight: 600;
	}

	.errors-list {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
	}

	.error-item {
		background: color-mix(in srgb, var(--color-error), transparent 85%);
		color: var(--color-error);
		padding: 0.15rem 0.5rem;
		border-radius: 4px;
		font-size: 0.75rem;
	}

	.table-footer {
		padding: 0.75rem;
		text-align: center;
		color: var(--color-muted);
		font-size: 0.85rem;
		background: var(--color-card);
		border-top: 1px solid var(--color-button);
	}

	.mr-2 {
		margin-right: 0.5rem;
	}

	.ml-1 {
		margin-left: 0.25rem;
	}

	/* Responsive design */
	@media (max-width: 768px) {
		.data-table th,
		.data-table td {
			padding: 0.5rem 0.25rem;
			font-size: 0.8rem;
		}
	}
</style>