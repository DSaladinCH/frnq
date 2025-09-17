<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { FullAutoFill } from 'svelte/elements';

	let {
		type = 'text',
		id = '',
		title = '',
		placeholder = 'Enter text',
		autocomplete = 'off',
		accept = '',
		name = '',
		required = false,
		value = $bindable(),
		checked = $bindable(),
		files = $bindable(),
		disabled = false,
		onchange = $bindable(),
		onkeypress
	}: {
		type?: string;
		id?: string;
		title?: string | Snippet;
		placeholder?: string;
		autocomplete?: FullAutoFill | undefined | null;
		accept?: string;
		name?: string;
		required?: boolean;
		value?: any;
		checked?: boolean;
		files?: FileList | null;
		disabled?: boolean;
		onchange?: (event: Event) => void;
		onkeypress?: (event: KeyboardEvent) => void;
	} = $props();

	id = id || `textbox-${Math.random().toString(36).slice(2, 11)}`;
	const isCheckbox = type === 'checkbox';
	const isFileInput = type === 'file';

	export function setFile(file: File) {
		const dataTransfer = new DataTransfer();
		dataTransfer.items.add(file);
		files = dataTransfer.files;
	}
	export function clearFileInput() {
		console.log('Clearing file input');
		value = '';
		files = null;
	}
</script>

<div
	class="flex h-full flex-col gap-1 {isCheckbox
		? 'flex-row-reverse items-center justify-end gap-2'
		: ''}"
>
	{#if title}
		<!-- Check if title is string or snippet -->
		<label for={id}>
			{#if typeof title === 'string'}
				{title}
			{:else}
				{@render title()}
			{/if}
		</label>
	{/if}

	{#if isCheckbox}
		<input class="checkbox" type="checkbox" {id} bind:checked {required} {onchange} {disabled} />
	{:else if isFileInput}
		<div class="relative flex w-full flex-1 grow">
			<input
				class="absolute h-0 w-0 overflow-hidden opacity-0"
				type="file"
				{id}
				bind:files
				bind:value
				{name}
				{accept}
				{required}
				{onchange}
				{disabled}
			/>
			<label
				for={id}
				class="textbox flex w-full flex-1 grow cursor-pointer items-center transition-all duration-200 {disabled
					? 'cursor-not-allowed opacity-60'
					: ''}"
			>
				{files && files.length > 0 ? files[0].name : 'Select a file...'}
			</label>
		</div>
	{:else}
		<input
			class="textbox grow"
			{type}
			{id}
			{autocomplete}
			{required}
			{onchange}
			bind:value
			{placeholder}
			{onkeypress}
			{accept}
			{disabled}
		/>
	{/if}
</div>

<style>
	.textbox,
	.textbox:-webkit-autofill {
		display: block;
		width: 100%;
		max-width: 100%;
		border-width: 1px;
		border-radius: 0.25em;
		border-style: solid;
		min-height: 30px;
		max-height: 50px;
		padding-left: 10px;
		padding-right: 10px;
		margin-top: 5px;
		margin-bottom: 5px;
		outline: 0;
		background-color: var(--color-card);
		border-color: var(--color-button);
		color: var(--color-text);
	}

	.textbox.flex {
		display: flex !important;
	}

	.textbox:hover {
        border-color: var(--color-primary) !important;
	}

	.textbox:focus,
	.textbox:-webkit-autofill:focus {
        box-shadow: none !important;
        border-color: var(--color-secondary) !important;
	}

	.checkbox {
		width: 20px;
		height: 20px;
		border-radius: 0.25rem;
		background-color: var(--color-control);
		border: 1px solid var(--color-primary);
		cursor: pointer;
		outline: 0;
	}

	.checkbox:hover {
		border-color: var(--color-primary) !important;
	}

	.checkbox:focus {
		box-shadow: none !important;
	}
</style>
