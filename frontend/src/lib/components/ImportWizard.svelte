<script lang="ts">
	import { onMount } from 'svelte';
	import ProgressBar from './ProgressBar.svelte';
	import FileUploadStep from './FileUploadStep.svelte';
	import ColumnMappingStep from './ColumnMappingStep.svelte';
	import DataPreviewStep from './DataPreviewStep.svelte';

	interface Step {
		id: number;
		title: string;
		description: string;
		completed: boolean;
	}

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

	// Wizard state
	let currentStep = $state(0);
	let isAnimating = $state(false);
	let slideDirection = $state<'left' | 'right'>('right');

	// Step definitions
	let steps: Step[] = $state([
		{
			id: 0,
			title: 'Upload File',
			description: 'Select your CSV file',
			completed: false
		},
		{
			id: 1,
			title: 'Map Columns',
			description: 'Configure field mappings',
			completed: false
		},
		{
			id: 2,
			title: 'Preview Data',
			description: 'Review and import',
			completed: false
		}
	]);

	// Data state
	let filename = $state('');
	let csvContent = $state('');
	let csvHeaders: string[] = $state([]);
	let csvData: string[][] = $state([]);
	let columnMappings: Record<string, string> = $state({});
	let valueMappings: Record<string, Record<string, string>> = $state({});
	let useFixedValue = $state(false);
	let fixedTypeValue = $state('BUY');
	let treatHeaderAsData = $state(false);

	function parseCsvContent(content: string, treatHeaderAsData: boolean = false) {
		const lines = content.split('\n').filter(line => line.trim());
		
		if (treatHeaderAsData) {
			// When treating header as data, use first row as headers and include all rows as data
			const headers = lines[0].split(';').map(cell => cell.trim());
			const data = lines.map(line => line.split(';').map(cell => cell.trim()));
			return { headers, data };
		} else {
			// Standard parsing with first row as headers
			const headers = lines[0].split(';').map(h => h.trim());
			const data = lines.slice(1).map(line => line.split(';').map(cell => cell.trim()));
			return { headers, data };
		}
	}

	function navigateToStep(targetStep: number) {
		if (targetStep === currentStep || isAnimating) return;
		
		isAnimating = true;
		slideDirection = targetStep > currentStep ? 'right' : 'left';
		
		// Update completed status for steps we're moving past
		for (let i = 0; i < targetStep; i++) {
			steps[i].completed = true;
		}
		
		// Small delay for animation
		setTimeout(() => {
			currentStep = targetStep;
			setTimeout(() => {
				isAnimating = false;
			}, 300);
		}, 150);
	}

	function handleFileSelected(data: { filename: string; content: string; treatHeaderAsData: boolean }) {
		filename = data.filename;
		csvContent = data.content;
		treatHeaderAsData = data.treatHeaderAsData;
		
		// Parse CSV to extract headers and data
		const parsed = parseCsvContent(data.content, data.treatHeaderAsData);
		csvHeaders = parsed.headers;
		csvData = parsed.data;
	}

	function handleFileUploadNext() {
		navigateToStep(1);
	}

	function handleMappingChanged(data: {
		columnMappings: Record<string, string>;
		valueMappings: Record<string, Record<string, string>>;
		useFixedValue: boolean;
		fixedTypeValue: string;
	}) {
		columnMappings = data.columnMappings;
		valueMappings = data.valueMappings;
		useFixedValue = data.useFixedValue;
		fixedTypeValue = data.fixedTypeValue;
	}

	function handleMappingNext() {
		navigateToStep(2);
	}

	function handleMappingBack() {
		navigateToStep(0);
	}

	function handlePreviewBack() {
		navigateToStep(1);
	}

	function handleImport(data: { validatedData: ProcessedInvestment[] }) {
		// Mark final step as completed
		steps[2].completed = true;
		
		// Call the parent component's import function
		if (onImportInvestments) {
			onImportInvestments(data.validatedData);
		}
	}

	onMount(() => {
		// ensure callbacks exist
		if (!onImportInvestments) {
			console.warn('ImportWizard: `onImportInvestments` prop is not provided.');
		}
	});
</script>

<div class="min-h-150 w-full">
	<div class="max-w-6xl mx-auto">
		<!-- Progress Bar -->
		<ProgressBar {steps} {currentStep} />

		<!-- Step Content Container -->
		<div class="relative overflow-hidden min-h-125 my-4">
			<div 
				class="w-full opacity-100 transform-none transition-all duration-400 ease-in-out {isAnimating ? 'opacity-0' : ''} {isAnimating && slideDirection === 'right' ? 'translate-x-[50px]' : ''} {isAnimating && slideDirection === 'left' ? 'translate-x-[-50px]' : ''}"
				class:step-0={currentStep === 0}
				class:step-1={currentStep === 1}
				class:step-2={currentStep === 2}
			>
				{#if currentStep === 0}
					<FileUploadStep 
						onfileSelected={handleFileSelected}
						onnext={handleFileUploadNext}
					/>
				{:else if currentStep === 1}
					<ColumnMappingStep 
						csvHeaders={csvHeaders}
						sampleData={csvData.slice(0, 5)}
						filename={filename}
						initialColumnMappings={columnMappings}
						initialValueMappings={valueMappings}
						initialUseFixedValue={useFixedValue}
						initialFixedTypeValue={fixedTypeValue}
						treatHeaderAsData={treatHeaderAsData}
						onmappingChanged={handleMappingChanged}
						onnext={handleMappingNext}
						onback={handleMappingBack}
					/>
				{:else if currentStep === 2}
					<DataPreviewStep 
						csvData={csvData}
						csvHeaders={csvHeaders}
						columnMappings={columnMappings}
						valueMappings={valueMappings}
						useFixedValue={useFixedValue}
						fixedTypeValue={fixedTypeValue}
						onback={handlePreviewBack}
						onimport={handleImport}
					/>
				{/if}
			</div>
		</div>


	</div>
</div>

<style>
	/* Responsive animation adjustments for mobile */
	@media (max-width: 768px) {
		.translate-x-\[50px\] {
			transform: translateX(30px);
		}
		
		.translate-x-\[-50px\] {
			transform: translateX(-30px);
		}
	}
</style>