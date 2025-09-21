<script lang="ts">
	import { onMount } from 'svelte';

	let { onImportInvestments }: { onImportInvestments: (data: string) => void } = $props();

	let fileInput: HTMLInputElement | null = $state(null);
	let isDragActive = $state(false);
	let filename: string | null = $state(null);
	let error: string | null = null;

    let fileText: string | null = $state(null);

	function openFilePicker() {
		fileInput?.click();
	}

	function reset() {
		filename = null;
		error = null;
		if (fileInput) fileInput.value = '';
	}

	function handleFiles(files: FileList | null) {
		error = null;
		filename = null;
		if (!files || files.length === 0) return;
		const file = files[0];

		if (!file.name.toLowerCase().endsWith('.csv')) {
			error = 'Please upload a CSV file.';
			return;
		}

		filename = file.name;
		const reader = new FileReader();
		reader.onload = () => {
			const text = reader.result as string;
			try {
                fileText = text;
			} catch (e) {
				error = 'Import handler threw an error.';
			}
		};
		reader.onerror = () => {
			error = 'Failed to read file.';
		};

		reader.readAsText(file, 'utf-8');
	}

	function onInputChange(e: Event) {
		const input = e.target as HTMLInputElement;
		handleFiles(input.files);
	}

	function onDrop(e: DragEvent) {
		e.preventDefault();
		e.stopPropagation();
		e.stopImmediatePropagation();
		isDragActive = false;
		handleFiles(e.dataTransfer?.files ?? null);
	}

	function onDragOver(e: DragEvent) {
		e.preventDefault();
		e.stopPropagation();
		e.dataTransfer!.dropEffect = 'copy';
		isDragActive = true;
	}

	function onDragLeave(e: DragEvent) {
		e.preventDefault();
		e.stopPropagation();
		isDragActive = false;
	}

	function onKeyDown(e: KeyboardEvent) {
		if (e.key === 'Enter' || e.key === ' ') {
			e.preventDefault();
			openFilePicker();
		}
		if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'r') {
			// Ctrl/Cmd+R resets selection
			e.preventDefault();
			reset();
		}
	}

	onMount(() => {
		// ensure callbacks exist
		if (!onImportInvestments) {
			console.warn('InvestmentImport: `onImportInvestments` prop is not provided.');
		}
	});
</script>

{#if filename}
    <p>
        {fileText}
    </p>
{:else}
	<div
		class="upload"
		role="button"
		tabindex="0"
		aria-label="Upload CSV file"
		aria-describedby="upload-desc upload-filename upload-error"
		onclick={openFilePicker}
		onkeydown={onKeyDown}
		ondrop={onDrop}
		ondragover={onDragOver}
		ondragleave={onDragLeave}
	>
		<input
			bind:this={fileInput}
			class="visually-hidden"
			id="file"
			type="file"
			accept=".csv,text/csv"
			aria-hidden="true"
			onchange={onInputChange}
		/>

		<div class={isDragActive ? 'upload-inner active' : 'upload-inner'}>
			<svg
				class="icon"
				width="48"
				height="48"
				viewBox="0 0 24 24"
				aria-hidden="true"
				focusable="false"
			>
				<path fill="currentColor" d="M19 9h-4V3H9v6H5l7 7 7-7zM5 18v2h14v-2H5z" />
			</svg>
			<div class="upload-text">
				<p id="upload-desc">Drop a CSV file here or click to select one.</p>
				<p class="hint">Or press <kbd>Enter</kbd> / <kbd>Space</kbd> to open file picker.</p>
			</div>
		</div>
	</div>
{/if}

<style>
	:global(.visually-hidden) {
		position: absolute !important;
		height: 1px;
		width: 1px;
		overflow: hidden;
		clip: rect(1px, 1px, 1px, 1px);
		white-space: nowrap;
	}

	.upload {
		border-radius: 12px;
		padding: 1rem;
		outline: none;
	}

	.upload-inner {
		display: flex;
		align-items: center;
		gap: 1rem;
		padding: 1.25rem;
		border: 1px solid var(--color-button);
		background: var(--color-card);
		box-shadow: 0 1px 2px rgba(0, 0, 0, 0.25);
		cursor: pointer;
		transition:
			transform 0.08s,
			border-color 0.15s,
			box-shadow 0.15s;
		border-radius: 10px;
	}

	.upload-inner:hover,
	.upload-inner:focus-within,
	.upload-inner.active {
		transform: translateY(-1px);
		border-color: var(--color-primary);
		box-shadow: 0 6px 18px color-mix(in srgb, var(--color-primary), transparent 65%);
	}

	.icon {
		color: var(--color-primary);
	}

	.upload-text p {
		margin: 0;
		color: var(--color-text);
	}
	.hint {
		color: var(--color-muted);
		font-size: 0.85rem;
		margin-top: 0.25rem;
	}

	.meta {
		margin-top: 0.5rem;
		color: var(--color-text);
	}
	#upload-error {
		color: var(--color-error);
	}

	kbd {
		display: inline-block;
		padding: 0.15rem 0.4rem;
		border-radius: 4px;
		background: color-mix(in srgb, var(--color-card), var(--color-background) 15%);
		border: 1px solid var(--color-button);
		color: var(--color-text);
		font-family:
			ui-monospace, SFMono-Regular, Menlo, Monaco, 'Roboto Mono', 'Segoe UI Mono', monospace;
		font-size: 0.75rem;
	}
</style>
