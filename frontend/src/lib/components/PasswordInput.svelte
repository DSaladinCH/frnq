<script lang="ts">
    import type { Snippet } from 'svelte';
    import type { FullAutoFill } from 'svelte/elements';

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

<div class="flex flex-col gap-1 h-full">
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

    <div class="relative">
        <input
            class="textbox grow"
            style="padding-right: 2.5rem;"
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
            class="absolute right-3 top-1/2 transform -translate-y-1/2 color-muted hover:color-primary transition-colors"
            onclick={togglePasswordVisibility}
            aria-label={type === 'text' ? 'Hide password' : 'Show password'}>
            <i class="fas {type === 'text' ? 'fa-eye-slash' : 'fa-eye'}"></i>
        </button>
    </div>
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

    .textbox:hover {
        border-color: var(--color-primary) !important;
    }

    .textbox:focus,
    .textbox:-webkit-autofill:focus {
        box-shadow: none !important;
        border-color: var(--color-secondary) !important;
    }
</style>
