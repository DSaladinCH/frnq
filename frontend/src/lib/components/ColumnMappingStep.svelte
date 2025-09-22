<script lang="ts">
	import Modal from './Modal.svelte';
	import CustomDropdown from './CustomDropdown.svelte';

	interface Props {
		csvHeaders: string[];
		sampleData: string[][];
		filename?: string;
		initialColumnMappings?: Record<string, string>;
		initialValueMappings?: Record<string, Record<string, string>>;
		initialUseFixedValue?: boolean;
		initialFixedTypeValue?: string;
		onback?: () => void;
		onnext?: () => void;
		onmappingChanged?: (data: {
			columnMappings: Record<string, string>;
			valueMappings: Record<string, Record<string, string>>;
			useFixedValue: boolean;
			fixedTypeValue: string;
		}) => void;
	}

	const {
		csvHeaders,
		sampleData,
		filename = 'uploaded-file.csv',
		initialColumnMappings = {},
		initialValueMappings = {},
		initialUseFixedValue = false,
		initialFixedTypeValue = '',
		onback,
		onnext,
		onmappingChanged
	}: Props = $props();

	interface ColumnMapping {
		csvColumn: string;
		targetField: string;
		sampleValues: string[];
	}

	interface ValueMapping {
		originalValue: string;
		mappedValue: string;
	}

	// Required fields for investment data
	const requiredFields = [
		{ value: 'symbol', label: 'Symbol', required: true },
		{ value: 'type', label: 'Transaction Type', required: true },
		{ value: 'datetime', label: 'Date & Time', required: true },
		{ value: 'amount', label: 'Amount/Shares', required: true },
		{ value: 'unitPrice', label: 'Unit Price', required: true },
		{ value: 'feePrice', label: 'Fee/Commission', required: false }
	];

	// Common transaction types for mapping
	const transactionTypes = [
		{ value: 'BUY', label: 'Buy' },
		{ value: 'SELL', label: 'Sell' },
		{ value: 'DIVIDEND', label: 'Dividend' },
		{ value: 'SPLIT', label: 'Stock Split' },
		{ value: 'MERGER', label: 'Merger' },
		{ value: 'SPIN_OFF', label: 'Spin-off' }
	];

	let columnMappings: Record<string, string> = $state(initialColumnMappings);
	let valueMappings: Record<string, Record<string, string>> = $state(initialValueMappings);
	let showValueMapping = $state(false);
	let selectedFieldForMapping = $state('');
	let useFixedValue = $state(initialUseFixedValue);
	let fixedTypeValue = $state(initialFixedTypeValue);

	// Initialize when headers are available
	$effect(() => {
		if (csvHeaders.length > 0) {
			initializeColumnMappings();
		}
	});

	function initializeColumnMappings() {
		// Only auto-map if no existing mappings
		if (Object.keys(columnMappings).length > 0) return;
		
		const smartMappings: Record<string, string[]> = {
			symbol: ['symbol', 'ticker', 'isin', 'instrument'],
			type: ['type', 'transaction', 'action', 'side'],
			datetime: ['date', 'datetime', 'timestamp', 'time'],
			amount: ['amount', 'shares', 'quantity', 'qty'],
			unitPrice: ['price', 'unit', 'unitprice', 'rate'],
			feePrice: ['fee', 'commission', 'cost', 'charge']
		};

		// Try to auto-map columns based on header names
		for (const [targetField, possibleNames] of Object.entries(smartMappings)) {
			for (const header of csvHeaders) {
				const normalizedHeader = header.toLowerCase().replace(/[^a-z]/g, '');
				if (possibleNames.some(name => normalizedHeader.includes(name))) {
					columnMappings[targetField] = header;
					break;
				}
			}
		}
	}

	function getSampleValues(csvColumn: string): string[] {
		const columnIndex = csvHeaders.indexOf(csvColumn);
		if (columnIndex === -1) return [];
		
		const values = sampleData
			.map(row => row[columnIndex])
			.filter(val => val && val.trim())
			.slice(0, 5);
		
		return [...new Set(values)]; // Remove duplicates
	}

	function getUniqueValues(csvColumn: string): string[] {
		const columnIndex = csvHeaders.indexOf(csvColumn);
		if (columnIndex === -1) return [];
		
		const values = sampleData
			.map(row => row[columnIndex])
			.filter(val => val && val.trim());
		
		return [...new Set(values)];
	}

	function handleColumnMappingChange(targetField: string, csvColumn: string) {
		if (csvColumn === '') {
			delete columnMappings[targetField];
		} else {
			columnMappings[targetField] = csvColumn;
		}
		
		// If this is a type field, initialize value mappings
		if (targetField === 'type' && csvColumn && !useFixedValue) {
			initializeValueMappings(csvColumn);
		}
		
		emitMappingChange();
	}

	function initializeValueMappings(csvColumn: string) {
		const uniqueValues = getUniqueValues(csvColumn);
		if (!valueMappings.type) {
			valueMappings.type = {};
		}
		
		// Try to auto-map common values
		for (const value of uniqueValues) {
			const normalizedValue = value.toLowerCase().trim();
			let mappedValue = '';
			
			if (['buy', 'purchase', 'b'].includes(normalizedValue)) {
				mappedValue = 'BUY';
			} else if (['sell', 'sale', 's'].includes(normalizedValue)) {
				mappedValue = 'SELL';
			} else if (['dividend', 'div', 'd'].includes(normalizedValue)) {
				mappedValue = 'DIVIDEND';
			} else if (normalizedValue.includes('split')) {
				mappedValue = 'SPLIT';
			}
			
			valueMappings.type[value] = mappedValue;
		}
	}

	function handleValueMappingChange(originalValue: string, mappedValue: string) {
		if (!valueMappings.type) {
			valueMappings.type = {};
		}
		valueMappings.type[originalValue] = mappedValue;
		emitMappingChange();
	}

	function handleFixedValueChange(enabled: boolean) {
		useFixedValue = enabled;
		if (enabled) {
			// Clear column mapping and set fixed value
			delete columnMappings.type;
			if (!valueMappings.type) {
				valueMappings.type = {};
			}
			valueMappings.type['__FIXED__'] = fixedTypeValue;
		} else {
			// Clear fixed value mapping
			if (valueMappings.type) {
				delete valueMappings.type['__FIXED__'];
			}
		}
		emitMappingChange();
	}

	function handleFixedTypeValueChange(value: string) {
		fixedTypeValue = value;
		if (useFixedValue) {
			if (!valueMappings.type) {
				valueMappings.type = {};
			}
			valueMappings.type['__FIXED__'] = value;
			emitMappingChange();
		}
	}

	function emitMappingChange() {
		onmappingChanged?.({
			columnMappings,
			valueMappings,
			useFixedValue,
			fixedTypeValue
		});
	}

	function showValueMappingDialog(fieldType: string) {
		if (fieldType === 'type' && (columnMappings.type || useFixedValue)) {
			selectedFieldForMapping = fieldType;
			showValueMapping = true;
		}
	}

	function isComplete(): boolean {
		const requiredFieldsSet = requiredFields.filter(f => f.required).map(f => f.value);
		// For type field, it's complete if we have either a column mapping or fixed value
		const typeComplete = Boolean(columnMappings.type) || useFixedValue;
		const otherFieldsComplete = requiredFieldsSet.filter(f => f !== 'type').every(field => Boolean(columnMappings[field]));
		return typeComplete && otherFieldsComplete;
	}

	function handleNext() {
		if (isComplete()) {
			onnext?.();
		}
	}

	function handleBack() {
		onback?.();
	}
