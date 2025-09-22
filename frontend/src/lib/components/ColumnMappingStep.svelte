<script lang="ts">
	import { createEventDispatcher } from 'svelte';
	import Modal from './Modal.svelte';
	import CustomDropdown from './CustomDropdown.svelte';

	const dispatch = createEventDispatcher<{
		back: void;
		next: void;
		mappingChanged: {
			columnMappings: Record<string, string>;
			valueMappings: Record<string, Record<string, string>>;
			useFixedValue: boolean;
			fixedTypeValue: string;
		};
	}>();

	interface ColumnMapping {
		csvColumn: string;
		targetField: string;
		sampleValues: string[];
	}

	interface ValueMapping {
		originalValue: string;
		mappedValue: string;
	}

	let { 
		csvContent,
		filename,
		columnMappings: initialColumnMappings = {},
		valueMappings: initialValueMappings = {},
		useFixedValue: initialUseFixedValue = false,
		fixedTypeValue: initialFixedTypeValue = 'BUY'
	}: { 
		csvContent: string; 
		filename: string; 
		columnMappings?: Record<string, string>;
		valueMappings?: Record<string, Record<string, string>>;
		useFixedValue?: boolean;
		fixedTypeValue?: string;
	} = $props();

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

	let csvHeaders: string[] = $state([]);
	let csvRows: string[][] = $state([]);
	let columnMappings: Record<string, string> = $state(initialColumnMappings);
	let valueMappings: Record<string, Record<string, string>> = $state(initialValueMappings);
	let showValueMapping = $state(false);
	let selectedFieldForMapping = $state('');
	let useFixedValue = $state(initialUseFixedValue);
	let fixedTypeValue = $state(initialFixedTypeValue);

	// Parse CSV content
	function parseCSV() {
		const lines = csvContent.split('\n').filter(line => line.trim());
		if (lines.length === 0) return;

		// Parse headers (first line)
		csvHeaders = lines[0].split(';').map(header => header.trim());
		
		// Parse data rows
		csvRows = lines.slice(1).map(line => 
			line.split(';').map(cell => cell.trim())
		);

		// Initialize column mappings with smart defaults
		initializeColumnMappings();
	}

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
		
		const values = csvRows
			.map(row => row[columnIndex])
			.filter(val => val && val.trim())
			.slice(0, 5);
		
		return [...new Set(values)]; // Remove duplicates
	}

	function getUniqueValues(csvColumn: string): string[] {
		const columnIndex = csvHeaders.indexOf(csvColumn);
		if (columnIndex === -1) return [];
		
		const values = csvRows
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
		dispatch('mappingChanged', {
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
			dispatch('next');
		}
	}

	function handleBack() {
		dispatch('back');
	}

	// Initialize when component mounts
	parseCSV();
</script>

<div class="mapping-step">
	<div class="step-header">
		<h2>Map CSV Columns</h2>
		<p class="step-description">
			Map the columns from your CSV file to the required investment data fields. 
			The system has tried to auto-detect the mappings based on column names.
		</p>
	</div>

	<div class="mapping-container">
		<div class="file-info">
			<div class="file-header">
				<i class="fa-solid fa-file-csv"></i>
				<span class="filename">{filename}</span>
				<span class="row-count">{csvRows.length} rows</span>
			</div>
		</div>

		<div class="mapping-grid">
			{#each requiredFields as field}
				<div class="mapping-row">
					<div class="target-field">
						<div class="field-info">
							<div class="field-label">
								{field.label}
								{#if field.required}
									<span class="required">*</span>
								{/if}
							</div>
							{#if field.value === 'type'}
								<div class="type-options">
									<div class="type-toggle">
										<label class="toggle-label">
											<input 
												type="checkbox" 
												bind:checked={useFixedValue}
												onchange={() => handleFixedValueChange(useFixedValue)}
											/>
											<span class="toggle-text">Use fixed value</span>
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

					<div class="arrow">
						<i class="fa-solid fa-arrow-left"></i>
					</div>

					<div class="csv-column">
						{#if field.value === 'type' && useFixedValue}
							<div class="fixed-value-display">
								<span class="fixed-label">Fixed Value:</span>
								<span class="fixed-value">{fixedTypeValue}</span>
							</div>
						{:else}
							<CustomDropdown
								options={[
									{ value: '', label: 'Select column...' },
									...csvHeaders.map(header => ({ value: header, label: header }))
								]}
								value={columnMappings[field.value] || ''}
								disabled={field.value === 'type' && useFixedValue}
								onchange={(e) => handleColumnMappingChange(field.value, e.detail)}
							/>
						{/if}

						{#if columnMappings[field.value] && !(field.value === 'type' && useFixedValue)}
							<div class="sample-values">
								<span class="sample-label">Sample values:</span>
								{#each getSampleValues(columnMappings[field.value]) as value}
									<span class="sample-value">{value}</span>
								{/each}
							</div>
						{/if}
					</div>
				</div>
			{/each}
		</div>

		<div class="validation-summary">
			{#if isComplete()}
				<div class="validation-success">
					<i class="fa-solid fa-check-circle"></i>
					All required fields are mapped correctly
				</div>
			{:else}
				<div class="validation-warning">
					<i class="fa-solid fa-exclamation-triangle"></i>
					Please map all required fields (marked with *)
				</div>
			{/if}
		</div>
	</div>

	<div class="step-actions">
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
		<div class="modal-content-inner">
			<p class="modal-description">
				{#if useFixedValue}
					Set a fixed transaction type that will be applied to all records.
				{:else}
					Map the transaction type values from your CSV to standard transaction types.
				{/if}
			</p>
			
			{#if useFixedValue}
				<div class="fixed-value-section">
					<div class="fixed-value-label">Transaction Type:</div>
					<CustomDropdown
						options={transactionTypes}
						value={fixedTypeValue}
						onchange={(e) => handleFixedTypeValueChange(e.detail)}
					/>
				</div>
			{:else}
				<div class="value-mappings">
					{#each getUniqueValues(columnMappings.type) as originalValue}
						<div class="value-mapping-row">
							<div class="original-value">
								<span class="value-label">CSV Value:</span>
								<code>{originalValue}</code>
							</div>
							
							<div class="arrow">
								<i class="fa-solid fa-arrow-right"></i>
							</div>
							
							<div class="mapped-value">
								<CustomDropdown
									options={[
										{ value: '', label: 'Select type...' },
										...transactionTypes
									]}
									value={valueMappings.type?.[originalValue] || ''}
									onchange={(e) => handleValueMappingChange(originalValue, e.detail)}
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
	.mapping-step {
		max-width: 800px;
		margin: 0 auto;
	}

	.step-header {
		text-align: center;
		margin-bottom: 2rem;
	}

	.step-header h2 {
		font-size: 1.5rem;
		font-weight: 600;
		color: var(--color-text);
		margin: 0 0 0.5rem 0;
	}

	.step-description {
		color: var(--color-muted);
		line-height: 1.5;
		margin: 0;
	}

	.mapping-container {
		background: var(--color-card);
		border-radius: 12px;
		padding: 1.5rem;
		margin-bottom: 2rem;
	}

	.file-info {
		margin-bottom: 1.5rem;
		padding-bottom: 1rem;
		border-bottom: 1px solid var(--color-button);
	}

	.file-header {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		font-size: 1rem;
	}

	.file-header i {
		color: var(--color-success);
		font-size: 1.2rem;
	}

	.filename {
		font-weight: 600;
		color: var(--color-text);
	}

	.row-count {
		color: var(--color-muted);
		font-size: 0.9rem;
	}

	.mapping-grid {
		display: flex;
		flex-direction: column;
		gap: 1.5rem;
	}

	.mapping-row {
		display: grid;
		grid-template-columns: 1fr 40px 1fr;
		gap: 1rem;
		align-items: start;
		padding: 1rem;
		border-radius: 8px;
		background: var(--color-background);
		border: 1px solid var(--color-button);
	}

	.target-field {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.field-info {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.field-label {
		font-weight: 600;
		color: var(--color-text);
		font-size: 0.95rem;
	}

	.required {
		color: var(--color-error);
		margin-left: 0.25rem;
	}

	.type-options {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.type-toggle {
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}

	.toggle-label {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		cursor: pointer;
		font-size: 0.9rem;
	}

	.toggle-label input[type="checkbox"] {
		width: 16px;
		height: 16px;
		accent-color: var(--color-primary);
	}

	.toggle-text {
		color: var(--color-text);
	}

	.mapping-btn {
		font-size: 0.8rem;
		padding: 0.25rem 0.75rem;
		align-self: flex-start;
	}

	.arrow {
		display: flex;
		align-items: center;
		justify-content: center;
		color: var(--color-muted);
		margin-top: 0.5rem;
	}

	.csv-column {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.fixed-value-display {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		padding: 0.5rem;
		background: var(--color-background);
		border: 1px solid var(--color-button);
		border-radius: 6px;
	}

	.fixed-label {
		font-size: 0.9rem;
		color: var(--color-muted);
	}

	.fixed-value {
		font-weight: 600;
		color: var(--color-primary);
		padding: 0.25rem 0.5rem;
		background: color-mix(in srgb, var(--color-primary), transparent 90%);
		border-radius: 4px;
		font-size: 0.8rem;
		text-transform: uppercase;
	}

	.sample-values {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem;
		align-items: center;
	}

	.sample-label {
		font-size: 0.8rem;
		color: var(--color-muted);
	}

	.sample-value {
		background: var(--color-card);
		padding: 0.25rem 0.5rem;
		border-radius: 4px;
		font-size: 0.8rem;
		color: var(--color-text);
		border: 1px solid var(--color-button);
		font-family: 'Fira Mono', 'Consolas', monospace;
	}

	.validation-summary {
		margin-top: 1.5rem;
		padding-top: 1rem;
		border-top: 1px solid var(--color-button);
	}

	.validation-success {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		color: var(--color-success);
		font-weight: 500;
	}

	.validation-warning {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		color: var(--color-error);
		font-weight: 500;
	}

	.step-actions {
		display: flex;
		justify-content: space-between;
		align-items: center;
		gap: 1rem;
	}

	/* Modal Styles */
	.modal-content-inner {
		width: 100%;
	}

	.modal-description {
		color: var(--color-muted);
		margin: 0 0 1.5rem 0;
		line-height: 1.5;
	}

	.fixed-value-section {
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
	}

	.fixed-value-label {
		font-weight: 600;
		color: var(--color-text);
	}

	.value-mappings {
		display: flex;
		flex-direction: column;
		gap: 1rem;
	}

	.value-mapping-row {
		display: grid;
		grid-template-columns: 1fr 40px 1fr;
		gap: 1rem;
		align-items: center;
		padding: 1rem;
		background: var(--color-background);
		border-radius: 6px;
		border: 1px solid var(--color-button);
	}

	.original-value {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
	}

	.value-label {
		font-size: 0.8rem;
		color: var(--color-muted);
	}

	.original-value code {
		background: var(--color-card);
		padding: 0.25rem 0.5rem;
		border-radius: 4px;
		font-family: 'Fira Mono', 'Consolas', monospace;
		font-size: 0.9rem;
		color: var(--color-text);
		border: 1px solid var(--color-button);
	}

	.mapped-value {
		/* Remove the margin-top to align properly */
	}

	.mr-2 {
		margin-right: 0.5rem;
	}

	.ml-2 {
		margin-left: 0.5rem;
	}

	/* Responsive design */
	@media (max-width: 768px) {
		.mapping-row {
			grid-template-columns: 1fr;
			gap: 0.75rem;
		}

		.arrow {
			transform: rotate(90deg);
		}

		.type-options {
			align-items: flex-start;
		}

		.step-actions {
			flex-direction: column;
		}

		.value-mapping-row {
			grid-template-columns: 1fr;
			gap: 0.75rem;
		}

		.value-mapping-row .arrow {
			transform: rotate(90deg);
		}
	}
</style>