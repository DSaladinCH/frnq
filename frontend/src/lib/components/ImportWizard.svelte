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
	let columnMappings: Record<string, string> = $state({});
	let valueMappings: Record<string, Record<string, string>> = $state({});
	let useFixedValue = $state(false);
	let fixedTypeValue = $state('BUY');

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

	function handleFileSelected(event: CustomEvent<{ filename: string; content: string }>) {
		filename = event.detail.filename;
		csvContent = event.detail.content;
	}

	function handleFileUploadNext() {
		navigateToStep(1);
	}

	function handleMappingChanged(event: CustomEvent<{
		columnMappings: Record<string, string>;
		valueMappings: Record<string, Record<string, string>>;
		useFixedValue?: boolean;
		fixedTypeValue?: string;
	}>) {
		columnMappings = event.detail.columnMappings;
		valueMappings = event.detail.valueMappings;
		if (event.detail.useFixedValue !== undefined) {
			useFixedValue = event.detail.useFixedValue;
		}
		if (event.detail.fixedTypeValue !== undefined) {
			fixedTypeValue = event.detail.fixedTypeValue;
		}
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

	function handleImport(event: CustomEvent<{ validatedData: ProcessedInvestment[] }>) {
		// Mark final step as completed
		steps[2].completed = true;
		
		// Call the parent component's import function
		if (onImportInvestments) {
			onImportInvestments(event.detail.validatedData);
		}
	}

	onMount(() => {
		// ensure callbacks exist
		if (!onImportInvestments) {
			console.warn('ImportWizard: `onImportInvestments` prop is not provided.');
		}
	});
</script>

<div class="import-wizard">
	<div class="wizard-container">
		<!-- Progress Bar -->
		<ProgressBar {steps} {currentStep} />

		<!-- Step Content Container -->
		<div class="step-container">
			<div 
				class="step-content {isAnimating ? 'animating' : ''} slide-{slideDirection}"
				class:step-0={currentStep === 0}
				class:step-1={currentStep === 1}
				class:step-2={currentStep === 2}
			>
				{#if currentStep === 0}
					<FileUploadStep 
						on:fileSelected={handleFileSelected}
						on:next={handleFileUploadNext}
					/>
				{:else if currentStep === 1}
					<ColumnMappingStep 
						{csvContent}
						{filename}
						{columnMappings}
						{valueMappings}
						{useFixedValue}
						{fixedTypeValue}
						on:mappingChanged={handleMappingChanged}
						on:next={handleMappingNext}
						on:back={handleMappingBack}
					/>
				{:else if currentStep === 2}
					<DataPreviewStep 
						{csvContent}
						{filename}
						{columnMappings}
						{valueMappings}
						on:back={handlePreviewBack}
						on:import={handleImport}
					/>
				{/if}
			</div>
		</div>


	</div>
</div>

<style>
	.import-wizard {
		min-height: 600px;
		width: 100%;
	}

	.wizard-container {
		max-width: 1200px;
		margin: 0 auto;
	}

	.step-container {
		position: relative;
		overflow: hidden;
		min-height: 500px;
		margin: 1rem 0;
	}

	.step-content {
		width: 100%;
		transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
		opacity: 1;
		transform: translateX(0);
	}

	.step-content.animating {
		opacity: 0;
	}

	.step-content.animating.slide-right {
		transform: translateX(50px);
	}

	.step-content.animating.slide-left {
		transform: translateX(-50px);
	}

	/* Step Navigation */
	/* Removed redundant bottom navigation - progress bar at top is sufficient */

	/* Responsive design */
	@media (max-width: 768px) {
		.step-content.animating.slide-right {
			transform: translateX(30px);
		}

		.step-content.animating.slide-left {
			transform: translateX(-30px);
		}
	}

	/* Animation keyframes for enhanced effects */
	@keyframes slideInRight {
		from {
			opacity: 0;
			transform: translateX(50px);
		}
		to {
			opacity: 1;
			transform: translateX(0);
		}
	}

	@keyframes slideInLeft {
		from {
			opacity: 0;
			transform: translateX(-50px);
		}
		to {
			opacity: 1;
			transform: translateX(0);
		}
	}

	.step-content:not(.animating) {
		animation: slideInRight 0.4s cubic-bezier(0.4, 0, 0.2, 1);
	}
</style>