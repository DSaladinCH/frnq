<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { FullAutoFill } from 'svelte/elements';
	import Input from './Input.svelte';

	let {
		id = '',
		title = '',
		placeholder = 'Enter Password',
		autocomplete = 'current-password',
		required = false,
		value = $bindable(),
		disabled = false,
		onchange = $bindable(),
		onkeypress
	}: {
		id?: string;
		title?: string | Snippet;
		placeholder?: string;
		autocomplete?: FullAutoFill | undefined | null;
		required?: boolean;
		value?: any;
		disabled?: boolean;
		onchange?: (event: Event) => void;
		onkeypress?: (event: KeyboardEvent) => void;
	} = $props();

	id = id || `textbox-${Math.random().toString(36).slice(2, 11)}`;

	let type = $state('password');

	function togglePasswordVisibility() {
		type = type === 'password' ? 'text' : 'password';
	}
</script>

<div class="flex h-full flex-col gap-1">
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

	<div class="relative">
		<Input
			{type}
			{id}
			{autocomplete}
			{required}
			{onchange}
			bind:value
			{placeholder}
			{onkeypress}
			{disabled} />

		<button
			type="button"
			class="color-muted hover:color-primary absolute right-3 top-1/2 -translate-y-1/2 transform transition-colors"
			onclick={togglePasswordVisibility}
			aria-label={type === 'text' ? 'Hide password' : 'Show password'}
		>
			<i class="fas {type === 'text' ? 'fa-eye-slash' : 'fa-eye'}"></i>
		</button>
	</div>
</div>

<style>
	:global(.textbox) {
		padding-right: 2.5rem !important;
	}
</style>
