<script lang="ts">
	import type { GeneralFeeViewDto } from '$lib/services/positionService';
	import Button from './Button.svelte';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import { TextSize } from '$lib/types/TextSize';
	import { ContentWidth } from '$lib/types/ContentSize';
	import { formatDate, DateFormatType } from '$lib/utils/dateFormat';
	import { StylePadding } from '$lib/types/StylePadding';

	let {
		fee,
		onEdit,
		onDelete
	}: {
		fee: GeneralFeeViewDto;
		onEdit: () => void;
		onDelete: (event: MouseEvent) => Promise<void>;
	} = $props();

	let isDeleting = $state(false);

	function formatCurrency(value: number): string {
		return value.toLocaleString(undefined, { style: 'currency', currency: 'CHF' });
	}

	async function deleteFee(event: MouseEvent) {
		if (isDeleting) return;

		isDeleting = true;

		event.stopPropagation();
		await onDelete(event);

		isDeleting = false;
	}
</script>

<button class="btn-fake w-full text-left" onclick={onEdit}>
	<div class="card card-reactive relative grid grid-cols-[2fr_130px_auto] grid-rows-[auto_1fr] gap-2 text-sm items-center leading-none">
		<div class="row-1 col-1 color-muted flex items-center gap-2">
			<span>{formatDate(new Date(fee.date), DateFormatType.English)}</span>
			<span>•</span>
			<span>Fee</span>
		</div>

		<div class="row-2 col-1 text-base leading-none">
			<span class="font-bold">{fee.description}</span>
		</div>

		<span class="row-1 color-muted">Amount</span>
		<span class="row-2 font-bold">{formatCurrency(fee.amount)}</span>

		<div class="row-span-2 -col-1 h-10 w-10">
			<Button
				onclick={deleteFee}
				disabled={isDeleting}
				isLoading={isDeleting}
				icon="fa-solid fa-trash"
				textSize={TextSize.Small}
				style={ColorStyle.Secondary}
				padding={StylePadding.None}
				width={ContentWidth.Full}
			/>
		</div>
	</div>
</button>

<style>
	.btn-fake {
		border: none;
		background: none;
		cursor: pointer;
		padding: 0;
		font-family: inherit;
	}

	.card-reactive {
		transition: all 0.2s ease;
	}

	:global(.card-reactive:hover) {
		transform: translateY(-2px);
	}

	.row-1 {
		grid-row: 1;
	}

	.row-2 {
		grid-row: 2;
	}

	.row-span-2 {
		grid-row: 1 / span 2;
	}

	.-col-1 {
		grid-column: -1;
	}
</style>
