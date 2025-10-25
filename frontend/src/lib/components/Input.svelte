<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { FullAutoFill } from 'svelte/elements';
	import AirDatepicker from 'air-datepicker';
	import 'air-datepicker/air-datepicker.css';
	import localeEn from 'air-datepicker/locale/en';
	import localeDe from 'air-datepicker/locale/de';
	import localeFr from 'air-datepicker/locale/fr';
	import localeIt from 'air-datepicker/locale/it';
	import { onMount } from 'svelte';
	import { userPreferences } from '$lib/stores/userPreferences';

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
		locale = 'de-CH',
		step,
		min,
		max,
		oninput = $bindable(),
		onkeydown = $bindable(),
		onchange = $bindable(),
		onfocus = $bindable(),
		onkeypress = $bindable(),
		inputElement = $bindable()
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
		locale?: string;
		step?: string | number;
		min?: string | number;
		max?: string | number;
		oninput?: (event: Event) => void;
		onkeydown?: (event: KeyboardEvent) => void;
		onchange?: (event: Event) => void;
		onfocus?: (event: FocusEvent) => void;
		onkeypress?: (event: KeyboardEvent) => void;
		inputElement?: HTMLInputElement;
	} = $props();

	id = id || `textbox-${Math.random().toString(36).slice(2, 11)}`;
	const isCheckbox = type === 'checkbox';
	const isFileInput = type === 'file';
	const isDateTimeInput = type === 'datetime-local' || type === 'date' || type === 'time';

	let dateInputRef: HTMLInputElement | null = $state(null);
	let datepicker: AirDatepicker<HTMLInputElement> | null = null;
	let preferences = $state($userPreferences);

	// Subscribe to user preferences changes
	$effect(() => {
		const unsubscribe = userPreferences.subscribe((prefs) => {
			preferences = prefs;
			// Update datepicker locale if it exists
			if (datepicker && isDateTimeInput) {
				const newLocale = getDatepickerLocale(prefs.dateFormat);
				datepicker.update({
					locale: newLocale
				});
			}
		});
		return unsubscribe;
	});

	// Map locale strings or user preference to air-datepicker locale objects
	function getDatepickerLocale(localeStr: string) {
		const localeLower = localeStr.toLowerCase();
		
		// Handle user preference format
		if (localeLower === 'german') return localeDe;
		if (localeLower === 'english') return localeEn;
		
		// Handle standard locale codes (backward compatibility)
		if (localeLower.startsWith('de')) return localeDe;
		if (localeLower.startsWith('fr')) return localeFr;
		if (localeLower.startsWith('it')) return localeIt;
		
		return localeEn; // Default to English
	}

	// Initialize air-datepicker when component mounts
	onMount(() => {
		if (isDateTimeInput && dateInputRef) {
			// Parse initial value
			let initialDate: Date | undefined = undefined;
			if (value) {
				if (value instanceof Date) {
					initialDate = value;
				} else if (typeof value === 'string') {
					const parsed = new Date(value);
					if (!isNaN(parsed.getTime())) {
						initialDate = parsed;
					}
				}
			}

			// Find if input is inside a modal dialog
			const modalDialog = dateInputRef.closest('dialog');
			const containerElement = modalDialog || document.body;

			// Configure datepicker based on type
			// Use user's preference if available, otherwise fall back to locale prop
			const datepickerLocale = preferences.dateFormat 
				? getDatepickerLocale(preferences.dateFormat)
				: getDatepickerLocale(locale);
			
			const options: any = {
				locale: datepickerLocale,
				timepicker: type === 'datetime-local',
				onlyTimepicker: type === 'time',
				autoClose: type === 'date',
				selectedDates: initialDate ? [initialDate] : [],
				container: containerElement, // Render in modal or body
				visible: false,
				position({
					$datepicker,
					$target,
					$pointer
				}: {
					$datepicker: HTMLDivElement;
					$target: HTMLInputElement;
					$pointer: HTMLElement;
					isViewChange: boolean;
					done: () => void;
				}) {
					// Get target element position
					const coords = $target.getBoundingClientRect();
					const dpHeight = $datepicker.clientHeight;
					const dpWidth = $datepicker.clientWidth;

					// Check if we're inside a modal
					const isInModal = !!modalDialog;

					let top: number;
					let left: number;

					if (isInModal && modalDialog) {
						// Calculate position relative to modal's viewport (no scroll offset needed)
						// Modal dialog is fixed, so use coords directly
						top = coords.bottom + 4; // 4px gap
						left = coords.left;

						// Check if datepicker would go off the bottom of the modal viewport
						if (coords.bottom + dpHeight + 4 > window.innerHeight) {
							// Position above the input instead
							top = coords.top - dpHeight - 4;
						}

						// Check if datepicker would go off the right of the viewport
						if (coords.left + dpWidth > window.innerWidth) {
							// Align to the right edge of the input
							left = coords.right - dpWidth;
						}

						// Ensure it doesn't go off the left edge
						if (left < 0) {
							left = 4; // Small margin from left edge
						}

						// Ensure it doesn't go off the top
						if (top < 0) {
							top = 4;
						}
					} else {
						// Calculate position relative to document (with scroll offset)
						top = coords.bottom + window.scrollY + 4; // 4px gap
						left = coords.left + window.scrollX;

						// Check if datepicker would go off the bottom of the viewport
						if (coords.bottom + dpHeight + 4 > window.innerHeight) {
							// Position above the input instead
							top = coords.top + window.scrollY - dpHeight - 4;
						}

						// Check if datepicker would go off the right of the viewport
						if (coords.left + dpWidth > window.innerWidth) {
							// Align to the right edge of the input
							left = coords.right + window.scrollX - dpWidth;
						}

						// Ensure it doesn't go off the left edge
						if (left < 0) {
							left = 4; // Small margin from left edge
						}
					}

					$datepicker.style.left = `${left}px`;
					$datepicker.style.top = `${top}px`;

					// Position the pointer arrow
					const pointerLeft = coords.left + coords.width / 2 - left - 10;
					$pointer.style.left = `${pointerLeft}px`;
				},
				onSelect({
					date,
					formattedDate
				}: {
					date: Date | Date[];
					formattedDate: string | string[];
				}) {
					if (date) {
						// Get the actual date object - handle both single Date and Date array
						let selectedDate: Date | null = null;
						
						if (Array.isArray(date)) {
							selectedDate = date[0];
						} else if (date instanceof Date) {
							selectedDate = date;
						}
						
						if (selectedDate instanceof Date) {
							// Format as YYYY-MM-DDTHH:mm for datetime-local or YYYY-MM-DD for date
							const pad = (n: number) => n.toString().padStart(2, '0');
							if (type === 'datetime-local') {
								value = `${selectedDate.getFullYear()}-${pad(selectedDate.getMonth() + 1)}-${pad(selectedDate.getDate())}T${pad(selectedDate.getHours())}:${pad(selectedDate.getMinutes())}`;
							} else if (type === 'date') {
								value = `${selectedDate.getFullYear()}-${pad(selectedDate.getMonth() + 1)}-${pad(selectedDate.getDate())}`;
							} else if (type === 'time') {
								value = `${pad(selectedDate.getHours())}:${pad(selectedDate.getMinutes())}`;
							}

							// Trigger onchange if provided
							if (onchange && dateInputRef) {
								const event = new Event('change', { bubbles: true });
								onchange(event);
							}
						}
					}
				}
			};

			// Add min/max constraints if provided
			if (min) {
				options.minDate = new Date(min as string);
			}
			if (max) {
				options.maxDate = new Date(max as string);
			}

			datepicker = new AirDatepicker(dateInputRef, options);

			return () => {
				if (datepicker) {
					datepicker.destroy();
				}
			};
		}
	});

	// Update datepicker when value changes externally
	$effect(() => {
		if (datepicker && value && isDateTimeInput) {
			let newDate: Date | undefined = undefined;
			if (value instanceof Date) {
				newDate = value;
			} else if (typeof value === 'string') {
				const parsed = new Date(value);
				if (!isNaN(parsed.getTime())) {
					newDate = parsed;
				}
			}
			if (newDate) {
				datepicker.selectDate(newDate, { silent: true });
			}
		}
	});

	export function setFile(file: File) {
		const dataTransfer = new DataTransfer();
		dataTransfer.items.add(file);
		files = dataTransfer.files;
	}
	export function clearFileInput() {
		value = '';
		files = null;
	}
</script>

<div
	class="flex h-full w-full flex-col gap-1 {isCheckbox
		? 'flex-row-reverse items-center justify-end gap-2'
		: ''}"
>
	{#if title}
		<!-- Check if title is string or snippet -->
		<label for={id} class="leading-none">
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
				{onfocus}
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
	{:else if isDateTimeInput}
		<input
			bind:this={dateInputRef}
			class="textbox grow"
			type="text"
			{id}
			{required}
			{disabled}
			{placeholder}
			{onfocus}
			readonly
		/>
	{:else}
		<input
			class="textbox grow"
			bind:this={inputElement}
			{type}
			{id}
			{autocomplete}
			{required}
			{oninput}
			{onkeydown}
			{onchange}
			{onfocus}
			bind:value
			{placeholder}
			{onkeypress}
			{accept}
			{disabled}
			{step}
			{min}
			{max}
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

	/* Air Datepicker theme customization - Colors only */
	:global(.air-datepicker) {
		z-index: 10000 !important;
		position: fixed !important;
	}

	/* When datepicker is inside a dialog, ensure it's on top */
	:global(dialog .air-datepicker) {
		z-index: 99999 !important;
		position: fixed !important;
	}
</style>