</script>

<div class="max-w-4xl mx-auto">
	<div class="text-center mb-8">
		<h2 class="text-2xl font-semibold color-default mb-2">Map CSV Columns</h2>
		<p class="color-muted leading-relaxed m-0">
			Map the columns from your CSV file to the required investment data fields. 
			The system has tried to auto-detect the mappings based on column names.
		</p>
	</div>

	<div class="bg-card rounded-xl p-6 mb-8">
		<div class="mb-6 pb-4 border-b border-button">
			<div class="flex items-center gap-3 text-base">
				<i class="fa-solid fa-file-csv color-success text-xl"></i>
				<span class="font-semibold color-default">{filename}</span>
				<span class="color-muted text-sm">{sampleData.length} rows</span>
			</div>
		</div>

		<div class="flex flex-col gap-6">
			{#each requiredFields as field}
				<div class="grid grid-cols-1 lg:grid-cols-[1fr_40px_1fr] gap-4 items-start p-4 rounded-lg bg-background border border-button">
					<div class="flex flex-col gap-2">
						<div class="flex flex-col gap-2">
							<div class="text-sm font-semibold color-default">
								{field.label}
								{#if field.required}
									<span class="color-error ml-1">*</span>
								{/if}
							</div>
							{#if field.value === 'type'}
								<div class="flex flex-col gap-2">
									<div class="type-toggle">
										<label class="flex items-center gap-2 cursor-pointer text-sm">
											<input 
												type="checkbox" 
												class="w-4 h-4 accent-[var(--color-primary)]"
												bind:checked={useFixedValue}
												onchange={() => handleFixedValueChange(useFixedValue)}
											/>
											<span class="color-default">Use fixed value</span>
										</label>
									</div>
									<button 
										class="btn btn-small mapping-btn"
										onclick={() => showValueMappingDialog('type')}
										disabled={!columnMappings.type && !useFixedValue}
									>
										<i class="fa-solid fa-cog"></i>
										{useFixedValue ? 'Set Value' : 'Map Values'}
									</button>
								</div>
							{/if}
						</div>
					</div>

					<div class="flex items-center justify-center color-muted mt-2">
						<i class="fa-solid fa-arrow-left"></i>
					</div>

					<div class="flex flex-col gap-2">
						{#if field.value === 'type' && useFixedValue}
							<div class="flex items-center gap-2 p-2 bg-background border border-button rounded-md">
								<span class="text-sm color-muted">Fixed Value:</span>
								<span class="font-semibold color-primary px-2 py-1 rounded text-xs uppercase" style="background: color-mix(in srgb, var(--color-primary), transparent 90%);">
									{fixedTypeValue}
								</span>
							</div>
						{:else}
							<CustomDropdown
								options={[
									{ value: '', label: 'Select column...' },
									...csvHeaders.map(header => ({ value: header, label: header }))
								]}
								value={columnMappings[field.value] || ''}
								disabled={field.value === 'type' && useFixedValue}
								onchange={(value) => handleColumnMappingChange(field.value, value)}
							/>
						{/if}

						{#if columnMappings[field.value] && !(field.value === 'type' && useFixedValue)}
							<div class="flex flex-wrap gap-2 items-center">
								<span class="text-xs color-muted">Sample values:</span>
								{#each getSampleValues(columnMappings[field.value]) as value}
									<span class="bg-card px-2 py-1 rounded text-xs color-default border border-button font-mono">{value}</span>
								{/each}
							</div>
						{/if}
					</div>
				</div>
			{/each}
		</div>

		<div class="mt-6 pt-4 border-t border-button">
			{#if isComplete()}
				<div class="flex items-center gap-2 color-success font-medium">
					<i class="fa-solid fa-check-circle"></i>
					All required fields are mapped correctly
				</div>
			{:else}
				<div class="flex items-center gap-2 color-accent font-medium">
					<i class="fa-solid fa-exclamation-triangle"></i>
					Please map all required fields (marked with *)
				</div>
			{/if}
		</div>
	</div>

	<div class="flex justify-between items-center gap-4 mt-8">
		<button class="btn btn-secondary" onclick={handleBack}>
			<i class="fa-solid fa-arrow-left mr-2"></i>
			Back
		</button>
		
		<button 
			class="btn btn-primary btn-big" 
			onclick={handleNext}
			disabled={!isComplete()}
		>
			Continue to Preview
			<i class="fa-solid fa-arrow-right ml-2"></i>
		</button>
	</div>
</div>

<!-- Value Mapping Modal -->
<Modal bind:showModal={showValueMapping} title="Configure Transaction Types" onClose={() => showValueMapping = false}>
	{#snippet children()}
		<div class="w-full">
			<p class="color-muted m-0 mb-6 leading-relaxed">
				{#if useFixedValue}
					Set a fixed transaction type that will be applied to all records.
				{:else}
					Map the transaction type values from your CSV to standard transaction types.
				{/if}
			</p>
			
			{#if useFixedValue}
				<div class="flex flex-col gap-3">
					<div class="font-semibold color-default">Transaction Type:</div>
					<CustomDropdown
						options={transactionTypes}
						value={fixedTypeValue}
						onchange={(value) => handleFixedTypeValueChange(value)}
					/>
				</div>
			{:else}
				<div class="flex flex-col gap-4">
					{#each getUniqueValues(columnMappings.type) as originalValue}
						<div class="grid grid-cols-1 md:grid-cols-[1fr_40px_1fr] gap-4 items-center p-4 bg-background rounded-md border border-button">
							<div class="flex flex-col gap-1">
								<span class="text-xs color-muted">CSV Value:</span>
								<code class="bg-card px-2 py-1 rounded font-mono text-sm color-default border border-button">{originalValue}</code>
							</div>
							
							<div class="flex items-center justify-center color-muted mt-2 md:mt-0">
								<i class="fa-solid fa-arrow-right md:block hidden"></i>
								<i class="fa-solid fa-arrow-down md:hidden block"></i>
							</div>
							
							<div>
								<CustomDropdown
									options={[
										{ value: '', label: 'Select type...' },
										...transactionTypes
									]}
									value={valueMappings.type?.[originalValue] || ''}
									onchange={(value) => handleValueMappingChange(originalValue, value)}
								/>
							</div>
						</div>
					{/each}
				</div>
			{/if}
		</div>
	{/snippet}
</Modal>

<style>
	/* Responsive arrow rotation for mobile */
	@media (max-width: 768px) {
		.grid-cols-1 .fa-arrow-left {
			transform: rotate(90deg);
		}
	}
</style>