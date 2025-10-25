<script lang="ts">
	import Button from './Button.svelte';
	import Input from './Input.svelte';

	interface Props {
		onfileSelected?: (data: {
			filename: string;
			content: string;
			treatHeaderAsData: boolean;
		}) => void;
		onnext?: () => void;
	}

	const { onfileSelected, onnext }: Props = $props();

	let fileInput: HTMLInputElement | null = $state(null);
	let isDragActive = $state(false);
	let filename: string | null = $state(null);
	let fileContent: string | null = $state(null);
	let error: string | null = $state(null);
	let isProcessing = $state(false);
	let treatHeaderAsData = $state(false);

	function openFilePicker() {
		fileInput?.click();
	}

	function reset() {
		filename = null;
		fileContent = null;
		error = null;
		treatHeaderAsData = false;
		if (fileInput) fileInput.value = '';
	}

	function handleFiles(files: FileList | null) {
		error = null;
		filename = null;
		fileContent = null;
		isProcessing = true;

		if (!files || files.length === 0) {
			isProcessing = false;
			return;
		}

		const file = files[0];

		if (!file.name.toLowerCase().endsWith('.csv')) {
			error = 'Please upload a CSV file.';
			isProcessing = false;
			return;
		}

		filename = file.name;
		const reader = new FileReader();

		reader.onload = () => {
			const text = reader.result as string;
			fileContent = text;
			isProcessing = false;

			onfileSelected?.({
				filename: filename!,
				content: text,
				treatHeaderAsData
			});
		};

		reader.onerror = () => {
			error = 'Failed to read file.';
			isProcessing = false;
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
			e.preventDefault();
			reset();
		}
	}

	function handleNext() {
		if (fileContent && filename) {
			onnext?.();
		}
	}
</script>

