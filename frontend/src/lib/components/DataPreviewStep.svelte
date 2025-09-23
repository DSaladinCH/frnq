<script lang="ts">
	import { ColorStyle } from "$lib/types/ColorStyle";
	import { TextSize } from "$lib/types/TextSize";
	import Button from "./Button.svelte";

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

<div class="max-w-5xl mx-auto flex flex-col gap-8">
	<div class="text-center">
		<h2 class="text-2xl font-semibold color-default mb-2">Preview Import Data</h2>
		<p class="color-muted leading-relaxed m-0">
			Review the processed data before importing. Check the validation results and ensure 
			all data looks correct.
		</p>
	</div>

	{#if isProcessing}
		<div class="text-center py-12 bg-card rounded-xl">
			<div class="text-5xl color-primary mb-4">
				<i class="fa-solid fa-spinner fa-spin"></i>
			</div>
			<h3 class="text-xl color-default mb-2">Processing Data...</h3>
			<p class="color-muted m-0">Validating and formatting your investment data</p>
		</div>
	{:else}
		<div class="bg-card rounded-xl p-6 flex flex-col gap-4">
			<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
				<div class="flex items-center gap-4 p-4 rounded-lg border border-button" style="background: color-mix(in srgb, var(--color-success), transparent 92%); border-color: color-mix(in srgb, var(--color-success), transparent 70%);">
					<div class="text-2xl color-success">
						<i class="fa-solid fa-check-circle"></i>
					</div>
					<div class="flex flex-col">
						<div class="text-2xl font-bold color-default">{validationResult.validRows.length}</div>
						<div class="text-sm color-muted">Valid Records</div>
					</div>
				</div>

				<div class="flex items-center gap-4 p-4 rounded-lg border border-button" style="background: color-mix(in srgb, var(--color-{validationResult.invalidRows.length === 0 ? 'success' : 'error'}), transparent 92%); border-color: color-mix(in srgb, var(--color-{validationResult.invalidRows.length === 0 ? 'success' : 'error'}), transparent 70%);">
					<div class="text-2xl {validationResult.invalidRows.length === 0 ? 'color-success' : 'color-error'}">
						<i class="fa-solid fa-exclamation-triangle"></i>
					</div>
					<div class="flex flex-col">
						<div class="text-2xl font-bold color-default">{validationResult.invalidRows.length}</div>
						<div class="text-sm color-muted">Invalid Records</div>
					</div>
				</div>

				<div class="flex items-center gap-4 p-4 rounded-lg border border-button" style="background: color-mix(in srgb, var(--color-{validationResult.invalidRows.length === 0 ? 'success' : 'primary'}), transparent 92%); border-color: color-mix(in srgb, var(--color-{validationResult.invalidRows.length === 0 ? 'success' : 'primary'}), transparent 70%);">
					<div class="text-2xl {validationResult.invalidRows.length === 0 ? 'color-success' : 'color-primary'}">
						<i class="fa-solid fa-file-csv"></i>
					</div>
					<div class="flex flex-col">
						<div class="text-2xl font-bold color-default">{validationResult.totalRows}</div>
						<div class="text-sm color-muted">Total Records</div>
					</div>
				</div>
			</div>

			{#if validationResult.invalidRows.length > 0}
				<div>
					<h4 class="text-lg font-semibold color-default mb-3">Validation Issues Found:</h4>
					<div class="flex flex-wrap gap-2">
						{#each Object.entries(validationResult.validationErrors) as [error, count]}
							<span class="px-3 py-1 rounded-xl text-xs border" style="background: color-mix(in srgb, var(--color-error), transparent 85%); color: var(--color-error); border-color: color-mix(in srgb, var(--color-error), transparent 70%);">
								{error} ({count})
							</span>
						{/each}
					</div>
				</div>
			{/if}
		</div>

		{#if validationResult.invalidRows.length > 0}
			<div class="bg-card rounded-xl p-6">
				<h3 class="text-xl font-semibold color-default mb-4">Invalid Records</h3>
				<div class="overflow-x-auto rounded-lg border border-button">
					<table class="w-full border-collapse bg-background">
						<thead>
							<tr>
								<th class="bg-card color-default font-semibold px-2 py-3 text-left border-b border-button text-sm">Symbol</th>
								<th class="bg-card color-default font-semibold px-2 py-3 text-left border-b border-button text-sm">Type</th>
								<th class="bg-card color-default font-semibold px-2 py-3 text-left border-b border-button text-sm">Date/Time</th>
								<th class="bg-card color-default font-semibold px-2 py-3 text-left border-b border-button text-sm">Amount</th>
								<th class="bg-card color-default font-semibold px-2 py-3 text-left border-b border-button text-sm">Unit Price</th>
								<th class="bg-card color-default font-semibold px-2 py-3 text-left border-b border-button text-sm">Fee</th>
								<th class="bg-card color-default font-semibold px-2 py-3 text-left border-b border-button text-sm">Errors</th>
							</tr>
						</thead>
						<tbody>
							{#each validationResult.invalidRows.slice(0, 10) as row}
								<tr>
									<td class="px-2 py-3 text-sm color-muted" style="border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);">{row.symbol || '-'}</td>
									<td class="px-2 py-3 text-sm color-muted" style="border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);">{row.type || '-'}</td>
									<td class="px-2 py-3 text-sm color-muted" style="border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);">{row.datetime ? formatDate(row.datetime) : '-'}</td>
									<td class="px-2 py-3 text-sm color-muted" style="border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);">{row.amount > 0 ? row.amount : '-'}</td>
									<td class="px-2 py-3 text-sm color-muted" style="border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);">{row.unitPrice > 0 ? formatCurrency(row.unitPrice) : '-'}</td>
									<td class="px-2 py-3 text-sm color-muted" style="border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);">{row.feePrice > 0 ? formatCurrency(row.feePrice) : '-'}</td>
									<td class="px-2 py-3 text-sm" style="border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);">
										<div class="flex flex-col gap-1">
											{#each row.errors as error}
												<span class="px-2 py-1 rounded text-xs" style="background: color-mix(in srgb, var(--color-error), transparent 85%); color: var(--color-error);">{error}</span>
											{/each}
										</div>
									</td>
								</tr>
							{/each}
						</tbody>
					</table>
					{#if validationResult.invalidRows.length > 10}
						<div class="px-3 py-3 text-center color-muted text-sm bg-card border-t border-button">
							Showing first 10 of {validationResult.invalidRows.length} invalid records
						</div>
					{/if}
				</div>
			</div>
		{/if}

		{#if validationResult.validRows.length > 0}
			<div class="bg-card rounded-xl p-6">
				<h3 class="text-xl font-semibold color-default mb-4">Valid Records Preview</h3>
				<div class="overflow-x-auto rounded-lg border border-button">
					<table class="w-full border-collapse bg-background">
						<thead>
							<tr>
								<th class="bg-card color-default font-semibold px-2 py-3 text-left border-b border-button text-sm">Symbol</th>
								<th class="bg-card color-default font-semibold px-2 py-3 text-left border-b border-button text-sm">Type</th>
								<th class="bg-card color-default font-semibold px-2 py-3 text-left border-b border-button text-sm">Date/Time</th>
								<th class="bg-card color-default font-semibold px-2 py-3 text-right border-b border-button text-sm">Amount</th>
								<th class="bg-card color-default font-semibold px-2 py-3 text-right border-b border-button text-sm">Unit Price</th>
								<th class="bg-card color-default font-semibold px-2 py-3 text-right border-b border-button text-sm">Fee</th>
								<th class="bg-card color-default font-semibold px-2 py-3 text-right border-b border-button text-sm">Total Value</th>
							</tr>
						</thead>
						<tbody>
							{#each validationResult.validRows.slice(0, 10) as row}
								<tr>
									<td class="px-2 py-3 text-sm color-default font-mono font-semibold" style="border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);">{row.symbol}</td>
									<td class="px-2 py-3 text-sm" style="border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);">
										<span class="px-2 py-1 rounded text-xs font-semibold uppercase {row.type.toLowerCase() === 'buy' ? 'color-success' : row.type.toLowerCase() === 'sell' ? 'color-error' : 'color-primary'}" style="background: color-mix(in srgb, var(--color-{row.type.toLowerCase() === 'buy' ? 'success' : row.type.toLowerCase() === 'sell' ? 'error' : 'primary'}), transparent 85%);">
											{row.type}
										</span>
									</td>
									<td class="px-2 py-3 text-sm color-default" style="border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);">{formatDate(row.datetime)}</td>
									<td class="px-2 py-3 text-sm color-default text-right font-mono" style="border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);">{row.amount}</td>
									<td class="px-2 py-3 text-sm color-default text-right font-mono" style="border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);">{formatCurrency(row.unitPrice)}</td>
									<td class="px-2 py-3 text-sm color-default text-right font-mono" style="border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);">{row.feePrice > 0 ? formatCurrency(row.feePrice) : '-'}</td>
									<td class="px-2 py-3 text-sm color-default text-right font-mono font-semibold" style="border-bottom: 1px solid color-mix(in srgb, var(--color-button), transparent 50%);">{formatCurrency(row.amount * row.unitPrice)}</td>
								</tr>
							{/each}
						</tbody>
					</table>
					{#if validationResult.validRows.length > 10}
						<div class="px-3 py-3 text-center color-muted text-sm bg-card border-t border-button">
							Showing first 10 of {validationResult.validRows.length} valid records
						</div>
					{/if}
				</div>
			</div>
		{/if}
	{/if}

	<div class="flex items-center justify-between gap-4 h-13">
		<div class="h-[stretch]">
			<Button
				text="Back"
				icon="fa-solid fa-arrow-left"
				onclick={handleBack}
				textSize={TextSize.Large}
				style={ColorStyle.Secondary}
			/>
		</div>

		<div class="h-[stretch]">
			<Button
				text="Import {validationResult.validRows.length} Records"
				icon="fa-solid fa-download"
				onclick={handleImport}
				disabled={isProcessing || validationResult.validRows.length === 0}
				textSize={TextSize.Large}
				style={ColorStyle.Primary}
			/>
		</div>
	</div>
</div>

<style>
	/* Responsive design for tables */
	@media (max-width: 768px) {
		th, td {
			padding: 0.5rem 0.25rem !important;
			font-size: 0.8rem !important;
		}
	}
</style>