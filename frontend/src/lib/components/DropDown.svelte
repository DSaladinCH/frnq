<script lang="ts">
	export let options: { value: string; label: string }[];
	export let selected: string;
	export let onSelect: (val: string) => void;
	let open = false;
	function label(val: string) {
		return (options && options.find((o) => o.value === val)?.label) || val;
	}
	function select(val: string) {
		if (onSelect) onSelect(val);
		open = false;
	}
	function handleDropdownKey(e: KeyboardEvent, val: string) {
		if (e.key === 'Enter' || e.key === ' ') {
			select(val);
			e.preventDefault();
		} else if (e.key === 'Escape') {
			open = false;
		}
	}
</script>

<div class="custom-dropdown">
	<button
		type="button"
		class="dropdown-toggle"
		aria-haspopup="listbox"
		aria-expanded={open}
		on:click={() => (open = !open)}
		on:keydown={(e) => {
			if (e.key === 'ArrowDown' && open) {
				const first = document.querySelector('.dropdown-list li');
				first && (first as HTMLElement).focus();
			} else if (e.key === 'Escape') {
				open = false;
			}
		}}
		tabindex="0"
	>
		{label(selected)}
		<span class="dropdown-arrow">▼</span>
	</button>
	{#if open}
		<ul class="dropdown-list" role="listbox">
			{#each options as opt}
				<li
					role="option"
					class:selected={selected === opt.value}
					aria-selected={selected === opt.value}
					tabindex="0"
					on:click={() => select(opt.value)}
					on:keydown={(e) => handleDropdownKey(e, opt.value)}
				>
					{opt.label}
				</li>
			{/each}
		</ul>
	{/if}
</div>

<style>
	.custom-dropdown {
		position: relative;
		width: 140px;
		z-index: 20;
	}
	.dropdown-toggle {
		width: 100%;
		background: rgba(62, 62, 68, 0.7);
		color: #fff;
		border: none;
		border-radius: 12px;
		padding: 0.3rem 1rem;
		font-size: 1rem;
		font-weight: 500;
		cursor: pointer;
		transition:
			background 0.2s,
			color 0.2s;
		outline: none;
		display: flex;
		align-items: center;
		justify-content: space-between;
	}
	.dropdown-toggle:focus {
		outline: none;
		box-shadow: none;
	}
	.dropdown-arrow {
		margin-left: 0.5rem;
		font-size: 0.9em;
		color: #aaa;
	}
	.dropdown-list {
		position: absolute;
		top: 110%;
		left: 0;
		width: 100%;
		background: #23232b;
		border-radius: 12px;
		box-shadow: 0 8px 32px #000c;
		padding: 0.2rem 0;
		margin: 0;
		list-style: none;
		z-index: 20;
		text-align: left;
	}
	.dropdown-list li {
		padding: 0.5rem 1rem;
		color: #fff;
		cursor: pointer;
		transition:
			background 0.15s,
			color 0.15s;
		font-size: 1rem;
		border: none;
		background: none;
	}
	.dropdown-list li.selected,
	.dropdown-list li[aria-selected='true'] {
		background: #18181b;
		color: var(--color-primary);
	}
	.dropdown-list li:hover {
		background: #2a2a33;
		color: #10b981;
	}
	.dropdown-list li:focus {
		outline: none;
	}
</style>
