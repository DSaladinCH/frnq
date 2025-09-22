<script lang="ts">
	interface Props {
		onfileSelected?: (data: { filename: string; content: string }) => void;
		onnext?: () => void;
	}

	const { onfileSelected, onnext }: Props = $props();

	let fileInput: HTMLInputElement | null = $state(null);
	let isDragActive = $state(false);
	let filename: string | null = $state(null);
	let fileContent: string | null = $state(null);
	let error: string | null = $state(null);
	let isProcessing = $state(false);

	function openFilePicker() {
		fileInput?.click();
	}

	function reset() {
		filename = null;
		fileContent = null;
		error = null;
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
				content: text
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

<div class="upload-step">
	<div class="step-header">
		<h2>Upload CSV File</h2>
		<p class="step-description">
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
				class="visually-hidden"
				id="file"
				type="file"
				accept=".csv,text/csv"
				aria-hidden="true"
				onchange={onInputChange}
			/>

			<div class={isDragActive ? 'upload-inner active' : 'upload-inner'}>
				{#if isProcessing}
					<div class="processing-icon">
						<i class="fa-solid fa-spinner fa-spin"></i>
					</div>
				{:else}
					<svg
						class="upload-icon"
						width="48"
						height="48"
						viewBox="0 0 24 24"
						aria-hidden="true"
						focusable="false"
					>
						<path fill="currentColor" d="M19 9h-4V3H9v6H5l7 7 7-7zM5 18v2h14v-2H5z" />
					</svg>
				{/if}
				
				<div class="upload-text">
					{#if isProcessing}
						<p>Processing file...</p>
					{:else}
						<p id="upload-desc">Drop a CSV file here or click to select one</p>
						<p class="hint">Supported formats: .csv</p>
						<p class="hint">Press <kbd>Enter</kbd> or <kbd>Space</kbd> to open file picker</p>
					{/if}
				</div>
			</div>
		</div>
	{:else}
		<div class="file-selected">
			<div class="file-info">
				<div class="file-icon">
					<i class="fa-solid fa-file-csv"></i>
				</div>
				<div class="file-details">
					<h3 class="filename">{filename}</h3>
					<p class="file-meta">
						{fileContent ? `${fileContent.split('\n').filter(line => line.trim()).length} rows` : 'Processing...'}
					</p>
				</div>
				<button class="btn-fake remove-file" onclick={reset} aria-label="Remove file">
					<i class="fa-solid fa-times"></i>
				</button>
			</div>
			
			{#if fileContent}
				<div class="file-preview">
					<h4>File Preview</h4>
					<div class="preview-content">
						{#each fileContent.split('\n').slice(0, 5) as line, index}
							{#if line.trim()}
								<div class="preview-line">
									<span class="line-number">{index + 1}</span>
									<span class="line-content">{line}</span>
								</div>
							{/if}
						{/each}
						{#if fileContent.split('\n').length > 5}
							<div class="preview-line more">
								<span class="line-number">...</span>
								<span class="line-content">and {fileContent.split('\n').length - 5} more rows</span>
							</div>
						{/if}
					</div>
				</div>
			{/if}
		</div>
	{/if}

	{#if error}
		<div class="error-message" id="upload-error">
			<i class="fa-solid fa-exclamation-triangle"></i>
			{error}
		</div>
	{/if}

	{#if filename && fileContent}
		<div class="step-actions">
			<button class="btn btn-primary btn-big" onclick={handleNext}>
				Continue to Column Mapping
				<i class="fa-solid fa-arrow-right ml-2"></i>
			</button>
		</div>
	{/if}
</div>

<style>
	:global(.visually-hidden) {
		position: absolute !important;
		height: 1px;
		width: 1px;
		overflow: hidden;
		clip: rect(1px, 1px, 1px, 1px);
		white-space: nowrap;
	}

	.upload-step {
		max-width: 600px;
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

	.upload-icon {
		color: var(--color-primary);
		flex-shrink: 0;
	}

	.processing-icon {
		color: var(--color-primary);
		font-size: 2rem;
		flex-shrink: 0;
	}

	.upload-text {
		flex: 1;
	}

	.upload-text p {
		margin: 0;
		color: var(--color-text);
	}

	.hint {
		color: var(--color-muted) !important;
		font-size: 0.85rem;
		margin-top: 0.5rem !important;
	}

	.file-selected {
		background: var(--color-card);
		border-radius: 12px;
		padding: 1.5rem;
		margin-bottom: 1.5rem;
	}

	.file-info {
		display: flex;
		align-items: center;
		gap: 1rem;
		margin-bottom: 1rem;
	}

	.file-icon {
		font-size: 2rem;
		color: var(--color-success);
		flex-shrink: 0;
	}

	.file-details {
		flex: 1;
	}

	.filename {
		font-size: 1.1rem;
		font-weight: 600;
		color: var(--color-text);
		margin: 0 0 0.25rem 0;
	}

	.file-meta {
		color: var(--color-muted);
		font-size: 0.9rem;
		margin: 0;
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

	.file-preview {
		border-top: 1px solid var(--color-button);
		padding-top: 1rem;
	}

	.file-preview h4 {
		font-size: 0.9rem;
		font-weight: 600;
		color: var(--color-text);
		margin: 0 0 0.75rem 0;
	}

	.preview-content {
		background: var(--color-background);
		border-radius: 6px;
		padding: 0.75rem;
		font-family: 'Fira Mono', 'Consolas', monospace;
		font-size: 0.8rem;
		overflow-x: auto;
	}

	.preview-line {
		display: flex;
		gap: 1rem;
		margin-bottom: 0.25rem;
	}

	.preview-line.more {
		color: var(--color-muted);
		font-style: italic;
	}

	.line-number {
		color: var(--color-muted);
		min-width: 20px;
		text-align: right;
		flex-shrink: 0;
	}

	.line-content {
		color: var(--color-text);
		word-break: break-all;
	}

	.error-message {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		background: color-mix(in srgb, var(--color-error), transparent 90%);
		color: var(--color-error);
		padding: 0.75rem 1rem;
		border-radius: 6px;
		border: 1px solid color-mix(in srgb, var(--color-error), transparent 70%);
		margin-bottom: 1.5rem;
	}

	.step-actions {
		display: flex;
		justify-content: center;
		margin-top: 2rem;
	}

	kbd {
		display: inline-block;
		padding: 0.15rem 0.4rem;
		border-radius: 4px;
		background: color-mix(in srgb, var(--color-card), var(--color-background) 15%);
		border: 1px solid var(--color-button);
		color: var(--color-text);
		font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, 'Roboto Mono', 'Segoe UI Mono', monospace;
		font-size: 0.75rem;
	}

	.ml-2 {
		margin-left: 0.5rem;
	}
</style>