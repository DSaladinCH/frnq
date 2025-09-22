<script lang="ts">
	interface Step {
		id: number;
		title: string;
		description: string;
		completed: boolean;
	}

	let { steps, currentStep }: { steps: Step[]; currentStep: number } = $props();

	function getStepClass(stepIndex: number): string {
		const step = steps[stepIndex];
		if (step.completed) return 'completed';
		if (stepIndex === currentStep) return 'current';
		if (stepIndex < currentStep) return 'completed';
		return 'upcoming';
	}

	function getStepCircleClasses(stepIndex: number): string {
		const baseClasses = "w-12 h-12 rounded-full flex items-center justify-center font-bold relative z-20 shadow-sm transition-all duration-400 ease-in-out";
		const step = steps[stepIndex];
		
		if (step.completed || stepIndex < currentStep) {
			// Completed step
			return `${baseClasses} bg-success border-2 border-success text-white scale-100`;
		} else if (stepIndex === currentStep) {
			// Current step
			return `${baseClasses} bg-primary border-2 border-secondary text-white scale-110 animate-pulse`;
		} else {
			// Upcoming step
			return `${baseClasses} bg-card border-2 border-button color-muted scale-90`;
		}
	}

	function getCompactDotClasses(stepIndex: number): string {
		const baseClasses = "w-8 h-8 rounded-full flex items-center justify-center text-sm font-semibold transition-all duration-300 relative z-10";
		const step = steps[stepIndex];
		
		if (step.completed || stepIndex < currentStep) {
			// Completed step
			return `${baseClasses} bg-success border-2 border-success text-white scale-100`;
		} else if (stepIndex === currentStep) {
			// Current step
			return `${baseClasses} bg-primary border-2 border-secondary text-white scale-110 animate-pulse`;
		} else {
			// Upcoming step
			return `${baseClasses} bg-card border-2 border-button color-muted scale-90`;
		}
	}
</script>

<div class="mb-4 p-4 flex justify-center">
	<!-- Desktop/Tablet Version -->
	<div class="progress-bar hidden md:flex justify-center items-start relative max-w-4xl w-full">
		{#each steps as step, index}
			<div class="step-wrapper h-full flex relative flex-1 last:flex-none">
				<div class="step {getStepClass(index)} flex flex-col items-center text-center z-10 relative transition-all duration-300">
					<div class="{getStepCircleClasses(index)}">
						{#if step.completed || index < currentStep}
							<i class="fa-solid fa-check"></i>
						{:else}
							<span class="step-number text-lg font-semibold">{index + 1}</span>
						{/if}
					</div>
					<div class="step-content mt-3 max-w-32">
						<h3 class="step-title text-sm font-semibold m-0 mb-1 transition-colors duration-300">{step.title}</h3>
						<p class="step-description text-xs m-0 leading-tight transition-colors duration-300">{step.description}</p>
					</div>
				</div>
				{#if index < steps.length - 1}
					<div class="connector {index < currentStep ? 'completed bg-success after:w-100' : 'bg-button'} flex-1 h-0.5 mx-4 relative top-6 transition-colors duration-500 ease-in-out"></div>
				{/if}
			</div>
		{/each}
	</div>

	<!-- Compact Mobile Version -->
	<div class="progress-compact flex md:hidden flex-col gap-4 w-full max-w-sm mx-auto">		
		<div class="compact-progress relative">
			<div class="bg-button h-1 rounded-sm relative overflow-hidden">
				<div 
					class="compact-fill bg-primary h-full rounded-sm relative transition-all duration-500" 
					style="width: {((currentStep + 1) / steps.length) * 100}%"
				></div>
			</div>
			<div class="compact-indicators flex justify-between mt-3 relative">
				{#each steps as step, index}
					<div class="{getCompactDotClasses(index)}">
						{#if step.completed || index < currentStep}
							<i class="fa-solid fa-check"></i>
						{:else if index === currentStep}
							<span class="compact-number text-sm font-semibold">{index + 1}</span>
						{:else}
							<span class="compact-number text-sm font-semibold">{index + 1}</span>
						{/if}
					</div>
				{/each}
			</div>
		</div>
	</div>
</div>

<style>
	/* Desktop Layout Specific Styles */
	.step.upcoming .step-title {
		color: var(--color-muted);
	}

	.step.upcoming .step-description {
		color: color-mix(in srgb, var(--color-muted), transparent 30%);
	}

	.step.current .step-title {
		color: var(--color-primary);
	}

	.step.current .step-description {
		color: var(--color-text);
	}

	.step.completed .step-title {
		color: var(--color-success);
	}

	.step.completed .step-description {
		color: var(--color-text);
	}

	.connector::after {
		content: '';
		position: absolute;
		top: 0;
		left: 0;
		height: 100%;
		background-color: var(--color-success);
		width: 0%;
		transition: width 0.5s ease 0.2s;
	}
</style>