<div class="mx-auto max-w-2xl">
	<div class="mb-8 text-center">
		<h2 class="color-default mb-2 text-2xl font-semibold">Upload CSV File</h2>
		<p class="color-muted m-0 leading-relaxed">
			Select or drop a CSV file containing your investment data. The file should include columns for
			symbol, transaction type, date, amount, unit price, and fees.
		</p>
	</div>

	{#if !filename}
		<div
			class="upload"
			role="button"
			tabindex="0"
			aria-label="Upload CSV file"
			aria-describedby="upload-desc upload-error"
			onclick={openFilePicker}
			onkeydown={onKeyDown}
			ondrop={onDrop}
			ondragover={onDragOver}
			ondragleave={onDragLeave}
		>
			<input
				bind:this={fileInput}
				class="sr-only"
				id="file"
				type="file"
				accept=".csv,text/csv"
				aria-hidden="true"
				onchange={onInputChange}
			/>

			<div class={isDragActive ? 'upload-inner active' : 'upload-inner'}>
				{#if isProcessing}
					<div class="color-primary flex-shrink-0 text-3xl">
						<i class="fa-solid fa-spinner fa-spin"></i>
					</div>
				{:else}
					<svg
						class="color-primary flex-shrink-0"
						width="48"
						height="48"
						viewBox="0 0 24 24"
						aria-hidden="true"
						focusable="false"
					>
						<path fill="currentColor" d="M19 9h-4V3H9v6H5l7 7 7-7zM5 18v2h14v-2H5z" />
					</svg>
				{/if}

				<div class="flex-1">
					{#if isProcessing}
						<p class="color-default m-0">Processing file...</p>
					{:else}
						<p id="upload-desc" class="color-default m-0">
							Drop a CSV file here or click to select one
						</p>
						<p class="hint">Supported formats: .csv</p>
						<p class="hint">Press <kbd>Enter</kbd> or <kbd>Space</kbd> to open file picker</p>
					{/if}
				</div>
			</div>
		</div>
	{:else}
		<div class="bg-card mb-6 rounded-xl p-6">
			<div class="mb-4 flex items-center gap-4">
				<div class="color-success flex-shrink-0 text-3xl">
					<i class="fa-solid fa-file-csv"></i>
				</div>
				<div class="flex-1">
					<h3 class="color-default mb-1 text-lg font-semibold">{filename}</h3>
					<p class="color-muted m-0 text-sm">
						{fileContent
							? `${fileContent.split('\n').filter((line) => line.trim()).length} rows`
							: 'Processing...'}
					</p>
				</div>
				<button class="btn-fake remove-file" onclick={reset} aria-label="Remove file">
					<i class="fa-solid fa-times"></i>
				</button>
			</div>

			{#if fileContent}
				<div class="border-button border-t pt-4">
					<h4 class="color-default mb-3 text-sm font-semibold">File Preview</h4>
					<div class="file-preview-content">
						{#each fileContent.split('\n').slice(0, 5) as line, index}
							{#if line.trim()}
								<div class="mb-1 last:mb-0 flex gap-4">
									<span class="color-muted min-w-5 flex-shrink-0 text-right text-sm"
										>{index + 1}</span
									>
									<span class="color-default break-all text-sm">{line}</span>
								</div>
							{/if}
						{/each}
						{#if fileContent.split('\n').length > 5}
							<div class="color-muted ml-2 mt-1 flex gap-4 italic">
								<span class="min-w-5 flex-shrink-0 text-right text-sm">...</span>
								<span class="text-sm">and {fileContent.split('\n').length - 5} more rows</span>
							</div>
						{/if}
					</div>
				</div>

				<div class="border-button mt-4 border-t pt-4 text-sm">
					<h4 class="color-default mb-3 font-semibold">Import Options</h4>
					<Input
						type="checkbox"
						bind:checked={treatHeaderAsData}
						title="Treat first row as data"
						onchange={() =>
							onfileSelected?.({ filename: filename!, content: fileContent!, treatHeaderAsData })}
					/>
					<div class="color-muted mt-1 text-xs">
						Enable this if your CSV file doesn't have headers and the first row contains actual
						investment data
					</div>
				</div>
			{/if}
		</div>
	{/if}

	{#if error}
		<div
			class="color-error mb-6 flex items-center gap-2 rounded-lg border border-[color-mix(in_srgb,var(--color-error),transparent_70%)] bg-[color-mix(in_srgb,var(--color-error),transparent_90%)] px-4 py-3"
			id="upload-error"
		>
			<i class="fa-solid fa-exclamation-triangle"></i>
			{error}
		</div>
	{/if}

	{#if filename && fileContent}
		<div class="flex justify-center">
			<Button text="Continue to Column Mapping" icon="fa-solid fa-arrow-right" onclick={handleNext} />
		</div>
	{/if}
</div>

<style>
	/* Keep complex interactions and animations in CSS */
	.upload {
		border-radius: 12px;
		padding: 1rem;
		outline: none;
		margin-bottom: 1.5rem;
	}

	.upload-inner {
		display: flex;
		align-items: center;
		gap: 1.5rem;
		padding: 2rem;
		border: 2px dashed var(--color-button);
		background: var(--color-card);
		cursor: pointer;
		transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
		border-radius: 12px;
		min-height: 120px;
	}

	.upload-inner:hover,
	.upload-inner:focus-within,
	.upload-inner.active {
		transform: translateY(-2px);
		border-color: var(--color-primary);
		background: color-mix(in srgb, var(--color-card), var(--color-primary) 5%);
		box-shadow: 0 8px 25px color-mix(in srgb, var(--color-primary), transparent 80%);
	}

	.hint {
		color: var(--color-muted) !important;
		font-size: 0.85rem;
		margin-top: 0.5rem !important;
	}

	.remove-file {
		color: var(--color-muted);
		font-size: 1.2rem;
		padding: 0.5rem;
		border-radius: 50%;
		transition: all 0.2s ease;
	}

	.remove-file:hover {
		color: var(--color-error);
		background: color-mix(in srgb, var(--color-error), transparent 90%);
	}

	.file-preview-content {
		background: var(--color-background);
		border-radius: 6px;
		padding: 0.75rem;
		font-family: 'Fira Mono', 'Consolas', monospace;
		overflow-x: auto;
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

	.ml-2 {
		margin-left: 0.5rem;
	}
</style>
