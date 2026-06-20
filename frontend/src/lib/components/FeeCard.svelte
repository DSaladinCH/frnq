<script lang="ts">
	import type { GeneralFeeViewDto } from '$lib/services/positionService';
	import Button from './Button.svelte';
	import { ColorStyle } from '$lib/types/ColorStyle';
	import { TextSize } from '$lib/types/TextSize';
	import { formatDate, DateFormatType } from '$lib/utils/dateFormat';
	import { StylePadding } from '$lib/types/StylePadding';
	import { ContentWidth } from '$lib/types/ContentSize';

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

<button class="btn-fake text-left w-full" onclick={onEdit}>
	<div class="card card-reactive @container relative grid grid-cols-1 gap-1">
		<div class="color-muted flex items-center gap-2 text-sm row-1">
			<span>{formatDate(new Date(fee.date), DateFormatType.English)}</span>
			<span>•</span>
			<span>Fee</span>
		</div>

		<div class="row-2">
			<span class="font-bold">{fee.description}</span>
		</div>

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

		<div class="pt-2 row-3 col-span-2 @md:grid-cols-2 @lg:grid-cols-2 grid grid-cols-1 gap-1 text-sm">
			<div class="grid grid-rows-2">
				<span class="color-muted">Amount</span>
				<span class="font-bold">{formatCurrency(fee.amount)}</span>
			</div>
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

	.row-3 {
		grid-row: 3;
	}

	.row-span-2 {
		grid-row: 1 / span 2;
	}

	.-col-1 {
		grid-column: -1;
	}

	.col-span-2 {
		grid-column: span 2;
	}
</style>
