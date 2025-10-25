<script lang="ts">
	import Modal from './Modal.svelte';
	import DropDown from './DropDown.svelte';
	import Input from './Input.svelte';
	import Button from './Button.svelte';
	import { TextSize } from '$lib/types/TextSize';
	import { ColorStyle } from '$lib/types/ColorStyle';

	interface Props {
		csvHeaders: string[];
		csvData: string[][];
		filename?: string;
		initialColumnMappings?: Record<string, string>;
		initialValueMappings?: Record<string, Record<string, string>>;
		initialUseFixedValue?: boolean;
		initialFixedTypeValue?: string;
		treatHeaderAsData?: boolean;
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
		csvData,
		filename = 'uploaded-file.csv',
		initialColumnMappings = {},
		initialValueMappings = {},
		initialUseFixedValue = false,
		initialFixedTypeValue = '',
		treatHeaderAsData = false,
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
		{ value: 'DIVIDEND', label: 'Dividend' }
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
				if (possibleNames.some((name) => normalizedHeader.includes(name))) {
					columnMappings[targetField] = header;
					break;
				}
			}
		}
	}

	function getSampleValues(csvColumn: string): string[] {
		const columnIndex = csvHeaders.indexOf(csvColumn);
		if (columnIndex === -1) return [];

		const values = csvData
			.slice(0, 5) // Only use first 5 rows for sample display
			.map((row) => row[columnIndex])
			.filter((val) => val && val.trim())
			.slice(0, 3); // Show only first 3 unique values for cleaner display

		return [...new Set(values)]; // Remove duplicates
	}

	function getUniqueValues(csvColumn: string): string[] {
		const columnIndex = csvHeaders.indexOf(csvColumn);
		if (columnIndex === -1) return [];

		const values = csvData.map((row) => row[columnIndex]).filter((val) => val && val.trim());

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
		const requiredFieldsSet = requiredFields.filter((f) => f.required).map((f) => f.value);
		// For type field, it's complete if we have either a column mapping or fixed value
		const typeComplete = Boolean(columnMappings.type) || useFixedValue;
		const otherFieldsComplete = requiredFieldsSet
			.filter((f) => f !== 'type')
			.every((field) => Boolean(columnMappings[field]));
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

<div class="mx-auto flex max-w-4xl flex-col gap-8">
	<div class="text-center">
		<h2 class="color-default mb-2 text-2xl font-semibold">Map CSV Columns</h2>
		<p class="color-muted m-0 leading-relaxed">
			{#if treatHeaderAsData}
				Map the columns from your CSV file to the required investment data fields. The column headers shown are the values from your first data row.
			{:else}
				Map the columns from your CSV file to the required investment data fields. The system has tried to auto-detect the mappings based on column names.
			{/if}
		</p>
	</div>

	<div class="bg-card rounded-xl p-6">
		<div class="border-button mb-6 border-b pb-4">
			<div class="flex items-center gap-3 text-base">
				<i class="fa-solid fa-file-csv color-success text-xl"></i>
				<span class="color-default font-semibold">{filename}</span>
				<span class="color-muted text-sm">
					{#if treatHeaderAsData}
						{csvData.length} total rows (all as data)
					{:else}
						{csvData.length} data rows
					{/if}
				</span>
			</div>
		</div>

		<div class="flex flex-col gap-4">
			{#each requiredFields as field}
				<div
					class="bg-background border-button grid grid-cols-1 items-start gap-x-4 rounded-lg border p-4 max-md:gap-y-4 md:grid-cols-[1fr_40px_1fr]"
				>
					<div class="row-1 col-1 flex flex-col gap-2 self-center max-md:text-center">
						<div class="color-default text-base font-semibold">
							{field.label}
							{#if field.required}
								<span class="color-error">*</span>
							{/if}
						</div>
					</div>

					{#if field.value === 'type'}
						<div class="row-2 col-1 flex flex-col gap-2">
							<div class="type-toggle text-sm max-md:self-center">
								<Input
									type="checkbox"
									bind:checked={useFixedValue}
									onchange={() => handleFixedValueChange(useFixedValue)}
									title="Use fixed value"
								/>
							</div>

							<div class="max-md:self-center">
								<Button
									icon="fa-solid fa-cog"
									text={useFixedValue ? 'Set Value' : 'Map Values'}
									onclick={() => showValueMappingDialog('type')}
									disabled={!columnMappings.type && !useFixedValue}
									textSize={TextSize.Small}
									style={ColorStyle.Control}
								/>
							</div>
						</div>
					{/if}

					<div class="color-muted flex items-center justify-center self-center">
						<i class="fa-solid fa-arrow-left max-md:rotate-90"></i>
					</div>

					<div class="flex flex-col gap-2">
						{#if field.value === 'type' && useFixedValue}
							<div
								class="bg-background border-button flex items-center gap-2 rounded-md border p-2"
							>
								<span class="color-muted text-sm">Fixed Value:</span>
								<span
									class="color-primary rounded px-2 py-1 text-xs font-semibold uppercase"
									style="background: color-mix(in srgb, var(--color-primary), transparent 90%);"
								>
									{fixedTypeValue}
								</span>
							</div>
						{:else}
							<DropDown
								options={[
									{ value: '', label: 'Select column...' },
									...csvHeaders.map((header) => ({ value: header, label: header }))
								]}
								value={columnMappings[field.value] || ''}
								disabled={field.value === 'type' && useFixedValue}
								onchange={(value) => handleColumnMappingChange(field.value, value)}
							/>
						{/if}

						{#if columnMappings[field.value] && !(field.value === 'type' && useFixedValue)}
							<div class="flex flex-wrap items-center gap-2">
								<span class="color-muted text-xs">Sample values:</span>
								{#each getSampleValues(columnMappings[field.value]) as value}
									<span
										class="bg-card color-default border-button rounded border px-2 py-1 font-mono text-xs"
										>{value}</span
									>
								{/each}
							</div>
						{/if}
					</div>
				</div>
			{/each}
		</div>

		<div class="border-button mt-6 border-t pt-4">
			{#if isComplete()}
				<div class="color-success flex items-center gap-2 font-medium">
					<i class="fa-solid fa-check-circle"></i>
					All required fields are mapped correctly
				</div>
			{:else}
				<div class="color-accent flex items-center gap-2 font-medium">
					<i class="fa-solid fa-exclamation-triangle"></i>
					Please map all required fields (marked with *)
				</div>
			{/if}
		</div>
	</div>

	<div class="h-13 flex items-center justify-between gap-4">
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
				text="Continue to Preview"
				icon="fa-solid fa-arrow-right"
				onclick={handleNext}
				disabled={!isComplete()}
				textSize={TextSize.Large}
				style={ColorStyle.Primary}
			/>
		</div>
	</div>
</div>

<!-- Value Mapping Modal -->
<Modal
	bind:showModal={showValueMapping}
	title="Configure Transaction Types"
	onClose={() => (showValueMapping = false)}
>
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
					<div class="color-default font-semibold">Transaction Type:</div>
					<DropDown
						options={transactionTypes}
						value={fixedTypeValue}
						onchange={(value) => handleFixedTypeValueChange(value)}
					/>
				</div>
			{:else}
				<div class="flex flex-col gap-4">
					{#each getUniqueValues(columnMappings.type) as originalValue}
						<div
							class="bg-background border-button grid grid-cols-1 items-center gap-4 rounded-md border p-4 md:grid-cols-[1fr_40px_1fr]"
						>
							<div class="flex flex-col gap-1">
								<code
									class="bg-card color-default border-button rounded border px-2 py-2 font-mono text-base"
									>{originalValue}</code
								>
							</div>

							<div class="color-muted mt-2 flex items-center justify-center md:mt-0">
								<i class="fa-solid fa-arrow-right max-md:rotate-90"></i>
							</div>

							<div>
								<DropDown
									options={[{ value: '', label: 'Select type...' }, ...transactionTypes]}
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
</style>
